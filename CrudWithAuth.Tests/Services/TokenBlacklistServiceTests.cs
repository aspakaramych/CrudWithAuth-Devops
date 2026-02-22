using CrudWithAuth.Services;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CrudWithAuth.Tests.Services;

public class TokenBlacklistServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _multiplexerMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly TokenBlacklistService _service;

    public TokenBlacklistServiceTests()
    {
        _multiplexerMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>();

        _multiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);

        _service = new TokenBlacklistService(_multiplexerMock.Object);
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldStoreTokenInRedis()
    {
        var token = "test-jwt-token";
        var expiry = TimeSpan.FromHours(1);

        _dbMock
            .Setup(x => x.StringSetAsync(
                $"blacklist:{token}",
                "revoked",
                expiry,
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await _service.BlacklistTokenAsync(token, expiry);

        _dbMock.Verify(x => x.StringSetAsync(
            $"blacklist:{token}",
            "revoked",
            expiry,
            It.IsAny<bool>(),
            It.IsAny<When>(),
            CommandFlags.DemandMaster
        ), Times.Once);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WhenTokenIsBlacklisted_ShouldReturnTrue()
    {
        var token = "blacklisted-jwt-token";

        _dbMock
            .Setup(x => x.StringGetAsync($"blacklist:{token}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("revoked"));

        var result = await _service.IsTokenBlacklistedAsync(token);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WhenTokenIsNotBlacklisted_ShouldReturnFalse()
    {
        var token = "valid-jwt-token";

        _dbMock
            .Setup(x => x.StringGetAsync($"blacklist:{token}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var result = await _service.IsTokenBlacklistedAsync(token);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldUseDemandMasterFlag()
    {
        var token = "token-for-flag-test";
        var expiry = TimeSpan.FromMinutes(30);

        _dbMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await _service.BlacklistTokenAsync(token, expiry);

        _dbMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            CommandFlags.DemandMaster
        ), Times.Once);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_ShouldUsePreferReplicaFlag()
    {
        var token = "token-for-replica-test";

        _dbMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        await _service.IsTokenBlacklistedAsync(token);

        _dbMock.Verify(x => x.StringGetAsync(
            It.IsAny<RedisKey>(),
            CommandFlags.PreferReplica
        ), Times.Once);
    }
}
