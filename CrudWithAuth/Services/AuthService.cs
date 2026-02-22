using CrudWithAuth.DTOs;
using CrudWithAuth.Entity;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Repository;
using Microsoft.Extensions.Configuration;

namespace CrudWithAuth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IJwtService _jwtService;
    private readonly int _refreshTokenExpirationDays;

    public AuthService(
        IUserRepository userRepository,
        ITokenBlacklistService tokenBlacklistService,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _tokenBlacklistService = tokenBlacklistService;
        _jwtService = jwtService;
        _refreshTokenExpirationDays = configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetUserByEmail(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Password = passwordHash
        };

        await _userRepository.CreateUser(user);

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        if (!isPasswordValid)
        {
            throw new InvalidOperationException("Invalid email or password");
        }

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }

    public async Task LogoutAsync(string accessToken)
    {
        var expiration = _jwtService.GetTokenExpiration(accessToken);
        var remaining = expiration - DateTime.UtcNow;

        if (remaining > TimeSpan.Zero)
        {
            await _tokenBlacklistService.BlacklistTokenAsync(accessToken, remaining);
        }
    }
}
