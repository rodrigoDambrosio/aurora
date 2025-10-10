using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Application.Services;
using Aurora.Domain.Entities;
using AutoFixture;
using FluentAssertions;
using Moq;

namespace Aurora.Application.Tests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IEventCategoryRepository> _categoryRepositoryMock;
    private readonly EventService _eventService;
    private readonly Fixture _fixture;

    public EventServiceTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _categoryRepositoryMock = new Mock<IEventCategoryRepository>();
        _eventService = new EventService(_eventRepositoryMock.Object, _categoryRepositoryMock.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public void Constructor_WithNullEventRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EventService(null!, _categoryRepositoryMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventRepository");
    }

    [Fact]
    public void Constructor_WithNullCategoryRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EventService(_eventRepositoryMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("categoryRepository");
    }

    [Fact]
    public async Task GetWeeklyEventsAsync_WithValidData_ShouldReturnWeeklyEventsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var weekStart = new DateTime(2025, 1, 6); // Monday
        var weekEnd = weekStart.AddDays(7).AddTicks(-1);

        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6"
        };

        var events = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Reunión",
                StartDate = weekStart.AddHours(9),
                EndDate = weekStart.AddHours(10),
                EventCategoryId = category.Id,
                EventCategory = category,
                UserId = userId
            }
        };

        var categories = new List<EventCategory> { category };

        _eventRepositoryMock
            .Setup(x => x.GetWeeklyEventsAsync(It.IsAny<Guid>(), weekStart, It.IsAny<Guid?>()))
            .ReturnsAsync(events);

        _categoryRepositoryMock
            .Setup(x => x.GetAvailableCategoriesForUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _eventService.GetWeeklyEventsAsync(userId, weekStart);

        // Assert
        result.Should().NotBeNull();
        result.Events.Should().HaveCount(1);
        result.Categories.Should().HaveCount(1);
        result.WeekStart.Should().Be(weekStart);
        result.WeekEnd.Should().Be(weekEnd);
        result.TotalEvents.Should().Be(1);
        result.HasMoreEvents.Should().BeFalse();

        var eventDto = result.Events.First();
        eventDto.Title.Should().Be("Reunión");
        eventDto.EventCategory.Should().NotBeNull();
        eventDto.EventCategory!.Name.Should().Be("Trabajo");
    }

    [Fact]
    public async Task GetEventAsync_WithValidEventId_ShouldReturnEventDto()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Personal"
        };

        var eventEntity = new Event
        {
            Id = eventId,
            Title = "Cita médica",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            UserId = userId,
            EventCategory = category
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _eventService.GetEventAsync(eventId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventId);
        result.Title.Should().Be("Cita médica");
    }

    [Fact]
    public async Task GetEventAsync_WithNonExistentEventId_ShouldReturnNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _eventService.GetEventAsync(eventId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEventAsync_WhenEventBelongsToAnotherUser_ShouldReturnNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            UserId = anotherUserId
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _eventService.GetEventAsync(eventId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEventAsync_WithValidData_ShouldCreateAndReturnEventDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var createEventDto = new CreateEventDto
        {
            Title = "Nueva reunión",
            Description = "Reunión importante",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            EventCategoryId = categoryId
        };

        var category = new EventCategory
        {
            Id = categoryId,
            Name = "Trabajo"
        };

        var createdEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = createEventDto.Title,
            Description = createEventDto.Description,
            StartDate = createEventDto.StartDate,
            EndDate = createEventDto.EndDate,
            EventCategoryId = categoryId,
            EventCategory = category,
            UserId = userId
        };

        _categoryRepositoryMock
            .Setup(x => x.UserCanUseCategoryAsync(categoryId, It.IsAny<Guid>()))
            .ReturnsAsync(true);

        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Event>()))
            .ReturnsAsync(createdEvent);

        _eventRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _eventService.CreateEventAsync(userId, createEventDto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Nueva reunión");
        result.Description.Should().Be("Reunión importante");

        _eventRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Event>()), Times.Once);
        _eventRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_WithInvalidCategory_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var createEventDto = new CreateEventDto
        {
            Title = "Nueva reunión",
            EventCategoryId = categoryId
        };

        _categoryRepositoryMock
            .Setup(x => x.UserCanUseCategoryAsync(categoryId, It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act & Assert
        var act = async () => await _eventService.CreateEventAsync(userId, createEventDto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("La categoría especificada no está disponible para este usuario");
    }

    [Fact]
    public async Task UpdateEventAsync_WithValidData_ShouldUpdateAndReturnEventDto()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var updateEventDto = new CreateEventDto
        {
            Title = "Reunión actualizada",
            EventCategoryId = categoryId
        };

        var existingEvent = new Event
        {
            Id = eventId,
            Title = "Reunión original",
            UserId = userId,
            EventCategoryId = categoryId
        };

        var updatedEvent = new Event
        {
            Id = eventId,
            Title = updateEventDto.Title,
            UserId = userId,
            EventCategoryId = categoryId
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(existingEvent);

        _categoryRepositoryMock
            .Setup(x => x.UserCanUseCategoryAsync(categoryId, It.IsAny<Guid>()))
            .ReturnsAsync(true);

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Event>()))
            .ReturnsAsync(updatedEvent);

        _eventRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _eventService.UpdateEventAsync(eventId, userId, updateEventDto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Reunión actualizada");

        _eventRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>()), Times.Once);
        _eventRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_WithNonExistentEvent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateEventDto = new CreateEventDto { Title = "Test" };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync((Event?)null);

        // Act & Assert
        var act = async () => await _eventService.UpdateEventAsync(eventId, userId, updateEventDto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Evento no encontrado o sin permisos para modificarlo");
    }

    [Fact]
    public async Task DeleteEventAsync_WithValidEventId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            UserId = userId
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        _eventRepositoryMock
            .Setup(x => x.DeleteAsync(eventId))
            .ReturnsAsync(true);

        _eventRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _eventService.DeleteEventAsync(eventId, userId);

        // Assert
        result.Should().BeTrue();

        _eventRepositoryMock.Verify(x => x.DeleteAsync(eventId), Times.Once);
        _eventRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteEventAsync_WithNonExistentEvent_ShouldReturnFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _eventService.DeleteEventAsync(eventId, userId);

        // Assert
        result.Should().BeFalse();

        _eventRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        _eventRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetConflictingEventsAsync_WithOverlappingEvents_ShouldReturnConflictingEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = DateTime.Now;
        var endDate = DateTime.Now.AddHours(1);

        var overlappingEvents = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento en conflicto",
                StartDate = startDate.AddMinutes(30),
                EndDate = endDate.AddMinutes(30),
                UserId = userId
            }
        };

        _eventRepositoryMock
            .Setup(x => x.GetOverlappingEventsAsync(It.IsAny<Guid>(), startDate, endDate))
            .ReturnsAsync(overlappingEvents);

        // Act
        var result = await _eventService.GetConflictingEventsAsync(userId, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Evento en conflicto");
    }
}