using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CrudWithAuth.Configuration;
using CrudWithAuth.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CrudWithAuth.Tests.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "test-super-secret-key-that-is-long-enough-for-hmac",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        _jwtService = new JwtService(Options.Create(_jwtSettings));
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";

        var token = _jwtService.GenerateAccessToken(userId, email, name);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainCorrectClaims()
    {
        var userId = Guid.NewGuid();
        var email = "claims@example.com";
        var name = "Claims User";

        var token = _jwtService.GenerateAccessToken(userId, email, name);
        var principal = _jwtService.ValidateToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirstValue(JwtRegisteredClaimNames.Sub).Should().Be(userId.ToString());
        principal.FindFirstValue(JwtRegisteredClaimNames.Email).Should().Be(email);
        principal.FindFirstValue(JwtRegisteredClaimNames.Name).Should().Be(name);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnPrincipal()
    {
        var userId = Guid.NewGuid();
        var token = _jwtService.GenerateAccessToken(userId, "test@test.com", "Test");

        var principal = _jwtService.ValidateToken(token);

        principal.Should().NotBeNull();
        principal.Should().BeOfType<ClaimsPrincipal>();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        var principal = _jwtService.ValidateToken("this.is.not.a.valid.jwt.token");

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ShouldReturnNull()
    {
        var userId = Guid.NewGuid();
        var token = _jwtService.GenerateAccessToken(userId, "test@test.com", "Test");

        var parts = token.Split('.');
        parts[2] = "invalidsignature";
        var tamperedToken = string.Join('.', parts);

        var principal = _jwtService.ValidateToken(tamperedToken);

        principal.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithValidToken_ShouldReturnUserId()
    {
        var userId = Guid.NewGuid();
        var token = _jwtService.GenerateAccessToken(userId, "test@test.com", "Test");

        var result = _jwtService.GetUserIdFromToken(token);

        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ShouldReturnNull()
    {
        var result = _jwtService.GetUserIdFromToken("invalid-token");

        result.Should().BeNull();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        var token = _jwtService.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GetTokenExpiration_WithValidToken_ShouldReturnFutureTime()
    {
        var userId = Guid.NewGuid();
        var token = _jwtService.GenerateAccessToken(userId, "test@test.com", "Test");

        var expiration = _jwtService.GetTokenExpiration(token);

        expiration.Should().BeAfter(DateTime.UtcNow);
        expiration.Should().BeBefore(DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes + 1));
    }

    [Fact]
    public void GenerateAccessToken_WithDifferentUsers_ShouldReturnDifferentTokens()
    {
        var token1 = _jwtService.GenerateAccessToken(Guid.NewGuid(), "user1@test.com", "User 1");
        var token2 = _jwtService.GenerateAccessToken(Guid.NewGuid(), "user2@test.com", "User 2");

        token1.Should().NotBe(token2);
    }
}
