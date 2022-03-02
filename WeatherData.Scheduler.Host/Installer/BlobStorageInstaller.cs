using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherData.BlobAccess;

namespace WeatherData.Scheduler.Host.Installer
{
    public class BlobStorageInstaller : IInstaller
    {
        public void InstallService(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAzureClients(builder => {
                var connectionString = configuration.GetConnectionString("BlobStorageConnection");
                builder.AddBlobServiceClient(connectionString);
            });

            services.AddScoped<IMeasurementStorage, MeasurementStorage>();
        }
    }
}