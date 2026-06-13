using GestorInventario.Interfaces.Application.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace GestorInventario.Application.Services.Common
{
    public class HybridCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer? _redis;

        public HybridCacheService(IDistributedCache distributedCache, IMemoryCache memoryCache, IConnectionMultiplexer? redis)
        {
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _redis = redis;
        }

        private bool UseRedis() => _redis != null && _redis.IsConnected;

        public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            if (UseRedis())
                await _distributedCache.SetStringAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromDays(30) });
            else
                _memoryCache.Set(key, value);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            if (UseRedis())
                return await _distributedCache.GetStringAsync(key);

            _memoryCache.TryGetValue(key, out string? value);
            return value;
        }
        public async Task RemoveAsync(string key)
        {
            if (UseRedis())
            {
                await _distributedCache.RemoveAsync(key);
            }
            else
            {
                _memoryCache.Remove(key);
              
                await Task.CompletedTask;
            }
        }
        // Métodos para cuando sea necesario usar la memoria local
        public void SetLocal(string key, string value) => _memoryCache.Set(key, value);
        public string? GetLocal(string key) { _memoryCache.TryGetValue(key, out string? value); return value; }
    }
}
