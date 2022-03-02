using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WeatherData.DataAccess.Db;
using WeatherData.DataAccess.Repository;

namespace WeatherData.Api.Host.Installer
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