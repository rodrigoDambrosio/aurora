using Aurora.Application.DTOs.Auth;
using Aurora.Application.Interfaces;
using Aurora.Application.Options;
using Aurora.Application.Services;
using Aurora.Domain.Entities;
using Aurora.Domain.Services;
using System.Linq;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aurora.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IEventCategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly IValidator<RegisterUserRequestDto> _registerValidator = new InlineValidator<RegisterUserRequestDto>();
    private readonly IValidator<LoginRequestDto> _loginValidator = new InlineValidator<LoginRequestDto>();
    private readonly Microsoft.Extensions.Options.IOptions<JwtOptions> _jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
    {
        SessionDurationDays = 7,
        AccessTokenMinutes = 60,
        SecretKey = new string('a', 32)
    });

    [Fact]
    public async Task RegisterAsync_ShouldCreateDefaultCategories_WhenUserHasNone()
    {
        var request = new RegisterUserRequestDto
        {
            Name = "Ana Tester",
            Email = "ana@example.com",
            Password = "Password123!"
        };

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var createdUser = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = normalizedEmail,
            PasswordHash = "hashed"
        };

        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.HashPassword(request.Password))
            .Returns(createdUser.PasswordHash);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        _userRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _categoryRepositoryMock
            .Setup(c => c.GetDefaultCategoriesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<EventCategory>());

        _categoryRepositoryMock
            .Setup(c => c.AddAsync(It.IsAny<EventCategory>()))
            .ReturnsAsync((EventCategory category) => category);

        _categoryRepositoryMock
            .Setup(c => c.SaveChangesAsync())
            .ReturnsAsync(1);

        _sessionRepositoryMock
            .Setup(s => s.AddAsync(It.IsAny<UserSession>()))
            .ReturnsAsync((UserSession session) => session);

        _sessionRepositoryMock
            .Setup(s => s.SaveChangesAsync())
            .ReturnsAsync(1);

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<UserSession>()))
            .Returns("token-value");

        var sut = CreateSut();

        var result = await sut.RegisterAsync(request);

        result.Should().NotBeNull();
        result.User.Id.Should().NotBeEmpty();

        var expectedCount = DefaultEventCategories.GetAllConfigurations().Count();

        _categoryRepositoryMock.Verify(c => c.AddAsync(It.IsAny<EventCategory>()), Times.Exactly(expectedCount));
        _categoryRepositoryMock.Verify(c => c.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotDuplicateDefaultCategories_WhenTheyAlreadyExist()
    {
        var request = new RegisterUserRequestDto
        {
            Name = "Carlos",
            Email = "carlos@example.com",
            Password = "Password123!"
        };

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingCategory = DefaultEventCategories.CreateSystemCategories().First();
        existingCategory.UserId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.HashPassword(request.Password))
            .Returns("hashed");

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        _userRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _categoryRepositoryMock
            .Setup(c => c.GetDefaultCategoriesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new[] { existingCategory });

        _sessionRepositoryMock
            .Setup(s => s.AddAsync(It.IsAny<UserSession>()))
            .ReturnsAsync((UserSession session) => session);

        _sessionRepositoryMock
            .Setup(s => s.SaveChangesAsync())
            .ReturnsAsync(1);

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<UserSession>()))
            .Returns("token-value");

        var sut = CreateSut();

        _ = await sut.RegisterAsync(request);

        _categoryRepositoryMock.Verify(c => c.AddAsync(It.IsAny<EventCategory>()), Times.Never);
        _categoryRepositoryMock.Verify(c => c.SaveChangesAsync(), Times.Never);
    }

    private AuthService CreateSut()
    {
        return new AuthService(
            _userRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _categoryRepositoryMock.Object,
            _registerValidator,
            _loginValidator,
            _jwtOptions,
            _loggerMock.Object);
    }
}
