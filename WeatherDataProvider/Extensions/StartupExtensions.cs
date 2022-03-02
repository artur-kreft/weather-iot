using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherData.Api.Host.Installer;

namespace WeatherData.Api.Host.Extensions
{
    public static class StartupExtensions
    {
        public static void InstallAllServices(this IServiceCollection services, IConfiguration configuration)
        {
            typeof(Startup)
                .Assembly
                .ExportedTypes
                .Where(it => typeof(IInstaller).IsAssignableFrom(it) && !it.IsInterface && !it.IsAbstract)
                .Select(Activator.CreateInstance)
                .Cast<IInstaller>()
                .ToList()
                .ForEach(it => it.InstallService(services, configuration));
        }
    }
}