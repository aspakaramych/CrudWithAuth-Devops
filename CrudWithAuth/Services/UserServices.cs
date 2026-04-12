using CrudWithAuth.DTOs;
using CrudWithAuth.Entity;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Repository;

namespace CrudWithAuth.Services;

public class UserServices : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserServices> _logger;

    public UserServices(IUserRepository userRepository, ILogger<UserServices> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsers()
    {
        _logger.LogInformation("Fetching all users from repository");
        var users = await _userRepository.GetAllUsers();
        var response = users.Select(u => new UserResponse
        {
            Id = u.Id,
            Email = u.Email,
            Name = u.Name
        }).ToList();
        _logger.LogInformation("Retrieved {Count} users from DB", response.Count);
        return response;
    }

    public async Task<UserResponse?> GetUserById(Guid id)
    {
        _logger.LogInformation("Fetching user by id: {UserId}", id);
        var user = await _userRepository.GetUserById(id);
        if (user == null)
        {
            _logger.LogWarning("User not found by id: {UserId}", id);
            throw new NotFoundException("User not found");
        }
        _logger.LogInformation("User found: {UserId} ({Email})", user.Id, user.Email);
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name
        };
    }

    public async Task CreateUser(UserRequest user)
    {
        _logger.LogInformation("Creating new user with email: {Email}", user.Email);
        var userEntity = new User
        {
            Id = Guid.NewGuid(),
            Name = user.Name,
            Email = user.Email,
            Password = user.Password
        };
        await _userRepository.CreateUser(userEntity);
        _logger.LogInformation("User created: Id={UserId}, Email={Email}", userEntity.Id, userEntity.Email);
    }

    public async Task UpdateUser(UserRequest user, Guid id)
    {
        _logger.LogInformation("Updating user id: {UserId}", id);
        var userEntity = await _userRepository.GetUserById(id);
        if (userEntity == null)
        {
            _logger.LogWarning("UpdateUser: user not found, id: {UserId}", id);
            throw new NotFoundException("User not found");
        }
        userEntity.Name = user.Name;
        userEntity.Email = user.Email;
        userEntity.Password = user.Password;
        await _userRepository.UpdateUser(userEntity);
        _logger.LogInformation("User updated successfully: {UserId}", id);
    }

    public async Task DeleteUser(Guid id)
    {
        _logger.LogInformation("Deleting user id: {UserId}", id);
        var userEntity = await _userRepository.GetUserById(id);
        if (userEntity == null)
        {
            _logger.LogWarning("DeleteUser: user not found, id: {UserId}", id);
            throw new NotFoundException("User not found");
        }
        await _userRepository.DeleteUser(userEntity);
        _logger.LogInformation("User deleted: {UserId}", id);
    }

    public async Task<UserResponse?> GetUserByEmail(string email)
    {
        _logger.LogInformation("Fetching user by email: {Email}", email);
        var userEntity = await _userRepository.GetUserByEmail(email);
        if (userEntity == null)
        {
            _logger.LogWarning("User not found by email: {Email}", email);
            throw new NotFoundException("User not found");
        }
        _logger.LogInformation("User found by email: {UserId} ({Email})", userEntity.Id, email);
        return new UserResponse
        {
            Id = userEntity.Id,
            Name = userEntity.Name,
            Email = userEntity.Email
        };
    }
}