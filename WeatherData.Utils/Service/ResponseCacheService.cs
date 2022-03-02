using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace WeatherData.Utils.Service
{
    public class ResponseCacheService : IResponseCacheService
    {
        private readonly IMemoryCache _cache;

        public ResponseCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task CacheResponseAsync(string key, object response, TimeSpan timeToLive)
        {
            if (response == null)
            {
                return Task.FromResult(false);
            }

            var serialized = JsonSerializer.Serialize(response);
            _cache.Set(key, serialized, new MemoryCacheEntryOptions(){SlidingExpiration = timeToLive});

            return Task.FromResult(true);
        }

        public Task<string> GetCachedResponseAsync(string key)
        {
            var response = _cache.Get<string>(key);
            return Task.FromResult(response);
        }
    }
}