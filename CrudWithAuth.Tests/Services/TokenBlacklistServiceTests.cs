using System.Text;
using CrudWithAuth.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace CrudWithAuth.Tests.Services;

public class TokenBlacklistServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly TokenBlacklistService _service;

    public TokenBlacklistServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _service = new TokenBlacklistService(_cacheMock.Object);
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldStoreTokenInCache()
    {
        var token = "test-token-123";
        var expiry = TimeSpan.FromHours(24);

        await _service.BlacklistTokenAsync(token, expiry);

        _cacheMock.Verify(x => x.SetAsync(
            "blacklist:test-token-123",
            It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "revoked"),
            It.Is<DistributedCacheEntryOptions>(opts => 
                opts.AbsoluteExpirationRelativeToNow == expiry),
            default
        ), Times.Once);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WhenTokenIsBlacklisted_ShouldReturnTrue()
    {
        var token = "blacklisted-token";
        var cachedValue = Encoding.UTF8.GetBytes("revoked");
        _cacheMock.Setup(x => x.GetAsync("blacklist:blacklisted-token", default))
            .ReturnsAsync(cachedValue);

        var result = await _service.IsTokenBlacklistedAsync(token);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WhenTokenIsNotBlacklisted_ShouldReturnFalse()
    {
        var token = "valid-token";
        _cacheMock.Setup(x => x.GetAsync("blacklist:valid-token", default))
            .ReturnsAsync((byte[]?)null);

        var result = await _service.IsTokenBlacklistedAsync(token);

        result.Should().BeFalse();
    }
}
