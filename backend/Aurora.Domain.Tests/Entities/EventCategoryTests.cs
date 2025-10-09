using Aurora.Domain.Entities;
using FluentAssertions;

namespace Aurora.Domain.Tests.Entities;

public class EventCategoryTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var category = new EventCategory();

        // Assert
        category.Name.Should().Be(string.Empty);
        category.IsActive.Should().BeTrue();
        category.IsSystemDefault.Should().BeFalse();
        category.SortOrder.Should().Be(0);
    }

    [Theory]
    [InlineData("Trabajo", "Eventos relacionados con el trabajo", "#1447e6", "briefcase")]
    [InlineData("Personal", "Eventos personales", "#ca3500", "user")]
    public void SetProperties_ShouldSetCorrectValues(string name, string description, string color, string icon)
    {
        // Arrange
        var category = new EventCategory();

        // Act
        category.Name = name;
        category.Description = description;
        category.Color = color;
        category.Icon = icon;

        // Assert
        category.Name.Should().Be(name);
        category.Description.Should().Be(description);
        category.Color.Should().Be(color);
        category.Icon.Should().Be(icon);
    }

    [Fact]
    public void IsAvailableForUser_WithMatchingUserId_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = new EventCategory
        {
            UserId = userId,
            IsActive = true
        };

        // Act
        var result = category.IsAvailableForUser(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAvailableForUser_WithSystemDefaultCategory_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = new EventCategory
        {
            UserId = Guid.NewGuid(), // Different user
            IsActive = true,
            IsSystemDefault = true
        };

        // Act
        var result = category.IsAvailableForUser(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAvailableForUser_WithInactiveCategory_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = new EventCategory
        {
            UserId = userId,
            IsActive = false
        };

        // Act
        var result = category.IsAvailableForUser(userId, allowAnonymousAccess: false);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAvailableForUser_WithDifferentUserAndNotSystemDefault_ShouldReturnFalse()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var category = new EventCategory
        {
            UserId = userId1,
            IsActive = true,
            IsSystemDefault = false
        };

        // Act
        var result = category.IsAvailableForUser(userId2, allowAnonymousAccess: false);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCustomCategory_WithUserIdAndNotSystemDefault_ShouldReturnTrue()
    {
        // Arrange
        var category = new EventCategory
        {
            UserId = Guid.NewGuid(),
            IsSystemDefault = false
        };

        // Act
        var result = category.IsCustomCategory;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCustomCategory_WithSystemDefault_ShouldReturnFalse()
    {
        // Arrange
        var category = new EventCategory
        {
            UserId = Guid.NewGuid(),
            IsSystemDefault = true
        };

        // Act
        var result = category.IsCustomCategory;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSystemCategory_WithSystemDefault_ShouldReturnTrue()
    {
        // Arrange
        var category = new EventCategory
        {
            IsSystemDefault = true
        };

        // Act
        var result = category.IsSystemCategory;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSystemCategory_WithoutUserId_ShouldReturnTrue()
    {
        // Arrange
        var category = new EventCategory
        {
            UserId = null,
            IsSystemDefault = false
        };

        // Act
        var result = category.IsSystemCategory;

        // Assert
        result.Should().BeTrue();
    }
}