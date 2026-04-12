using CrudWithAuth.DTOs;
using CrudWithAuth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrudWithAuth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Register attempt for email: {Email}", request.Email);
        try
        {
            var response = await _authService.RegisterAsync(request);
            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Register failed for {Email}: {Message}", request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);
        try
        {
            var response = await _authService.LoginAsync(request);
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Login failed for {Email}: {Message}", request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        var userId = HttpContext.Items["UserId"]?.ToString() ?? "unknown";
        _logger.LogInformation("Logout requested by user: {UserId}", userId);
        try
        {
            var token = HttpContext.Items["Token"]?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Logout failed: token not found in context for user {UserId}", userId);
                return BadRequest(new { message = "Token not found" });
            }

            await _authService.LogoutAsync(token);
            _logger.LogInformation("User logged out successfully: {UserId}", userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }
}
