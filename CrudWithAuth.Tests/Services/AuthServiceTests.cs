using CrudWithAuth.Configuration;
using CrudWithAuth.DTOs;
using CrudWithAuth.Entity;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Repository;
using CrudWithAuth.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CrudWithAuth.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenBlacklistService> _tokenBlacklistServiceMock;
    private readonly IJwtService _jwtService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenBlacklistServiceMock = new Mock<ITokenBlacklistService>();

        var jwtSettings = new JwtSettings
        {
            SecretKey = "test-super-secret-key-that-is-long-enough-for-hmac",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
        _jwtService = new JwtService(Options.Create(jwtSettings));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _tokenBlacklistServiceMock.Object,
            _jwtService,
            configuration);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldCreateUserAndReturnJwtToken()
    {
        var request = new RegisterRequest
        {
            Name = "New User",
            Email = "newuser@test.com",
            Password = "password123"
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(request.Email)).ReturnsAsync((User?)null);

        var result = await _authService.RegisterAsync(request);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Name.Should().Be(request.Name);
        result.User.Email.Should().Be(request.Email);
        _userRepositoryMock.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        var request = new RegisterRequest
        {
            Name = "New User",
            Email = "newuser@test.com",
            Password = "password123"
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(request.Email)).ReturnsAsync((User?)null);

        User? createdUser = null;
        _userRepositoryMock
            .Setup(x => x.CreateUser(It.IsAny<User>()))
            .Callback<User>(u => createdUser = u);

        await _authService.RegisterAsync(request);

        createdUser.Should().NotBeNull();
        createdUser!.Password.Should().NotBe(request.Password);
        BCrypt.Net.BCrypt.Verify(request.Password, createdUser.Password).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnValidJwt()
    {
        var request = new RegisterRequest
        {
            Name = "JWT User",
            Email = "jwtuser@test.com",
            Password = "password123"
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(request.Email)).ReturnsAsync((User?)null);

        var result = await _authService.RegisterAsync(request);

        var principal = _jwtService.ValidateToken(result.AccessToken);
        principal.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        var request = new RegisterRequest
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
            Password = BCrypt.Net.BCrypt.HashPassword("oldpassword")
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(request.Email)).ReturnsAsync(existingUser);

        Func<Task> act = async () => await _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User with this email already exists");
        _userRepositoryMock.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnJwtToken()
    {
        var plainPassword = "password123";
        var request = new LoginRequest { Email = "user@test.com", Password = plainPassword };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "user@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword(plainPassword)
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(request.Email)).ReturnsAsync(user);

        var result = await _authService.LoginAsync(request);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be(request.Email);
        result.User.Name.Should().Be("Test User");
        _jwtService.GetUserIdFromToken(result.AccessToken).Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowNotFoundException()
    {
        var request = new LoginRequest { Email = "nonexistent@test.com", Password = "password123" };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(request.Email)).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _authService.LoginAsync(request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowInvalidOperationException()
    {
        var request = new LoginRequest { Email = "user@test.com", Password = "wrongpassword" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "user@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correctpassword")
        };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(request.Email)).ReturnsAsync(user);

        Func<Task> act = async () => await _authService.LoginAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LogoutAsync_ShouldBlacklistAccessTokenWithRemainingExpiry()
    {
        var accessToken = _jwtService.GenerateAccessToken(Guid.NewGuid(), "user@test.com", "Test User");

        await _authService.LogoutAsync(accessToken);

        _tokenBlacklistServiceMock.Verify(x => x.BlacklistTokenAsync(
            accessToken,
            It.Is<TimeSpan>(ts => ts > TimeSpan.Zero && ts <= TimeSpan.FromHours(1))
        ), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithExpiredToken_ShouldNotBlacklist()
    {
        var expiredToken = "expired.token.string";

        await _authService.LogoutAsync(expiredToken);

        _tokenBlacklistServiceMock.Verify(
            x => x.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }
}
