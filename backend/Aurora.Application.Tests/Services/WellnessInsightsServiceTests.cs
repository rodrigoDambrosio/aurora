using System;
using System.Collections.Generic;
using System.Linq;
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

public class WellnessInsightsServiceTests
{
    private readonly Mock<IDailyMoodRepository> _dailyMoodRepositoryMock = new();
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly WellnessInsightsService _service;

    public WellnessInsightsServiceTests()
    {
        _service = new WellnessInsightsService(
            _dailyMoodRepositoryMock.Object,
            _eventRepositoryMock.Object,
            NullLogger<WellnessInsightsService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullDailyMoodRepository_ShouldThrow()
    {
        var act = () => new WellnessInsightsService(
            null!,
            _eventRepositoryMock.Object,
            NullLogger<WellnessInsightsService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("dailyMoodRepository");
    }

    [Fact]
    public void Constructor_WithNullEventRepository_ShouldThrow()
    {
        var act = () => new WellnessInsightsService(
            _dailyMoodRepositoryMock.Object,
            null!,
            NullLogger<WellnessInsightsService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("eventRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new WellnessInsightsService(
            _dailyMoodRepositoryMock.Object,
            _eventRepositoryMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_WithoutData_ShouldReturnEmptySummary()
    {
        var userId = Guid.NewGuid();
        const int year = 2025;
        const int month = 1;

        _dailyMoodRepositoryMock
            .Setup(repo => repo.GetMonthlyEntriesAsync(It.IsAny<Guid>(), year, month))
            .ReturnsAsync(Array.Empty<DailyMoodEntry>());

        _eventRepositoryMock
            .Setup(repo => repo.GetMonthlyEventsAsync(It.IsAny<Guid>(), year, month, null))
            .ReturnsAsync(new List<Event>());

        var result = await _service.GetMonthlySummaryAsync(userId, year, month);

        result.Should().NotBeNull();
        result.Year.Should().Be(year);
        result.Month.Should().Be(month);
        result.AverageMood.Should().Be(0);
        result.TotalTrackedDays.Should().Be(0);
        result.MoodTrend.Should().HaveCount(DateTime.DaysInMonth(year, month));
        result.MoodTrend.Should().OnlyContain(point => !point.AverageMood.HasValue && point.Entries == 0);
        result.MoodDistribution.Should().HaveCount(5);
        result.HasEventMoodData.Should().BeFalse();
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_WithMoodEntries_ShouldCalculateMetrics()
    {
        var userId = Guid.NewGuid();
        const int year = 2025;
        const int month = 3;
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        var entries = new List<DailyMoodEntry>
        {
            new() { EntryDate = monthStart, MoodRating = 5, Notes = "Óptimo" },
            new() { EntryDate = monthStart.AddDays(1), MoodRating = 3 },
            new() { EntryDate = monthStart.AddDays(2), MoodRating = 2, Notes = "Cansancio" }
        };

        _dailyMoodRepositoryMock
            .Setup(repo => repo.GetMonthlyEntriesAsync(It.IsAny<Guid>(), year, month))
            .ReturnsAsync(entries);

        _eventRepositoryMock
            .Setup(repo => repo.GetMonthlyEventsAsync(It.IsAny<Guid>(), year, month, null))
            .ReturnsAsync(new List<Event>());

        var result = await _service.GetMonthlySummaryAsync(userId, year, month);

        result.AverageMood.Should().Be(3.33);
        result.TotalTrackedDays.Should().Be(3);
        result.PositiveDays.Should().Be(1);
        result.NeutralDays.Should().Be(1);
        result.NegativeDays.Should().Be(1);
        result.Streaks.LongestPositive.Should().Be(1);
        result.Streaks.LongestNegative.Should().Be(1);

        result.BestDay.Should().NotBeNull();
        result.BestDay!.MoodRating.Should().Be(5);
        result.WorstDay.Should().NotBeNull();
        result.WorstDay!.MoodRating.Should().Be(2);

        result.MoodTrend.Should().HaveCount(DateTime.DaysInMonth(year, month));
        result.MoodTrend.ElementAt(0).AverageMood.Should().Be(5);
        result.MoodTrend.ElementAt(1).AverageMood.Should().Be(3);
        result.MoodTrend.ElementAt(2).AverageMood.Should().Be(2);
        result.MoodTrend.ElementAt(3).AverageMood.Should().BeNull();
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_WithEventMoodData_ShouldExposeCategoryImpacts()
    {
        var userId = Guid.NewGuid();
        const int year = 2025;
        const int month = 4;
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        _dailyMoodRepositoryMock
            .Setup(repo => repo.GetMonthlyEntriesAsync(It.IsAny<Guid>(), year, month))
            .ReturnsAsync(Array.Empty<DailyMoodEntry>());

        var focusCategory = new EventCategory { Id = Guid.NewGuid(), Name = "Trabajo", Color = "#3366FF" };
        var funCategory = new EventCategory { Id = Guid.NewGuid(), Name = "Ocio", Color = "#FFAA00" };

        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Sprint demo",
                StartDate = monthStart.AddDays(1),
                EndDate = monthStart.AddDays(1).AddHours(1),
                MoodRating = 5,
                EventCategoryId = focusCategory.Id,
                EventCategory = focusCategory
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Code review",
                StartDate = monthStart.AddDays(2),
                EndDate = monthStart.AddDays(2).AddHours(1),
                MoodRating = 4,
                EventCategoryId = focusCategory.Id,
                EventCategory = focusCategory
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Película",
                StartDate = monthStart.AddDays(3),
                EndDate = monthStart.AddDays(3).AddHours(2),
                MoodRating = 2,
                EventCategoryId = funCategory.Id,
                EventCategory = funCategory
            }
        };

        _eventRepositoryMock
            .Setup(repo => repo.GetMonthlyEventsAsync(It.IsAny<Guid>(), year, month, null))
            .ReturnsAsync(events);

        var result = await _service.GetMonthlySummaryAsync(userId, year, month);

        result.HasEventMoodData.Should().BeTrue();
        result.CategoryImpacts.Should().HaveCount(2);

        var topCategory = result.CategoryImpacts.Should().ContainSingle(dto => dto.CategoryId == focusCategory.Id).Which;
        topCategory.AverageMood.Should().Be(4.5);
        topCategory.EventCount.Should().Be(2);
        topCategory.PositiveCount.Should().Be(2);
        topCategory.NegativeCount.Should().Be(0);

        var secondCategory = result.CategoryImpacts.Should().ContainSingle(dto => dto.CategoryId == funCategory.Id).Which;
        secondCategory.AverageMood.Should().Be(2);
        secondCategory.EventCount.Should().Be(1);
        secondCategory.PositiveCount.Should().Be(0);
        secondCategory.NegativeCount.Should().Be(1);
    }
}
