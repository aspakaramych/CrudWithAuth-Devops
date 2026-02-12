using CrudWithAuth.Controllers;
using CrudWithAuth.DTOs;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CrudWithAuth.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnOkWithUsers()
    {
        var users = new List<UserResponse>
        {
            new UserResponse { Id = Guid.NewGuid(), Name = "User1", Email = "user1@test.com" },
            new UserResponse { Id = Guid.NewGuid(), Name = "User2", Email = "user2@test.com" }
        };
        _userServiceMock.Setup(x => x.GetAllUsers()).ReturnsAsync(users);

        var result = await _controller.GetAllUsers();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value.Should().BeAssignableTo<IEnumerable<UserResponse>>().Subject;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnOkWithUser()
    {
        var userId = Guid.NewGuid();
        var user = new UserResponse { Id = userId, Name = "Test User", Email = "test@test.com" };
        _userServiceMock.Setup(x => x.GetUserById(userId)).ReturnsAsync(user);

        var result = await _controller.GetUserById(userId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeOfType<UserResponse>().Subject;
        returnedUser.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.GetUserById(userId)).ThrowsAsync(new NotFoundException("User not found"));

        var result = await _controller.GetUserById(userId);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreatedAtAction()
    {
        var userRequest = new UserRequest
        {
            Name = "New User",
            Email = "newuser@test.com",
            Password = "password123"
        };

        var result = await _controller.CreateUser(userRequest);

        result.Should().BeOfType<CreatedAtActionResult>();
        _userServiceMock.Verify(x => x.CreateUser(userRequest), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WhenUserExists_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var userRequest = new UserRequest
        {
            Name = "Updated User",
            Email = "updated@test.com",
            Password = "newpassword"
        };

        var result = await _controller.UpdateUser(userId, userRequest);

        result.Should().BeOfType<NoContentResult>();
        _userServiceMock.Verify(x => x.UpdateUser(userRequest, userId), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        var userRequest = new UserRequest
        {
            Name = "Updated User",
            Email = "updated@test.com",
            Password = "newpassword"
        };
        _userServiceMock.Setup(x => x.UpdateUser(userRequest, userId))
            .ThrowsAsync(new NotFoundException("User not found"));

        var result = await _controller.UpdateUser(userId, userRequest);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();

        var result = await _controller.DeleteUser(userId);

        result.Should().BeOfType<NoContentResult>();
        _userServiceMock.Verify(x => x.DeleteUser(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.DeleteUser(userId))
            .ThrowsAsync(new NotFoundException("User not found"));

        var result = await _controller.DeleteUser(userId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
