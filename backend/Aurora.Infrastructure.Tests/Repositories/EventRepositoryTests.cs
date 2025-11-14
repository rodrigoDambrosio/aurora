using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data;
using Aurora.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Tests.Repositories;

public class EventRepositoryTests : IDisposable
{
    private readonly AuroraDbContext _context;
    private readonly EventRepository _repository;

    public EventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuroraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuroraDbContext(options);
        _repository = new EventRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetEventsByUserIdAsync_WithExistingUser_ShouldReturnUserEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var userEvents = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento del usuario",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                UserId = userId,
                EventCategoryId = category.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Otro evento del usuario",
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(1).AddHours(2),
                UserId = userId,
                EventCategoryId = category.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento de otro usuario",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                UserId = otherUserId,
                EventCategoryId = category.Id
            }
        };

        _context.EventCategories.Add(category);
        _context.Events.AddRange(userEvents);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEventsByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.UserId == userId);
    }

    [Fact]
    public async Task GetWeeklyEventsAsync_WithEventsInWeek_ShouldReturnEventsInDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var weekStart = new DateTime(2024, 1, 1); // Monday
        var weekEnd = weekStart.AddDays(7);

        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var events = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento en la semana",
                StartDate = weekStart.AddHours(9),
                EndDate = weekStart.AddHours(10),
                UserId = userId,
                EventCategoryId = category.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento fuera de la semana",
                StartDate = weekEnd.AddDays(1),
                EndDate = weekEnd.AddDays(1).AddHours(1),
                UserId = userId,
                EventCategoryId = category.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento al final de la semana",
                StartDate = weekEnd.AddMinutes(-1),
                EndDate = weekEnd.AddHours(1),
                UserId = userId,
                EventCategoryId = category.Id
            }
        };

        _context.EventCategories.Add(category);
        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWeeklyEventsAsync(userId, weekStart);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Title == "Evento en la semana");
        result.Should().Contain(e => e.Title == "Evento al final de la semana");
        result.Should().NotContain(e => e.Title == "Evento fuera de la semana");
    }

    [Fact]
    public async Task GetEventsByCategoryAsync_WithEventsInCategory_ShouldReturnCategoryEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workCategoryId = Guid.NewGuid();
        var personalCategoryId = Guid.NewGuid();

        var workCategory = new EventCategory
        {
            Id = workCategoryId,
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var personalCategory = new EventCategory
        {
            Id = personalCategoryId,
            Name = "Personal",
            Color = "#f39c12",
            UserId = userId
        };

        var events = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Reunión de trabajo",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                UserId = userId,
                EventCategoryId = workCategoryId
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Cita personal",
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(1).AddHours(1),
                UserId = userId,
                EventCategoryId = personalCategoryId
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Otra reunión de trabajo",
                StartDate = DateTime.Now.AddDays(2),
                EndDate = DateTime.Now.AddDays(2).AddHours(2),
                UserId = userId,
                EventCategoryId = workCategoryId
            }
        };

        _context.EventCategories.AddRange([workCategory, personalCategory]);
        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEventsByCategoryAsync(userId, workCategoryId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.EventCategoryId == workCategoryId);
        result.Should().Contain(e => e.Title == "Reunión de trabajo");
        result.Should().Contain(e => e.Title == "Otra reunión de trabajo");
    }

    [Fact]
    public async Task GetOverlappingEventsAsync_WithOverlappingEvents_ShouldReturnOverlappingEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var targetStart = new DateTime(2024, 1, 1, 10, 0, 0);
        var targetEnd = new DateTime(2024, 1, 1, 12, 0, 0);

        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var events = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento que se superpone",
                StartDate = new DateTime(2024, 1, 1, 11, 0, 0),
                EndDate = new DateTime(2024, 1, 1, 13, 0, 0),
                UserId = userId,
                EventCategoryId = category.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento que no se superpone",
                StartDate = new DateTime(2024, 1, 1, 13, 0, 0),
                EndDate = new DateTime(2024, 1, 1, 14, 0, 0),
                UserId = userId,
                EventCategoryId = category.Id
            }
        };

        _context.EventCategories.Add(category);
        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetOverlappingEventsAsync(userId, targetStart, targetEnd);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Evento que se superpone");
    }

    [Fact]
    public async Task GetEventsByDateRangeAsync_WithEventsInRange_ShouldReturnEventsInRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = new DateTime(2024, 1, 1);
        var endDateExclusive = new DateTime(2024, 1, 8);

        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var events = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento en rango",
                StartDate = new DateTime(2024, 1, 3),
                EndDate = new DateTime(2024, 1, 3, 2, 0, 0),
                UserId = userId,
                EventCategoryId = category.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Evento fuera de rango",
                StartDate = new DateTime(2024, 1, 10),
                EndDate = new DateTime(2024, 1, 10, 1, 0, 0),
                UserId = userId,
                EventCategoryId = category.Id
            }
        };

        _context.EventCategories.Add(category);
        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEventsByDateRangeAsync(userId, startDate, endDateExclusive);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Evento en rango");
    }

    [Fact]
    public async Task GetEventsByDateRangeAsync_WithOverlappingEvents_ShouldIncludeEventsOverlappingRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = new DateTime(2024, 1, 5);
        var endDateExclusive = new DateTime(2024, 1, 7);

        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var spanningEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Evento que se solapa",
            StartDate = new DateTime(2024, 1, 4, 12, 0, 0),
            EndDate = new DateTime(2024, 1, 5, 12, 0, 0),
            UserId = userId,
            EventCategoryId = category.Id
        };

        var fullyOutsideEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Evento fuera",
            StartDate = new DateTime(2024, 1, 7, 12, 0, 0),
            EndDate = new DateTime(2024, 1, 7, 14, 0, 0),
            UserId = userId,
            EventCategoryId = category.Id
        };

        _context.EventCategories.Add(category);
        _context.Events.AddRange(spanningEvent, fullyOutsideEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEventsByDateRangeAsync(userId, startDate, endDateExclusive);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Evento que se solapa");
    }

    [Fact]
    public async Task AddAsync_WithValidEvent_ShouldAddEventToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var eventToAdd = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Nuevo evento",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            UserId = userId,
            EventCategoryId = category.Id
        };

        _context.EventCategories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AddAsync(eventToAdd);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Nuevo evento");

        var eventInDb = await _context.Events.FindAsync(eventToAdd.Id);
        eventInDb.Should().NotBeNull();
        eventInDb?.Title.Should().Be("Nuevo evento");
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEvent_ShouldUpdateEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Evento original",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            UserId = userId,
            EventCategoryId = category.Id
        };

        _context.EventCategories.Add(category);
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        // Modificar el evento
        originalEvent.Title = "Evento actualizado";
        originalEvent.Description = "Nueva descripción";

        // Act
        var result = _repository.Update(originalEvent);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Evento actualizado");
        result.Description.Should().Be("Nueva descripción");

        var eventInDb = await _context.Events.FindAsync(originalEvent.Id);
        eventInDb?.Title.Should().Be("Evento actualizado");
        eventInDb?.Description.Should().Be("Nueva descripción");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEvent_ShouldRemoveEventFromDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trabajo",
            Color = "#1447e6",
            UserId = userId
        };

        var eventToDelete = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Evento a eliminar",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            UserId = userId,
            EventCategoryId = category.Id
        };

        _context.EventCategories.Add(category);
        _context.Events.Add(eventToDelete);
        await _context.SaveChangesAsync();

        // Act
        _repository.Remove(eventToDelete);
        await _context.SaveChangesAsync();

        // Assert
        var eventInDb = await _context.Events.FindAsync(eventToDelete.Id);
        eventInDb.Should().NotBeNull();
        eventInDb?.IsActive.Should().BeFalse(); // Soft delete
    }
}