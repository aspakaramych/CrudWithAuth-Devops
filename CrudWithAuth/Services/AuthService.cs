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
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ITokenBlacklistService tokenBlacklistService,
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenBlacklistService = tokenBlacklistService;
        _jwtService = jwtService;
        _refreshTokenExpirationDays = configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Checking if user with email {Email} already exists", request.Email);
        var existingUser = await _userRepository.GetUserByEmail(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration blocked: email {Email} is already taken", request.Email);
            throw new InvalidOperationException("User with this email already exists");
        }

        _logger.LogInformation("Hashing password and creating user: {Email}", request.Email);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Password = passwordHash
        };

        await _userRepository.CreateUser(user);
        _logger.LogInformation("User created in DB: Id={UserId}, Email={Email}", user.Id, user.Email);

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name);
        var refreshToken = _jwtService.GenerateRefreshToken();
        _logger.LogInformation("Tokens generated for new user: {UserId}", user.Id);

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
        _logger.LogInformation("Looking up user by email: {Email}", request.Email);
        var user = await _userRepository.GetUserByEmail(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: no user found with email {Email}", request.Email);
            throw new NotFoundException("Invalid email or password");
        }

        _logger.LogInformation("Verifying password for user: {UserId}", user.Id);
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: invalid password for user {UserId} ({Email})", user.Id, request.Email);
            throw new InvalidOperationException("Invalid email or password");
        }

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name);
        var refreshToken = _jwtService.GenerateRefreshToken();
        _logger.LogInformation("Login successful, tokens issued for user: {UserId} ({Email})", user.Id, user.Email);

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
        _logger.LogInformation("Processing logout: reading token expiration");
        var expiration = _jwtService.GetTokenExpiration(accessToken);
        var remaining = expiration - DateTime.UtcNow;

        if (remaining > TimeSpan.Zero)
        {
            _logger.LogInformation("Blacklisting token with remaining TTL: {Remaining}", remaining);
            await _tokenBlacklistService.BlacklistTokenAsync(accessToken, remaining);
            _logger.LogInformation("Token blacklisted successfully");
        }
        else
        {
            _logger.LogInformation("Token already expired, skipping blacklist");
        }
    }
}
