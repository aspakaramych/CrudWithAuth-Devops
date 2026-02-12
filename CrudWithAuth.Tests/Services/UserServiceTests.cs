using CrudWithAuth.DTOs;
using CrudWithAuth.Entity;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Repository;
using CrudWithAuth.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CrudWithAuth.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UserServices _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userService = new UserServices(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnAllUsers()
    {
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "User1", Email = "user1@test.com", Password = "pass1" },
            new User { Id = Guid.NewGuid(), Name = "User2", Email = "user2@test.com", Password = "pass2" }
        };
        _userRepositoryMock.Setup(x => x.GetAllUsers()).ReturnsAsync(users);

        var result = await _userService.GetAllUsers();

        result.Should().HaveCount(2);
        result.Should().AllBeOfType<UserResponse>();
        result.Should().OnlyContain(u => !string.IsNullOrEmpty(u.Name));
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnUser()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Test User", Email = "test@test.com", Password = "password" };
        _userRepositoryMock.Setup(x => x.GetUserById(userId)).ReturnsAsync(user);

        var result = await _userService.GetUserById(userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(x => x.GetUserById(userId)).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _userService.GetUserById(userId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task CreateUser_ShouldCallRepositoryCreateUser()
    {
        var userRequest = new UserRequest
        {
            Name = "New User",
            Email = "newuser@test.com",
            Password = "password123"
        };

        await _userService.CreateUser(userRequest);

        _userRepositoryMock.Verify(x => x.CreateUser(It.Is<User>(u =>
            u.Name == userRequest.Name &&
            u.Email == userRequest.Email &&
            u.Password == userRequest.Password
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WhenUserExists_ShouldUpdateUser()
    {
        var userId = Guid.NewGuid();
        var existingUser = new User { Id = userId, Name = "Old Name", Email = "old@test.com", Password = "oldpass" };
        var updateRequest = new UserRequest { Name = "New Name", Email = "new@test.com", Password = "newpass" };

        _userRepositoryMock.Setup(x => x.GetUserById(userId)).ReturnsAsync(existingUser);

        await _userService.UpdateUser(updateRequest, userId);

        _userRepositoryMock.Verify(x => x.UpdateUser(It.Is<User>(u =>
            u.Id == userId &&
            u.Name == "New Name" &&
            u.Email == "new@test.com" &&
            u.Password == "newpass"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        var updateRequest = new UserRequest { Name = "New Name", Email = "new@test.com", Password = "newpass" };
        _userRepositoryMock.Setup(x => x.GetUserById(userId)).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _userService.UpdateUser(updateRequest, userId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldDeleteUser()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Test User", Email = "test@test.com", Password = "password" };
        _userRepositoryMock.Setup(x => x.GetUserById(userId)).ReturnsAsync(user);

        await _userService.DeleteUser(userId);

        _userRepositoryMock.Verify(x => x.DeleteUser(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(x => x.GetUserById(userId)).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _userService.DeleteUser(userId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task GetUserByEmail_WhenUserExists_ShouldReturnUser()
    {
        var email = "test@test.com";
        var user = new User { Id = Guid.NewGuid(), Name = "Test User", Email = email, Password = "password" };
        _userRepositoryMock.Setup(x => x.GetUserByEmail(email)).ReturnsAsync(user);

        var result = await _userService.GetUserByEmail(email);

        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
        result.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetUserByEmail_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var email = "nonexistent@test.com";
        _userRepositoryMock.Setup(x => x.GetUserByEmail(email)).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _userService.GetUserByEmail(email);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found");
    }
}
