using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherData.DataAccess.Db;
using WeatherData.DataAccess.Repository;

namespace WeatherData.Api.Host.Installer
{
    public class DbInstaller : IInstaller
    {
        public void InstallService(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<WeatherDbContext>(it =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                it.UseSqlServer(connectionString);
            });

            services.AddDbContextFactory<WeatherDbContext>(it =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                it.UseSqlServer(connectionString, _ =>
                {
                    _.EnableRetryOnFailure();
                    _.CommandTimeout(720);
                });
            }, ServiceLifetime.Scoped);

            services.AddScoped<IMeasurementRepository, MeasurementRepository>();
            services.AddScoped<IDeviceRepository, DeviceRepository>();
        }
    }
}