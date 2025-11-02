using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Aurora.Api.Controllers;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aurora.Api.Tests.Controllers;

public class WellnessControllerTests
{
    private readonly Mock<IWellnessInsightsService> _wellnessServiceMock = new();
    private readonly Mock<ILogger<WellnessController>> _loggerMock = new();
    private readonly WellnessController _controller;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public WellnessControllerTests()
    {
        _controller = new WellnessController(_wellnessServiceMock.Object, _loggerMock.Object);
        SetAuthenticatedUser(_currentUserId);
    }

    private void SetAuthenticatedUser(Guid userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public void Constructor_WithNullService_ShouldThrow()
    {
        var act = () => new WellnessController(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("wellnessInsightsService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new WellnessController(_wellnessServiceMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetMonthlySummary_WithInvalidMonth_ShouldReturnBadRequest()
    {
        var result = await _controller.GetMonthlySummary(year: 2025, month: 13);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var problem = result.Result.As<BadRequestObjectResult>().Value.As<ProblemDetails>();
        problem.Detail.Should().Contain("mes");

        _wellnessServiceMock.Verify(service => service.GetMonthlySummaryAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMonthlySummary_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var controller = new WellnessController(_wellnessServiceMock.Object, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.GetMonthlySummary(2025, 5);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetMonthlySummary_WithValidRequest_ShouldReturnSummary()
    {
        const int year = 2025;
        const int month = 5;
        var summary = new WellnessSummaryDto
        {
            Year = year,
            Month = month,
            AverageMood = 4.2,
            MoodTrend = Array.Empty<MoodTrendPointDto>(),
            MoodDistribution = Array.Empty<MoodDistributionSliceDto>(),
            CategoryImpacts = Array.Empty<CategoryMoodImpactDto>(),
            Streaks = new MoodStreaksDto(),
            TrackingCoverage = 0.5
        };

        _wellnessServiceMock
            .Setup(service => service.GetMonthlySummaryAsync(It.Is<Guid?>(id => id.HasValue && id.Value == _currentUserId), year, month))
            .ReturnsAsync(summary);

        var result = await _controller.GetMonthlySummary(year, month);

        result.Result.Should().BeOfType<OkObjectResult>();
        result.Result.As<OkObjectResult>().Value.Should().BeEquivalentTo(summary);
    }

    [Fact]
    public async Task GetMonthlySummary_WhenServiceThrows_ShouldReturnInternalServerError()
    {
        const int year = 2025;
        const int month = 6;

        _wellnessServiceMock
            .Setup(service => service.GetMonthlySummaryAsync(It.IsAny<Guid?>(), year, month))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var result = await _controller.GetMonthlySummary(year, month);

        result.Result.Should().BeOfType<ObjectResult>();
        result.Result.As<ObjectResult>().StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
