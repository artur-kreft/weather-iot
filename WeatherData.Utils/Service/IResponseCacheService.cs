using System;
using System.Threading.Tasks;

namespace WeatherData.Utils.Service
{
    public interface IResponseCacheService
    {
        Task CacheResponseAsync(string key, object response, TimeSpan timeToLive);
        Task<string> GetCachedResponseAsync(string key);
    }
}