using CrudWithAuth.Entity;

namespace CrudWithAuth.Repository;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsers();
    Task<User?> GetUserById(Guid id);
    Task CreateUser(User user);
    Task UpdateUser(User user);
    Task DeleteUser(User user);
    Task<User?> GetUserByEmail(string email);
}