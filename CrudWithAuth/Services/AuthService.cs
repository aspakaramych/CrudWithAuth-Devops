using CrudWithAuth.DTOs;
using CrudWithAuth.Entity;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Repository;

namespace CrudWithAuth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public AuthService(IUserRepository userRepository, ITokenBlacklistService tokenBlacklistService)
    {
        _userRepository = userRepository;
        _tokenBlacklistService = tokenBlacklistService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetUserByEmail(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Password = request.Password
        };

        await _userRepository.CreateUser(user);

        var token = Guid.NewGuid().ToString();

        return new AuthResponse
        {
            Token = token,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmail(request.Email);
        if (user == null)
        {
            throw new NotFoundException("Invalid email or password");
        }

        if (user.Password != request.Password)
        {
            throw new InvalidOperationException("Invalid email or password");
        }

        var token = Guid.NewGuid().ToString();

        return new AuthResponse
        {
            Token = token,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }

    public async Task LogoutAsync(string token)
    {
        await _tokenBlacklistService.BlacklistTokenAsync(token, TimeSpan.FromHours(24));
    }
}
