using StackExchange.Redis;

namespace CrudWithAuth.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<TokenBlacklistService> _logger;

    public TokenBlacklistService(IConnectionMultiplexer redis, ILogger<TokenBlacklistService> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _logger = logger;
    }

    public async Task BlacklistTokenAsync(string token, TimeSpan expiry)
    {
        var key = GetCacheKey(token);
        _logger.LogInformation("Blacklisting token in Redis, key: {Key}, TTL: {Expiry}", key[..20] + "...", expiry);
        await _db.StringSetAsync(key, "revoked", expiry, flags: CommandFlags.DemandMaster);
        _logger.LogInformation("Token blacklisted successfully");
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        var key = GetCacheKey(token);
        var value = await _db.StringGetAsync(key, flags: CommandFlags.PreferReplica);
        var isBlacklisted = value.HasValue;
        if (isBlacklisted)
            _logger.LogWarning("Blacklisted token detected, key: {Key}", key[..20] + "...");
        return isBlacklisted;
    }

    private static string GetCacheKey(string token) => $"blacklist:{token}";
}