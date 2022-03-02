using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherData.Api.Host.Service;

namespace WeatherData.Api.Host.Installer
{
    public class CacheInstaller :IInstaller
    {
        public void InstallService(IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.AddSingleton<IResponseCacheService, ResponseCacheService>();
        }
    }
}