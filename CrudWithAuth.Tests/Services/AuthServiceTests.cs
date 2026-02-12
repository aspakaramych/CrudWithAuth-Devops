using CrudWithAuth.DTOs;
using CrudWithAuth.Entity;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Repository;
using CrudWithAuth.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CrudWithAuth.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenBlacklistService> _tokenBlacklistServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenBlacklistServiceMock = new Mock<ITokenBlacklistService>();
        _authService = new AuthService(_userRepositoryMock.Object, _tokenBlacklistServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldCreateUserAndReturnToken()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "New User",
            Email = "newuser@test.com",
            Password = "password123"
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(registerRequest.Email)).ReturnsAsync((User?)null);

        var result = await _authService.RegisterAsync(registerRequest);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Name.Should().Be(registerRequest.Name);
        result.User.Email.Should().Be(registerRequest.Email);
        _userRepositoryMock.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "New User",
            Email = "existing@test.com",
            Password = "password123"
        };
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Existing User",
            Email = "existing@test.com",
            Password = "oldpassword"
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(registerRequest.Email)).ReturnsAsync(existingUser);

        Func<Task> act = async () => await _authService.RegisterAsync(registerRequest);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User with this email already exists");
        _userRepositoryMock.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        var loginRequest = new LoginRequest
        {
            Email = "user@test.com",
            Password = "password123"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "user@test.com",
            Password = "password123"
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(loginRequest.Email)).ReturnsAsync(user);

        var result = await _authService.LoginAsync(loginRequest);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(loginRequest.Email);
        result.User.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowNotFoundException()
    {
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "password123"
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(loginRequest.Email)).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _authService.LoginAsync(loginRequest);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowInvalidOperationException()
    {
        var loginRequest = new LoginRequest
        {
            Email = "user@test.com",
            Password = "wrongpassword"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "user@test.com",
            Password = "correctpassword"
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(loginRequest.Email)).ReturnsAsync(user);

        Func<Task> act = async () => await _authService.LoginAsync(loginRequest);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LogoutAsync_ShouldBlacklistToken()
    {
        var token = "test-token-123";

        await _authService.LogoutAsync(token);

        _tokenBlacklistServiceMock.Verify(x => x.BlacklistTokenAsync(
            token,
            It.Is<TimeSpan>(ts => ts.TotalHours == 24)
        ), Times.Once);
    }
}
