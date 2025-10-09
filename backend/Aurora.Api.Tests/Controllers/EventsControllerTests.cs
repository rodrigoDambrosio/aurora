using Aurora.Api.Controllers;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aurora.Api.Tests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<ILogger<EventsController>> _loggerMock;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _loggerMock = new Mock<ILogger<EventsController>>();
        _controller = new EventsController(_eventServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullEventService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EventsController(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EventsController(_eventServiceMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetWeeklyEvents_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var request = new WeeklyEventsRequestDto
        {
            WeekStart = new DateTime(2024, 1, 1)
        };

        var response = new WeeklyEventsResponseDto
        {
            WeekStart = request.WeekStart,
            Events = new List<EventDto>
            {
                new EventDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Event",
                    StartDate = request.WeekStart.AddHours(10),
                    EndDate = request.WeekStart.AddHours(11)
                }
            }
        };

        _eventServiceMock
            .Setup(x => x.GetWeeklyEventsAsync(It.IsAny<Guid?>(), request.WeekStart))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetWeeklyEvents(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetEvent_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDto = new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1)
        };

        _eventServiceMock
            .Setup(x => x.GetEventAsync(eventId, It.IsAny<Guid?>()))
            .ReturnsAsync(eventDto);

        // Act
        var result = await _controller.GetEvent(eventId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(eventDto);
    }

    [Fact]
    public async Task GetEvent_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventServiceMock
            .Setup(x => x.GetEventAsync(eventId, It.IsAny<Guid?>()))
            .ReturnsAsync((EventDto?)null);

        // Act
        var result = await _controller.GetEvent(eventId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateEvent_WithValidDto_ShouldReturnCreatedResult()
    {
        // Arrange
        var createEventDto = new CreateEventDto
        {
            Title = "New Event",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            EventCategoryId = Guid.NewGuid()
        };

        var createdEventDto = new EventDto
        {
            Id = Guid.NewGuid(),
            Title = createEventDto.Title,
            StartDate = createEventDto.StartDate,
            EndDate = createEventDto.EndDate,
            EventCategoryId = createEventDto.EventCategoryId
        };

        _eventServiceMock
            .Setup(x => x.CreateEventAsync(It.IsAny<Guid?>(), createEventDto))
            .ReturnsAsync(createdEventDto);

        // Act
        var result = await _controller.CreateEvent(createEventDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdEventDto);
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateEvent_WhenServiceThrowsException_ShouldReturnBadRequest()
    {
        // Arrange
        var createEventDto = new CreateEventDto
        {
            Title = "Test Event",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            EventCategoryId = Guid.NewGuid()
        };

        _eventServiceMock
            .Setup(x => x.CreateEventAsync(It.IsAny<Guid?>(), createEventDto))
            .ThrowsAsync(new ArgumentException("Invalid category"));

        // Act
        var result = await _controller.CreateEvent(createEventDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateEvent_WithValidData_ShouldReturnOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateEventDto = new CreateEventDto
        {
            Title = "Updated Event",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            EventCategoryId = Guid.NewGuid()
        };

        var updatedEventDto = new EventDto
        {
            Id = eventId,
            Title = updateEventDto.Title,
            StartDate = updateEventDto.StartDate,
            EndDate = updateEventDto.EndDate,
            EventCategoryId = updateEventDto.EventCategoryId
        };

        _eventServiceMock
            .Setup(x => x.UpdateEventAsync(eventId, It.IsAny<Guid?>(), updateEventDto))
            .ReturnsAsync(updatedEventDto);

        // Act
        var result = await _controller.UpdateEvent(eventId, updateEventDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(updatedEventDto);
    }

    [Fact]
    public async Task UpdateEvent_WhenServiceThrowsException_ShouldReturnBadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateEventDto = new CreateEventDto
        {
            Title = "Test Event",
            EventCategoryId = Guid.NewGuid()
        };

        _eventServiceMock
            .Setup(x => x.UpdateEventAsync(eventId, It.IsAny<Guid?>(), updateEventDto))
            .ThrowsAsync(new ArgumentException("Invalid event"));

        // Act
        var result = await _controller.UpdateEvent(eventId, updateEventDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteEvent_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventServiceMock
            .Setup(x => x.DeleteEventAsync(eventId, It.IsAny<Guid?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _eventServiceMock.Verify(x => x.DeleteEventAsync(eventId, It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEvent_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventServiceMock
            .Setup(x => x.DeleteEventAsync(eventId, It.IsAny<Guid?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();

        _eventServiceMock.Verify(x => x.DeleteEventAsync(eventId, It.IsAny<Guid?>()), Times.Once);
    }
}