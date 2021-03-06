using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using WeatherData.Domain;

namespace WeatherData.Scheduler.Host.Installer
{
    public class ApiInstaller : IInstaller
    {
        public void InstallService(IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WeatherDataProvider", Version = "v1" });
            });

            services.AddScoped<IMeasurementImporter, MeasurementImporter>();
        }
    }
}