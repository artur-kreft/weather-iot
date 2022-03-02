using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherData.DataAccess.Db;

namespace WeatherData.Scheduler.Host.Installer
{
    public class HealthCheckInstaller : IInstaller
    {
        public void InstallService(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<WeatherDbContext>()
                .AddAzureBlobStorage(configuration.GetConnectionString("BlobStorageConnection"));
        }
    }
}