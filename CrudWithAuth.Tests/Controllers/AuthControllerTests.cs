using CrudWithAuth.Controllers;
using CrudWithAuth.DTOs;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CrudWithAuth.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Register_WithValidRequest_ShouldReturnOkWithAuthResponse()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "New User",
            Email = "newuser@test.com",
            Password = "password123"
        };
        var authResponse = new AuthResponse
        {
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test",
            RefreshToken = "refresh-token-base64",
            User = new UserResponse
            {
                Id = Guid.NewGuid(),
                Name = "New User",
                Email = "newuser@test.com"
            }
        };
        _authServiceMock.Setup(x => x.RegisterAsync(registerRequest)).ReturnsAsync(authResponse);

        var result = await _controller.Register(registerRequest);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        returnedResponse.AccessToken.Should().Be(authResponse.AccessToken);
        returnedResponse.RefreshToken.Should().Be(authResponse.RefreshToken);
        returnedResponse.User.Email.Should().Be("newuser@test.com");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnBadRequest()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "New User",
            Email = "existing@test.com",
            Password = "password123"
        };
        _authServiceMock.Setup(x => x.RegisterAsync(registerRequest))
            .ThrowsAsync(new InvalidOperationException("User with this email already exists"));

        var result = await _controller.Register(registerRequest);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithAuthResponse()
    {
        var loginRequest = new LoginRequest { Email = "user@test.com", Password = "password123" };
        var authResponse = new AuthResponse
        {
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test",
            RefreshToken = "refresh-token-base64",
            User = new UserResponse { Id = Guid.NewGuid(), Name = "Test User", Email = "user@test.com" }
        };
        _authServiceMock.Setup(x => x.LoginAsync(loginRequest)).ReturnsAsync(authResponse);

        var result = await _controller.Login(loginRequest);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        returnedResponse.AccessToken.Should().Be(authResponse.AccessToken);
        returnedResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnBadRequest()
    {
        var loginRequest = new LoginRequest { Email = "user@test.com", Password = "wrongpassword" };
        _authServiceMock.Setup(x => x.LoginAsync(loginRequest))
            .ThrowsAsync(new NotFoundException("Invalid email or password"));

        var result = await _controller.Login(loginRequest);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldReturnOk()
    {
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-jwt";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.HttpContext.Items["Token"] = token;

        var result = await _controller.Logout();

        result.Should().BeOfType<OkObjectResult>();
        _authServiceMock.Verify(x => x.LogoutAsync(token), Times.Once);
    }

    [Fact]
    public async Task Logout_WithoutToken_ShouldReturnBadRequest()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.Logout();

        result.Should().BeOfType<BadRequestObjectResult>();
        _authServiceMock.Verify(x => x.LogoutAsync(It.IsAny<string>()), Times.Never);
    }
}
