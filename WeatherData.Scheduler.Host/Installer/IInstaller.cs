using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherData.Scheduler.Host.Installer
{
    public interface IInstaller
    {
        void InstallService(IServiceCollection services, IConfiguration configuration);
    }
}