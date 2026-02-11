using CrudWithAuth.DTOs;

namespace CrudWithAuth.Services;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetAllUsers();
    Task<UserResponse?> GetUserById(Guid id);
    Task CreateUser(UserRequest user);
    Task UpdateUser(UserRequest user, Guid id);
    Task DeleteUser(Guid id);
    Task<UserResponse?> GetUserByEmail(string email);
}