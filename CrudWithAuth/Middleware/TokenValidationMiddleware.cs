using System.IdentityModel.Tokens.Jwt;
using CrudWithAuth.Services;

namespace CrudWithAuth.Middleware;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenValidationMiddleware> _logger;

    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/openapi/v1.json",
        "/scalar",
        "/metrics"
    };

    public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IJwtService jwtService,
        ITokenBlacklistService tokenBlacklistService)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        _logger.LogInformation("Incoming request: {Method} {Path}", method, path);

        if (IsPublicPath(path))
        {
            _logger.LogInformation("Public path, skipping token validation: {Path}", path);
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Missing or invalid Authorization header for {Method} {Path}", method, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Unauthorized: Missing or invalid Authorization header" });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        var principal = jwtService.ValidateToken(token);
        if (principal == null)
        {
            _logger.LogWarning("Invalid or expired JWT token for {Method} {Path}", method, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Unauthorized: Invalid or expired token" });
            return;
        }

        var isBlacklisted = await tokenBlacklistService.IsTokenBlacklistedAsync(token);
        if (isBlacklisted)
        {
            _logger.LogWarning("Revoked token used for {Method} {Path}", method, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Unauthorized: Token has been revoked" });
            return;
        }

        var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        context.Items["Token"] = token;
        context.Items["UserId"] = userId;
        context.User = principal;

        _logger.LogInformation("Token validated, userId: {UserId}, proceeding to {Method} {Path}", userId, method, path);
        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        foreach (var publicPath in PublicPaths)
        {
            if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
