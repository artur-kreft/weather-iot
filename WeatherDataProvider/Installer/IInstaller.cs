using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherData.Api.Host.Installer
{
    public interface IInstaller
    {
        void InstallService(IServiceCollection services, IConfiguration configuration);
    }
}