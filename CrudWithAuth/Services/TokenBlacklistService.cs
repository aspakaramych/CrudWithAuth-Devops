using Microsoft.Extensions.Caching.Distributed;

namespace CrudWithAuth.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;

    public TokenBlacklistService(IDistributedCache cache)
    {
        _cache = cache;
    }
    
    public async Task BlacklistTokenAsync(string token, TimeSpan expiry)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        };
        
        await _cache.SetStringAsync(GetCacheKey(token), "revoked", options);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        var cachedValue = await _cache.GetStringAsync(GetCacheKey(token));
        return cachedValue != null;
    }
    
    private string GetCacheKey(string token) => $"blacklist:{token}";
}