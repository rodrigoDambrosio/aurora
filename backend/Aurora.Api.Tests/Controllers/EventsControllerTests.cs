using Aurora.Api.Controllers;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Aurora.Api.Tests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<IAIValidationService> _aiValidationServiceMock;
    private readonly Mock<IEventCategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ILogger<EventsController>> _loggerMock;
    private readonly EventsController _controller;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public EventsControllerTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _aiValidationServiceMock = new Mock<IAIValidationService>();
        _categoryRepositoryMock = new Mock<IEventCategoryRepository>();
        _loggerMock = new Mock<ILogger<EventsController>>();
        _controller = new EventsController(_eventServiceMock.Object, _aiValidationServiceMock.Object, _categoryRepositoryMock.Object, _loggerMock.Object);
        SetAuthenticatedUser(_currentUserId);
    }

    private void SetAuthenticatedUser(Guid userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private bool BeCurrentUser(Guid? userId) => userId.HasValue && userId.Value == _currentUserId;

    private bool BeCurrentUser(Guid userId) => userId == _currentUserId;

    [Fact]
    public void Constructor_WithNullEventService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EventsController(null!, _aiValidationServiceMock.Object, _categoryRepositoryMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventService");
    }

    [Fact]
    public void Constructor_WithNullAIValidationService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EventsController(_eventServiceMock.Object, null!, _categoryRepositoryMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("aiValidationService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EventsController(_eventServiceMock.Object, _aiValidationServiceMock.Object, _categoryRepositoryMock.Object, null!);
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
            .Setup(x => x.GetWeeklyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), request.WeekStart, null))
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
            .Setup(x => x.GetEventAsync(eventId, It.Is<Guid?>(userId => BeCurrentUser(userId))))
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
            .Setup(x => x.GetEventAsync(eventId, It.Is<Guid?>(userId => BeCurrentUser(userId))))
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
            .Setup(x => x.CreateEventAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), createEventDto))
            .ReturnsAsync(createdEventDto);

        // Act
        var result = await _controller.CreateEvent(createEventDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdEventDto);
        createdResult.StatusCode.Should().Be(201);

        _aiValidationServiceMock.Verify(
            x => x.ValidateEventCreationAsync(
                It.IsAny<CreateEventDto>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<EventDto>>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEvent_ShouldNotCallAIValidationService()
    {
        // Arrange
        var createEventDto = new CreateEventDto
        {
            Title = "Party Event",
            StartDate = DateTime.Now.Date.AddHours(2), // 2 AM
            EndDate = DateTime.Now.Date.AddHours(6),
            EventCategoryId = Guid.NewGuid()
        };

        var createdEvent = new EventDto
        {
            Id = Guid.NewGuid(),
            Title = createEventDto.Title,
            StartDate = createEventDto.StartDate,
            EndDate = createEventDto.EndDate,
            EventCategoryId = createEventDto.EventCategoryId
        };

        _eventServiceMock
            .Setup(x => x.CreateEventAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), It.IsAny<CreateEventDto>()))
            .ReturnsAsync(createdEvent);

        // Act
        var result = await _controller.CreateEvent(createEventDto);

        // Assert - El evento se crea a pesar de la advertencia de la IA
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdEvent);

        _aiValidationServiceMock.Verify(
            x => x.ValidateEventCreationAsync(
                It.IsAny<CreateEventDto>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<EventDto>>()),
            Times.Never);
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
            .Setup(x => x.CreateEventAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), createEventDto))
            .ThrowsAsync(new ArgumentException("Invalid category"));

        // Act
        var result = await _controller.CreateEvent(createEventDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        _aiValidationServiceMock.Verify(
            x => x.ValidateEventCreationAsync(
                It.IsAny<CreateEventDto>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<EventDto>>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateEvent_WithValidData_ShouldReturnOkResult()
    {
        // Arrange
        var createEventDto = new CreateEventDto
        {
            Title = "Evento para validar",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(1),
            EventCategoryId = Guid.NewGuid()
        };

        var existingEvents = new List<EventDto>
        {
            new EventDto
            {
                Id = Guid.NewGuid(),
                Title = "Otro evento",
                StartDate = createEventDto.StartDate.AddHours(-2),
                EndDate = createEventDto.StartDate.AddHours(-1),
                EventCategoryId = Guid.NewGuid()
            }
        };

        var validationResult = new AIValidationResult
        {
            IsApproved = true,
            Severity = AIValidationSeverity.Info,
            RecommendationMessage = "Todo en orden",
            UsedAi = true
        };

        _eventServiceMock
            .Setup(x => x.GetEventsByDateRangeAsync(
                It.Is<Guid?>(userId => BeCurrentUser(userId)),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(existingEvents);

        _aiValidationServiceMock
            .Setup(x => x.ValidateEventCreationAsync(createEventDto, It.Is<Guid>(userId => BeCurrentUser(userId)), existingEvents))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateEvent(createEventDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(validationResult);

        _eventServiceMock.Verify(
            x => x.GetEventsByDateRangeAsync(
                It.Is<Guid?>(userId => BeCurrentUser(userId)),
                createEventDto.StartDate.AddDays(-1),
                createEventDto.StartDate.AddDays(7)),
            Times.Once);

        _aiValidationServiceMock.Verify(
            x => x.ValidateEventCreationAsync(createEventDto, It.Is<Guid>(userId => BeCurrentUser(userId)), existingEvents),
            Times.Once);
    }

    [Fact]
    public async Task ValidateEvent_WhenAiThrows_ShouldReturnFallbackValidation()
    {
        // Arrange
        var createEventDto = new CreateEventDto
        {
            Title = "Evento posible solapado",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(2),
            EventCategoryId = Guid.NewGuid()
        };

        var existingEvents = new List<EventDto>
        {
            new EventDto
            {
                Id = Guid.NewGuid(),
                Title = "Evento existente",
                StartDate = createEventDto.StartDate.AddMinutes(-30),
                EndDate = createEventDto.EndDate.AddMinutes(30),
                EventCategoryId = Guid.NewGuid()
            }
        };

        _eventServiceMock
            .Setup(x => x.GetEventsByDateRangeAsync(
                It.Is<Guid?>(userId => BeCurrentUser(userId)),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(existingEvents);

        _aiValidationServiceMock
            .Setup(x => x.ValidateEventCreationAsync(createEventDto, It.Is<Guid>(userId => BeCurrentUser(userId)), existingEvents))
            .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

        // Act
        var result = await _controller.ValidateEvent(createEventDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var fallback = okResult!.Value as AIValidationResult;

        fallback.Should().NotBeNull();
        fallback!.UsedAi.Should().BeFalse();
        fallback.Severity.Should().Be(AIValidationSeverity.Warning);
        fallback.IsApproved.Should().BeFalse();
        fallback.RecommendationMessage.Should().Contain("solapamiento");
        fallback.Suggestions.Should().NotBeNull();
        fallback.Suggestions.Should().NotBeEmpty();

        _aiValidationServiceMock.Verify(
            x => x.ValidateEventCreationAsync(createEventDto, It.Is<Guid>(userId => BeCurrentUser(userId)), existingEvents),
            Times.Once);
    }

    [Fact]
    public async Task ParseFromText_WhenAiReturnsAnalysis_ShouldReturnOkWithAiDetails()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var parseRequest = new ParseNaturalLanguageRequestDto
        {
            Text = "Reunión mañana a las 10",
            TimezoneOffsetMinutes = -180
        };

        var availableCategories = new List<EventCategory>
        {
            new()
            {
                Id = categoryId,
                Name = "Trabajo",
                Color = "#3366FF",
                IsSystemDefault = true,
                SortOrder = 1
            }
        };

        _categoryRepositoryMock
            .Setup(repo => repo.GetAvailableCategoriesForUserAsync(It.Is<Guid>(userId => BeCurrentUser(userId))))
            .ReturnsAsync(availableCategories);

        _eventServiceMock
            .Setup(service => service.GetEventsByDateRangeAsync(
                It.Is<Guid?>(userId => BeCurrentUser(userId)),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EventDto>());

        var aiEvent = new CreateEventDto
        {
            Title = "Reunión con equipo",
            Description = "Planificación semanal",
            StartDate = DateTime.UtcNow.AddHours(4),
            EndDate = DateTime.UtcNow.AddHours(5),
            EventCategoryId = categoryId,
            Priority = EventPriority.Medium,
            TimezoneOffsetMinutes = -180
        };

        var aiValidation = new AIValidationResult
        {
            IsApproved = true,
            Severity = AIValidationSeverity.Info,
            RecommendationMessage = "Agenda libre, continúa",
            Suggestions = new List<string> { "Prepara la agenda" },
            UsedAi = true
        };

        _aiValidationServiceMock
            .Setup(service => service.ParseNaturalLanguageAsync(
                parseRequest.Text,
                It.Is<Guid>(userId => BeCurrentUser(userId)),
                It.IsAny<IEnumerable<EventCategoryDto>>(),
                parseRequest.TimezoneOffsetMinutes,
                It.IsAny<IEnumerable<EventDto>>()))
            .ReturnsAsync(new ParseNaturalLanguageResponseDto
            {
                Success = true,
                Event = aiEvent,
                Validation = aiValidation
            });

        // Act
        var response = await _controller.ParseFromText(parseRequest);

        // Assert
        response.Result.Should().BeOfType<OkObjectResult>();
        var ok = response.Result as OkObjectResult;
        ok!.Value.Should().BeOfType<ParseNaturalLanguageResponseDto>();
        var payload = ok.Value as ParseNaturalLanguageResponseDto;

        payload!.Success.Should().BeTrue();
        payload.Event.Should().BeEquivalentTo(aiEvent);
        payload.Validation.Should().BeEquivalentTo(aiValidation);

        _aiValidationServiceMock.Verify(
            service => service.ValidateEventCreationAsync(
                It.IsAny<CreateEventDto>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<EventDto>>()),
            Times.Never);
    }

    [Fact]
    public async Task ParseFromText_WhenAiOmitsAnalysis_ShouldFallbackToBasicValidation()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var parseRequest = new ParseNaturalLanguageRequestDto
        {
            Text = "Evento sin análisis",
            TimezoneOffsetMinutes = 0
        };

        var availableCategories = new List<EventCategory>
        {
            new()
            {
                Id = categoryId,
                Name = "Personal",
                Color = "#FF7766",
                IsSystemDefault = true,
                SortOrder = 1
            }
        };

        _categoryRepositoryMock
            .Setup(repo => repo.GetAvailableCategoriesForUserAsync(It.Is<Guid>(userId => BeCurrentUser(userId))))
            .ReturnsAsync(availableCategories);

        var parsedEventStart = DateTime.UtcNow.AddHours(1);
        var parsedEventEnd = parsedEventStart.AddHours(2);

        var overlappingEvent = new EventDto
        {
            Id = Guid.NewGuid(),
            Title = "Evento existente",
            StartDate = parsedEventStart.AddMinutes(-30),
            EndDate = parsedEventEnd.AddMinutes(30),
            EventCategoryId = categoryId,
            EventCategory = new EventCategoryDto
            {
                Id = categoryId,
                Name = "Personal",
                Color = "#FF7766",
                SortOrder = 1,
                IsSystemDefault = true
            }
        };

        _eventServiceMock
            .Setup(service => service.GetEventsByDateRangeAsync(
                It.Is<Guid?>(userId => BeCurrentUser(userId)),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EventDto> { overlappingEvent });

        var aiEvent = new CreateEventDto
        {
            Title = "Nuevo evento",
            StartDate = parsedEventStart,
            EndDate = parsedEventEnd,
            EventCategoryId = categoryId,
            Priority = EventPriority.Medium
        };

        _aiValidationServiceMock
            .Setup(service => service.ParseNaturalLanguageAsync(
                parseRequest.Text,
                It.Is<Guid>(userId => BeCurrentUser(userId)),
                It.IsAny<IEnumerable<EventCategoryDto>>(),
                parseRequest.TimezoneOffsetMinutes,
                It.IsAny<IEnumerable<EventDto>>()))
            .ReturnsAsync(new ParseNaturalLanguageResponseDto
            {
                Success = true,
                Event = aiEvent,
                Validation = null
            });

        // Act
        var response = await _controller.ParseFromText(parseRequest);

        // Assert
        response.Result.Should().BeOfType<OkObjectResult>();
        var ok = response.Result as OkObjectResult;
        var payload = ok!.Value as ParseNaturalLanguageResponseDto;

        payload!.Validation.Should().NotBeNull();
        payload.Validation!.UsedAi.Should().BeFalse();
        payload.Validation.Severity.Should().Be(AIValidationSeverity.Warning);
        payload.Validation.IsApproved.Should().BeFalse();
        payload.Validation.RecommendationMessage.Should().Contain("solapamiento");

        _aiValidationServiceMock.Verify(
            service => service.ValidateEventCreationAsync(
                It.IsAny<CreateEventDto>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<EventDto>>()),
            Times.Never);
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
            .Setup(x => x.UpdateEventAsync(eventId, It.Is<Guid?>(userId => BeCurrentUser(userId)), updateEventDto))
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
            .Setup(x => x.UpdateEventAsync(eventId, It.Is<Guid?>(userId => BeCurrentUser(userId)), updateEventDto))
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
            .Setup(x => x.DeleteEventAsync(eventId, It.Is<Guid?>(userId => BeCurrentUser(userId))))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _eventServiceMock.Verify(x => x.DeleteEventAsync(eventId, It.Is<Guid?>(userId => BeCurrentUser(userId))), Times.Once);
    }

    [Fact]
    public async Task DeleteEvent_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventServiceMock
            .Setup(x => x.DeleteEventAsync(eventId, It.Is<Guid?>(userId => BeCurrentUser(userId))))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();

        _eventServiceMock.Verify(x => x.DeleteEventAsync(eventId, It.Is<Guid?>(userId => BeCurrentUser(userId))), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyEvents_WithDefaultParameters_ShouldReturnOkWithCurrentMonthEvents()
    {
        // Arrange
        var currentDate = DateTime.Now;
        var response = new WeeklyEventsResponseDto
        {
            Categories = new List<EventCategoryDto>
            {
                new EventCategoryDto { Id = Guid.NewGuid(), Name = "Trabajo", Color = "#2b7fff" }
            },
            Events = new List<EventDto>
            {
                new EventDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Event",
                    StartDate = currentDate,
                    EndDate = currentDate.AddHours(1),
                    EventCategory = new EventCategoryDto { Id = Guid.NewGuid(), Name = "Trabajo", Color = "#2b7fff" }
                }
            }
        };

        _eventServiceMock
            .Setup(x => x.GetMonthlyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), currentDate.Year, currentDate.Month, null))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetMonthlyEvents();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);

        _eventServiceMock.Verify(x => x.GetMonthlyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), currentDate.Year, currentDate.Month, null), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyEvents_WithSpecificYearAndMonth_ShouldReturnEventsForThatMonth()
    {
        // Arrange
        var year = 2025;
        var month = 10;
        var response = new WeeklyEventsResponseDto
        {
            Categories = new List<EventCategoryDto>(),
            Events = new List<EventDto>()
        };

        _eventServiceMock
            .Setup(x => x.GetMonthlyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), year, month, null))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetMonthlyEvents(year: year, month: month);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();

        _eventServiceMock.Verify(x => x.GetMonthlyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), year, month, null), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyEvents_WithCategoryFilter_ShouldReturnFilteredEvents()
    {
        // Arrange
        var year = 2025;
        var month = 10;
        var categoryId = Guid.NewGuid();
        var response = new WeeklyEventsResponseDto
        {
            Categories = new List<EventCategoryDto>
            {
                new EventCategoryDto { Id = categoryId, Name = "Trabajo", Color = "#2b7fff" }
            },
            Events = new List<EventDto>
            {
                new EventDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Work Event",
                    StartDate = new DateTime(year, month, 15),
                    EndDate = new DateTime(year, month, 15).AddHours(1),
                    EventCategory = new EventCategoryDto { Id = categoryId, Name = "Trabajo", Color = "#2b7fff" }
                }
            }
        };

        _eventServiceMock
            .Setup(x => x.GetMonthlyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), year, month, categoryId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetMonthlyEvents(year: year, month: month, categoryId: categoryId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var actualResponse = okResult!.Value as WeeklyEventsResponseDto;
        actualResponse.Should().NotBeNull();
        var responseValue = actualResponse!;
        responseValue.Events.Should().NotBeNull();
        var responseEvents = responseValue.Events!;
        responseEvents.Should().AllSatisfy(e =>
        {
            e.EventCategory.Should().NotBeNull();
            e.EventCategory!.Id.Should().Be(categoryId);
        });

        _eventServiceMock.Verify(x => x.GetMonthlyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), year, month, categoryId), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyEvents_WithInvalidMonth_ShouldReturnBadRequest()
    {
        // Arrange
        var year = 2025;
        var invalidMonth = 13;

        // Act
        var result = await _controller.GetMonthlyEvents(year: year, month: invalidMonth);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();

        _eventServiceMock.Verify(x => x.GetMonthlyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task GetWeeklyEvents_WithCategoryFilter_ShouldReturnFilteredEvents()
    {
        // Arrange
        var startDate = new DateTime(2025, 10, 1);
        var categoryId = Guid.NewGuid();
        var request = new WeeklyEventsRequestDto
        {
            WeekStart = startDate
        };

        var response = new WeeklyEventsResponseDto
        {
            Categories = new List<EventCategoryDto>
            {
                new EventCategoryDto { Id = categoryId, Name = "Estudio", Color = "#00c950" }
            },
            Events = new List<EventDto>
            {
                new EventDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Study Session",
                    StartDate = startDate,
                    EndDate = startDate.AddHours(2),
                    EventCategory = new EventCategoryDto { Id = categoryId, Name = "Estudio", Color = "#00c950" }
                }
            }
        };

        _eventServiceMock
            .Setup(x => x.GetWeeklyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), startDate, categoryId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetWeeklyEvents(request, categoryId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var actualResponse = okResult!.Value as WeeklyEventsResponseDto;
        actualResponse.Should().NotBeNull();
        var weeklyResponse = actualResponse!;
        weeklyResponse.Events.Should().NotBeNull();
        var weeklyEvents = weeklyResponse.Events!;
        weeklyEvents.Should().AllSatisfy(e =>
        {
            e.EventCategory.Should().NotBeNull();
            e.EventCategory!.Id.Should().Be(categoryId);
        });

        _eventServiceMock.Verify(x => x.GetWeeklyEventsAsync(It.Is<Guid?>(userId => BeCurrentUser(userId)), startDate, categoryId), Times.Once);
    }
}