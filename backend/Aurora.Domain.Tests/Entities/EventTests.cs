using Aurora.Domain.Entities;
using FluentAssertions;

namespace Aurora.Domain.Tests.Entities;

public class EventTests
{
    [Fact]
    public void IsValidDateRange_WhenStartDateIsBeforeEndDate_ShouldReturnTrue()
    {
        // Arrange
        var eventEntity = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 10, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 11, 0, 0)
        };

        // Act
        var result = eventEntity.IsValidDateRange();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidDateRange_WhenStartDateIsAfterEndDate_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 11, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 10, 0, 0)
        };

        // Act
        var result = eventEntity.IsValidDateRange();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidDateRange_WhenStartDateEqualsEndDate_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 10, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 10, 0, 0)
        };

        // Act
        var result = eventEntity.IsValidDateRange();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(60)] // 1 hour
    [InlineData(30)] // 30 minutes  
    [InlineData(120)] // 2 hours
    public void GetDurationInMinutes_ShouldReturnCorrectDuration(int expectedMinutes)
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1, 10, 0, 0);
        var endDate = startDate.AddMinutes(expectedMinutes);

        var eventEntity = new Event
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = eventEntity.GetDurationInMinutes();

        // Assert
        result.Should().Be(expectedMinutes);
    }

    [Theory]
    [InlineData("2025-01-01", "2025-01-01", true)] // Same day
    [InlineData("2025-01-01", "2025-01-02", false)] // Different day
    public void OccursOnDate_WithAllDayEvent_ShouldReturnExpectedResult(string eventDateStr, string targetDateStr, bool expected)
    {
        // Arrange
        var eventDate = DateTime.Parse(eventDateStr);
        var targetDate = DateTime.Parse(targetDateStr);

        var eventEntity = new Event
        {
            StartDate = eventDate,
            EndDate = eventDate.AddHours(1),
            IsAllDay = true
        };

        // Act
        var result = eventEntity.OccursOnDate(targetDate);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void OccursOnDate_WithMultiDayEvent_ShouldReturnTrueForDateInRange()
    {
        // Arrange
        var eventEntity = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 10, 0, 0),
            EndDate = new DateTime(2025, 1, 3, 10, 0, 0),
            IsAllDay = false
        };

        // Act & Assert
        eventEntity.OccursOnDate(new DateTime(2025, 1, 1)).Should().BeTrue();
        eventEntity.OccursOnDate(new DateTime(2025, 1, 2)).Should().BeTrue();
        eventEntity.OccursOnDate(new DateTime(2025, 1, 3)).Should().BeTrue();
        eventEntity.OccursOnDate(new DateTime(2025, 1, 4)).Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_WhenEventsOverlap_ShouldReturnTrue()
    {
        // Arrange
        var event1 = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 10, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 12, 0, 0)
        };

        var event2 = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 11, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 13, 0, 0)
        };

        // Act
        var result = event1.OverlapsWith(event2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_WhenEventsDoNotOverlap_ShouldReturnFalse()
    {
        // Arrange
        var event1 = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 10, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 11, 0, 0)
        };

        var event2 = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 12, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 13, 0, 0)
        };

        // Act
        var result = event1.OverlapsWith(event2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_WhenEventsAreAdjacent_ShouldReturnFalse()
    {
        // Arrange
        var event1 = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 10, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 11, 0, 0)
        };

        var event2 = new Event
        {
            StartDate = new DateTime(2025, 1, 1, 11, 0, 0),
            EndDate = new DateTime(2025, 1, 1, 12, 0, 0)
        };

        // Act
        var result = event1.OverlapsWith(event2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void BelongsToUser_WhenUserIdMatches_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventEntity = new Event
        {
            UserId = userId
        };

        // Act
        var result = eventEntity.BelongsToUser(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void BelongsToUser_WhenUserIdDoesNotMatch_ShouldReturnFalse()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var eventEntity = new Event
        {
            UserId = userId1
        };

        // Act
        var result = eventEntity.BelongsToUser(userId2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetOwner_WithSpecificUserId_ShouldSetCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventEntity = new Event();

        // Act
        eventEntity.SetOwner(userId);

        // Assert
        eventEntity.UserId.Should().Be(userId);
    }

    [Fact]
    public void SetOwner_WithNullUserId_ShouldSetDemoUserId()
    {
        // Arrange
        var eventEntity = new Event();

        // Act
        eventEntity.SetOwner(null);

        // Assert
        eventEntity.UserId.Should().Be(Aurora.Domain.Constants.DomainConstants.DemoUser.Id);
    }
}