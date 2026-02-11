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
        throw new NotImplementedException();
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        throw new NotImplementedException();
    }
}