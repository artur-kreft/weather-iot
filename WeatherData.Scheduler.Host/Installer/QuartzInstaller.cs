using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Quartz;
using WeatherData.Scheduler.Host.Jobs;

namespace WeatherData.Scheduler.Host.Installer
{
    public class QuartzInstaller : IInstaller
    {
        public void InstallService(IServiceCollection services, IConfiguration configuration)
        {
            services.AddQuartz(it =>
            {
                it.UseMicrosoftDependencyInjectionJobFactory();
                it.UseInMemoryStore();
                it.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

                var jobKey = new JobKey("import-measurements", "weather-data");
                it.AddJob<ImportMeasurementsJob>(jobKey);

                it.AddTrigger(t => t
                    .WithIdentity("Cron Trigger")
                    .ForJob(jobKey)
                    .WithCronSchedule("0 0 1 ? * * *")
                );

            });

            services.AddQuartzServer(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;
            });
        }
    }
}