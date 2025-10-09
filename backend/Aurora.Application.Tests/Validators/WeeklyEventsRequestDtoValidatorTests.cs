using Aurora.Application.DTOs;
using Aurora.Application.Validators;
using FluentAssertions;

namespace Aurora.Application.Tests.Validators;

public class WeeklyEventsRequestDtoValidatorTests
{
    private readonly WeeklyEventsRequestDtoValidator _validator;

    public WeeklyEventsRequestDtoValidatorTests()
    {
        _validator = new WeeklyEventsRequestDtoValidator();
    }

    [Fact]
    public void Validate_WithValidWeekStart_ShouldBeValid()
    {
        // Arrange
        var dto = new WeeklyEventsRequestDto
        {
            WeekStart = new DateTime(2025, 1, 6) // Valid date
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDefaultWeekStart_ShouldBeValid()
    {
        // Arrange
        var dto = new WeeklyEventsRequestDto
        {
            WeekStart = DateTime.MinValue // Default value should be valid
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithFutureWeekStart_ShouldBeValid()
    {
        // Arrange
        var dto = new WeeklyEventsRequestDto
        {
            WeekStart = DateTime.Now.AddDays(30) // Future date
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithPastWeekStart_ShouldBeValid()
    {
        // Arrange
        var dto = new WeeklyEventsRequestDto
        {
            WeekStart = DateTime.Now.AddDays(-30) // Past date
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithLeapYearDate_ShouldBeValid()
    {
        // Arrange
        var dto = new WeeklyEventsRequestDto
        {
            WeekStart = new DateTime(2024, 2, 29) // Valid leap year date
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithVariousFlags_ShouldBeValid()
    {
        // Arrange
        var dto = new WeeklyEventsRequestDto
        {
            WeekStart = DateTime.Now,
            UserId = Guid.NewGuid(),
            IncludeCategories = false,
            IncludeAllDayEvents = false,
            IncludeRecurringEvents = false
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}