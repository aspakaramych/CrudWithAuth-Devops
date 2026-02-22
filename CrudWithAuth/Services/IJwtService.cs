using System.Security.Claims;

namespace CrudWithAuth.Services;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string name);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Guid? GetUserIdFromToken(string token);
    DateTime GetTokenExpiration(string token);
}
