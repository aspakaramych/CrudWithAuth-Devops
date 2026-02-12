using CrudWithAuth.DTOs;
using CrudWithAuth.Entity;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Repository;

namespace CrudWithAuth.Services;

public class UserServices : IUserService
{
    private readonly IUserRepository  _userRepository;

    public UserServices(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<IEnumerable<UserResponse>> GetAllUsers()
    {
        var users =  await _userRepository.GetAllUsers();
        var response = users.Select(u => new UserResponse
        {
            Id = u.Id,
            Email = u.Email,
            Name = u.Name
        });
        return response;
    }

    public async Task<UserResponse?> GetUserById(Guid id)
    {
        var user =  await _userRepository.GetUserById(id);
        if (user == null)
            throw new NotFoundException("User not found");
        var response = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name
        };
        return response;
    }

    public async Task CreateUser(UserRequest user)
    {
        var userEntity = new User
        {
            Id = Guid.NewGuid(),
            Name = user.Name,
            Email = user.Email,
            Password = user.Password
        };
        await _userRepository.CreateUser(userEntity);
    }

    public async Task UpdateUser(UserRequest user, Guid id)
    {
        var userEntity = await _userRepository.GetUserById(id);
        if (userEntity == null)
            throw new NotFoundException("User not found");
        userEntity.Name = user.Name;
        userEntity.Email = user.Email;
        userEntity.Password = user.Password;
        await _userRepository.UpdateUser(userEntity);
    }

    public async Task DeleteUser(Guid id)
    {
        var  userEntity = await _userRepository.GetUserById(id);
        if (userEntity == null)
            throw new NotFoundException("User not found");
        await _userRepository.DeleteUser(userEntity);
    }

    public async Task<UserResponse?> GetUserByEmail(string email)
    {
        var userEntity = await _userRepository.GetUserByEmail(email);
        if (userEntity == null)
            throw new NotFoundException("User not found");
        var response = new UserResponse
        {
            Id = userEntity.Id,
            Name = userEntity.Name,
            Email = userEntity.Email
        };
        return response;
    }
}