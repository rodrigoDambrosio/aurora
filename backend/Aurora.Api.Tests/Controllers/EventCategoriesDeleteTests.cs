using Aurora.Api.Controllers;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aurora.Api.Tests.Controllers;

/// <summary>
/// Tests para verificar la eliminación de categorías con soft delete
/// </summary>
public class EventCategoriesDeleteTests
{
    private readonly Mock<IEventCategoryRepository> _repositoryMock;
    private readonly Mock<ILogger<EventCategoriesController>> _loggerMock;
    private readonly EventCategoriesController _controller;
    private readonly Guid _userId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    public EventCategoriesDeleteTests()
    {
        _repositoryMock = new Mock<IEventCategoryRepository>();
        _loggerMock = new Mock<ILogger<EventCategoriesController>>();
        _controller = new EventCategoriesController(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DeleteCategory_SinEventos_DebeEliminarCorrectamente()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new EventCategory
        {
            Id = categoryId,
            Name = "Test Category",
            UserId = _userId,
            IsSystemDefault = false,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);
        _repositoryMock.Setup(r => r.GetEventCountForCategoryAsync(categoryId))
            .ReturnsAsync(0);
        _repositoryMock.Setup(r => r.DeleteAsync(categoryId))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.DeleteCategory(categoryId, null, _userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _repositoryMock.Verify(r => r.DeleteAsync(categoryId), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteCategory_ConEventos_SinReasignacion_DebeRetornarBadRequest()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new EventCategory
        {
            Id = categoryId,
            Name = "Test Category",
            UserId = _userId,
            IsSystemDefault = false,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);
        _repositoryMock.Setup(r => r.GetEventCountForCategoryAsync(categoryId))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.DeleteCategory(categoryId, null, _userId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task DeleteCategory_ConEventos_ConReasignacion_DebeEliminarYReasignar()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var targetCategoryId = Guid.NewGuid();
        
        var category = new EventCategory
        {
            Id = categoryId,
            Name = "Test Category",
            UserId = _userId,
            IsSystemDefault = false,
            IsActive = true
        };

        var targetCategory = new EventCategory
        {
            Id = targetCategoryId,
            Name = "Target Category",
            UserId = _userId,
            IsSystemDefault = false,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);
        _repositoryMock.Setup(r => r.GetByIdAsync(targetCategoryId))
            .ReturnsAsync(targetCategory);
        _repositoryMock.Setup(r => r.GetEventCountForCategoryAsync(categoryId))
            .ReturnsAsync(3);
        _repositoryMock.Setup(r => r.ReassignEventsToAnotherCategoryAsync(categoryId, targetCategoryId))
            .ReturnsAsync(3);
        _repositoryMock.Setup(r => r.DeleteAsync(categoryId))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.DeleteCategory(categoryId, targetCategoryId, _userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _repositoryMock.Verify(r => r.ReassignEventsToAnotherCategoryAsync(categoryId, targetCategoryId), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(categoryId), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteCategory_CategoriaDelSistema_DebeRetornarForbidden()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var systemCategory = new EventCategory
        {
            Id = categoryId,
            Name = "System Category",
            UserId = _userId,
            IsSystemDefault = true,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(systemCategory);

        // Act
        var result = await _controller.DeleteCategory(categoryId, null, _userId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteCategory_CategoriaNoExiste_DebeRetornarNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((EventCategory?)null);

        // Act
        var result = await _controller.DeleteCategory(categoryId, null, _userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteCategory_UsuarioDiferente_DebeRetornarForbidden()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        
        var category = new EventCategory
        {
            Id = categoryId,
            Name = "Test Category",
            UserId = otherUserId, // Diferente usuario
            IsSystemDefault = false,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.DeleteCategory(categoryId, null, _userId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(403);
    }
}
