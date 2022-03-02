using System;
using System.Threading.Tasks;

namespace WeatherData.Api.Host.Service
{
    public interface IResponseCacheService
    {
        Task CacheResponseAsync(string key, object response, TimeSpan timeToLive);
        Task<string> GetCachedResponseAsync(string key);
    }
}