using CrudWithAuth.Data;
using CrudWithAuth.Entity;
using Microsoft.EntityFrameworkCore;

namespace CrudWithAuth.Repository;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(AppDbContext dbContext, ILogger<UserRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<User>> GetAllUsers()
    {
        _logger.LogInformation("DB: querying all users");
        var users = await _dbContext.Users.ToListAsync();
        _logger.LogInformation("DB: retrieved {Count} users", users.Count);
        return users;
    }

    public async Task<User?> GetUserById(Guid id)
    {
        _logger.LogInformation("DB: querying user by id: {UserId}", id);
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            _logger.LogInformation("DB: user not found, id: {UserId}", id);
        else
            _logger.LogInformation("DB: user found, id: {UserId}", id);
        return user;
    }

    public async Task CreateUser(User user)
    {
        _logger.LogInformation("DB: inserting new user, id: {UserId}, email: {Email}", user.Id, user.Email);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("DB: user saved, id: {UserId}", user.Id);
    }

    public async Task UpdateUser(User user)
    {
        _logger.LogInformation("DB: updating user id: {UserId}", user.Id);
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("DB: user updated, id: {UserId}", user.Id);
    }

    public async Task DeleteUser(User user)
    {
        _logger.LogInformation("DB: deleting user id: {UserId}", user.Id);
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("DB: user deleted, id: {UserId}", user.Id);
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        _logger.LogInformation("DB: querying user by email: {Email}", email);
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            _logger.LogInformation("DB: user not found by email: {Email}", email);
        else
            _logger.LogInformation("DB: user found by email, id: {UserId}", user.Id);
        return user;
    }
}