using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CrudWithAuth.DTOs;
using CrudWithAuth.Exceptions;
using CrudWithAuth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrudWithAuth.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAllUsers()
    {
        _logger.LogInformation("GetAllUsers called by user: {UserId}", GetCurrentUserId());
        try
        {
            var users = await _userService.GetAllUsers();
            var list = users.ToList();
            _logger.LogInformation("GetAllUsers returned {Count} users", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllUsers");
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("GetCurrentUser called, userId: {UserId}", userId);
        try
        {
            if (userId == null)
            {
                _logger.LogWarning("GetCurrentUser: invalid token claims, userId is null");
                return Unauthorized(new { message = "Invalid token claims" });
            }

            var user = await _userService.GetUserById(userId.Value);
            _logger.LogInformation("GetCurrentUser success for userId: {UserId}", userId);
            return Ok(user);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("GetCurrentUser: user not found, userId: {UserId} — {Message}", userId, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCurrentUser for userId: {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetUserById(Guid id)
    {
        _logger.LogInformation("GetUserById called for id: {TargetUserId}", id);
        try
        {
            var user = await _userService.GetUserById(id);
            _logger.LogInformation("GetUserById success for id: {TargetUserId}", id);
            return Ok(user);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("GetUserById: user not found, id: {TargetUserId} — {Message}", id, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserById for id: {TargetUserId}", id);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult> CreateUser([FromBody] UserRequest request)
    {
        _logger.LogInformation("CreateUser called with email: {Email}", request.Email);
        try
        {
            await _userService.CreateUser(request);
            _logger.LogInformation("CreateUser success for email: {Email}", request.Email);
            return CreatedAtAction(nameof(GetUserById), new { id = Guid.NewGuid() }, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateUser for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateUser(Guid id, [FromBody] UserRequest request)
    {
        _logger.LogInformation("UpdateUser called for id: {TargetUserId}", id);
        try
        {
            await _userService.UpdateUser(request, id);
            _logger.LogInformation("UpdateUser success for id: {TargetUserId}", id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("UpdateUser: user not found, id: {TargetUserId} — {Message}", id, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateUser for id: {TargetUserId}", id);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("DeleteUser called for id: {TargetUserId}", id);
        try
        {
            await _userService.DeleteUser(id);
            _logger.LogInformation("DeleteUser success for id: {TargetUserId}", id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("DeleteUser: user not found, id: {TargetUserId} — {Message}", id, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteUser for id: {TargetUserId}", id);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpDelete("me")]
    public async Task<ActionResult> DeleteCurrentUser()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("DeleteCurrentUser called by userId: {UserId}", userId);
        try
        {
            if (userId == null)
            {
                _logger.LogWarning("DeleteCurrentUser: invalid token claims");
                return Unauthorized(new { message = "Invalid token claims" });
            }

            await _userService.DeleteUser(userId.Value);
            _logger.LogInformation("DeleteCurrentUser success for userId: {UserId}", userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("DeleteCurrentUser: user not found, userId: {UserId} — {Message}", userId, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteCurrentUser for userId: {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
