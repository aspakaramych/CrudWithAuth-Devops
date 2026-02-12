using CrudWithAuth.Services;

namespace CrudWithAuth.Middleware;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService tokenBlacklistService)
    {
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (path.Contains("/auth/login") || path.Contains("/auth/register")))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized: Missing or invalid token");
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        var isBlacklisted = await tokenBlacklistService.IsTokenBlacklistedAsync(token);
        if (isBlacklisted)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized: Token has been revoked");
            return;
        }

        context.Items["Token"] = token;

        await _next(context);
    }
}
