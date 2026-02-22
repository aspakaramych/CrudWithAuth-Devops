using StackExchange.Redis;

namespace CrudWithAuth.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public TokenBlacklistService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    public async Task BlacklistTokenAsync(string token, TimeSpan expiry)
    {
        var key = GetCacheKey(token);
        await _db.StringSetAsync(key, "revoked", expiry, flags: CommandFlags.DemandMaster);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        var key = GetCacheKey(token);
        var value = await _db.StringGetAsync(key, flags: CommandFlags.PreferReplica);
        return value.HasValue;
    }

    private static string GetCacheKey(string token) => $"blacklist:{token}";
}