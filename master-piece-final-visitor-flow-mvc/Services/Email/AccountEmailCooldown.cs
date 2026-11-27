using Microsoft.Extensions.Caching.Memory;

namespace SkillsSwap.Services.Email;

public sealed class AccountEmailCooldown(IMemoryCache cache)
{
    private readonly object sync = new();

    public bool TryBegin(string purpose, string normalizedEmail)
    {
        lock (sync)
        {
            var key = (purpose, normalizedEmail);
            if (cache.TryGetValue(key, out _)) return false;
            cache.Set(key, true, TimeSpan.FromMinutes(1));
            return true;
        }
    }
}
