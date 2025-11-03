using System;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Application.Services;
using Aurora.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aurora.Application.Tests.Services;

public class RecommendationServiceTests
{
    private readonly Mock<IDailyMoodRepository> _dailyMoodRepositoryMock = new();
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IRecommendationFeedbackRepository> _feedbackRepositoryMock = new();
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        _service = new RecommendationService(
            _dailyMoodRepositoryMock.Object,
            _eventRepositoryMock.Object,
            _feedbackRepositoryMock.Object,
            NullLogger<RecommendationService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullFeedbackRepository_ShouldThrow()
    {
        var act = () => new RecommendationService(
            _dailyMoodRepositoryMock.Object,
            _eventRepositoryMock.Object,
            null!,
            NullLogger<RecommendationService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("feedbackRepository");
    }

    [Fact]
    public async Task RecordFeedbackAsync_WithInvalidMood_ShouldThrow()
    {
        var dto = new RecommendationFeedbackDto
        {
            RecommendationId = "rec-123",
            Accepted = true,
            MoodAfter = 6
        };

        var act = async () => await _service.RecordFeedbackAsync(Guid.NewGuid(), dto);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*MoodAfter debe estar entre 1 y 5*");
    }

    [Fact]
    public async Task RecordFeedbackAsync_WithNewFeedback_ShouldAddEntity()
    {
        _feedbackRepositoryMock
            .Setup(repo => repo.GetByUserAndRecommendationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationFeedback?)null);

        _feedbackRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<RecommendationFeedback>()))
            .ReturnsAsync((RecommendationFeedback feedback) => feedback);

        _feedbackRepositoryMock
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new RecommendationFeedbackDto
        {
            RecommendationId = "rec-123",
            Accepted = true,
            Notes = "Muy buena sugerencia"
        };

        await _service.RecordFeedbackAsync(Guid.NewGuid(), dto);

        _feedbackRepositoryMock.Verify(repo => repo.AddAsync(It.Is<RecommendationFeedback>(entity =>
            entity.RecommendationId == "rec-123"
            && entity.Accepted
            && entity.Notes == "Muy buena sugerencia"
        )), Times.Once);

        _feedbackRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordFeedbackAsync_WithExistingFeedback_ShouldUpdateEntity()
    {
        var existing = new RecommendationFeedback
        {
            Id = Guid.NewGuid(),
            RecommendationId = "rec-123",
            UserId = Guid.NewGuid(),
            Accepted = false,
            Notes = "",
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        _feedbackRepositoryMock
            .Setup(repo => repo.GetByUserAndRecommendationAsync(It.IsAny<Guid>(), "rec-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _feedbackRepositoryMock
            .Setup(repo => repo.UpdateAsync(existing))
            .ReturnsAsync(existing);

        _feedbackRepositoryMock
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new RecommendationFeedbackDto
        {
            RecommendationId = "rec-123",
            Accepted = true,
            Notes = "Actualizado",
            MoodAfter = 4
        };

        await _service.RecordFeedbackAsync(existing.UserId, dto);

        existing.Accepted.Should().BeTrue();
        existing.Notes.Should().Be("Actualizado");
        existing.MoodAfter.Should().Be(4);

        _feedbackRepositoryMock.Verify(repo => repo.UpdateAsync(existing), Times.Once);
        _feedbackRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFeedbackSummaryAsync_WithFutureStart_ShouldThrow()
    {
        var future = DateTime.UtcNow.AddDays(1);

        var act = async () => await _service.GetFeedbackSummaryAsync(Guid.NewGuid(), future);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*inicio no puede estar en el futuro*");
    }

    [Fact]
    public async Task GetFeedbackSummaryAsync_WithNoEntries_ShouldReturnEmptySummary()
    {
        _feedbackRepositoryMock
            .Setup(repo => repo.GetFromDateAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RecommendationFeedback>());

        var periodStart = DateTime.UtcNow.AddDays(-30);

        var summary = await _service.GetFeedbackSummaryAsync(Guid.NewGuid(), periodStart);

        summary.TotalFeedback.Should().Be(0);
        summary.AcceptedCount.Should().Be(0);
        summary.AcceptanceRate.Should().Be(0);
        summary.AverageMoodAfter.Should().BeNull();
    }

    [Fact]
    public async Task GetFeedbackSummaryAsync_WithData_ShouldComputeMetrics()
    {
        var periodStart = DateTime.UtcNow.AddDays(-7);
        var feedback = new[]
        {
            new RecommendationFeedback { Accepted = true, MoodAfter = 5, SubmittedAtUtc = DateTime.UtcNow.AddDays(-1) },
            new RecommendationFeedback { Accepted = true, MoodAfter = 4, SubmittedAtUtc = DateTime.UtcNow.AddDays(-2) },
            new RecommendationFeedback { Accepted = false, SubmittedAtUtc = DateTime.UtcNow.AddDays(-3) }
        };

        _feedbackRepositoryMock
            .Setup(repo => repo.GetFromDateAsync(It.IsAny<Guid>(), periodStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        var summary = await _service.GetFeedbackSummaryAsync(Guid.NewGuid(), periodStart);

        summary.TotalFeedback.Should().Be(3);
        summary.AcceptedCount.Should().Be(2);
        summary.RejectedCount.Should().Be(1);
        summary.AcceptanceRate.Should().Be(66.7);
        summary.AverageMoodAfter.Should().Be(4.5);
    }
}
