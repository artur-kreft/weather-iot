using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeatherData.Api.Host;
using WeatherData.Contract.v1;
using WeatherData.DataAccess.Db;
using WeatherData.DataAccess.Repository;
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace WeatherData.Tests.Integration
{
    public class HostTest
    {
        protected readonly HttpClient Client;

        public HostTest()
        {
            var appFactory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(it =>
                {
                    it.ConfigureServices(async services =>
                    {
                        InstallInMemoryDatabase(services);
                        await InitTestDatabase(services);
                    });
                });
            Client = appFactory.CreateClient();
        }

        private static void InstallInMemoryDatabase(IServiceCollection services)
        {
            var dbcontext = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WeatherDbContext>));
            var dbfactory = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<WeatherDbContext>));

            services.Remove(dbcontext);
            services.Remove(dbfactory);

            services.AddDbContextFactory<WeatherDbContext>(options => { options.UseInMemoryDatabase("TestDb"); },
                ServiceLifetime.Scoped);
        }

        private static async Task InitTestDatabase(IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();

            var scopedServices = scope.ServiceProvider;
            var measurement = scopedServices.GetRequiredService<IMeasurementRepository>();
            var device = scopedServices.GetRequiredService<IDeviceRepository>();

            var factory = scopedServices.GetRequiredService<IDbContextFactory<WeatherDbContext>>();
            var db = factory.CreateDbContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            await ContextInitializer.InitializeDb(measurement, device);
        }
    }
}
