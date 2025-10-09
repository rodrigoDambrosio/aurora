using Aurora.Application.DTOs;
using Aurora.Application.Validators;
using FluentAssertions;

namespace Aurora.Application.Tests.Validators;

public class CreateEventDtoValidatorTests
{
    private readonly CreateEventDtoValidator _validator;

    public CreateEventDtoValidatorTests()
    {
        _validator = new CreateEventDtoValidator();
    }

    [Fact]
    public void Validate_WithValidDto_ShouldBeValid()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Reunión importante",
            Description = "Descripción del evento",
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.NewGuid(),
            IsAllDay = false,
            Location = "Sala de juntas",
            Notes = "Notas del evento"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithInvalidTitle_ShouldBeInvalid(string? title)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = title!,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateEventDto.Title));
    }

    [Fact]
    public void Validate_WithTitleTooLong_ShouldBeInvalid()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = new string('A', 201), // Más de 200 caracteres
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateEventDto.Title));
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldBeInvalid()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Título válido",
            Description = new string('A', 1001), // Más de 1000 caracteres
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateEventDto.Description));
    }

    [Fact]
    public void Validate_WithEndDateBeforeStartDate_ShouldBeInvalid()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Evento",
            StartDate = DateTime.Now.AddDays(1).AddHours(2),
            EndDate = DateTime.Now.AddDays(1).AddHours(1), // Antes de la fecha de inicio
            EventCategoryId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateEventDto.EndDate));
    }

    [Fact]
    public void Validate_WithEmptyEventCategoryId_ShouldBeInvalid()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Evento",
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.Empty
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateEventDto.EventCategoryId));
    }

    [Fact]
    public void Validate_WithLocationTooLong_ShouldBeInvalid()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Evento",
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.NewGuid(),
            Location = new string('A', 501) // Más de 500 caracteres
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateEventDto.Location));
    }

    [Theory]
    [InlineData("#FF0000")] // Valid hex color
    [InlineData("#123456")] // Valid hex color
    [InlineData("")]        // Empty is valid (optional)
    [InlineData(null)]      // Null is valid (optional)
    public void Validate_WithValidColor_ShouldBeValid(string? color)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Evento",
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.NewGuid(),
            Color = color
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("FF0000")]   // Missing #
    [InlineData("#FF")]      // Too short
    [InlineData("#GGGGGG")]  // Invalid hex characters
    public void Validate_WithInvalidColor_ShouldBeInvalid(string color)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Evento",
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.NewGuid(),
            Color = color
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateEventDto.Color));
    }

    [Fact]
    public void Validate_WithNotesTooLong_ShouldBeInvalid()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Evento",
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(1).AddHours(1),
            EventCategoryId = Guid.NewGuid(),
            Notes = new string('A', 1001) // Más de 1000 caracteres
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateEventDto.Notes));
    }
}