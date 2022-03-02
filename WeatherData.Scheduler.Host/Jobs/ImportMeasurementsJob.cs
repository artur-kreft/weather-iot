using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using WeatherData.BlobAccess;
using WeatherData.DataAccess.Repository;
using WeatherData.Domain;

namespace WeatherData.Scheduler.Host.Jobs
{
    [DisallowConcurrentExecution]
    public class ImportMeasurementsJob : IJob
    {
        private readonly ILogger<ImportMeasurementsJob> _logger;
        private readonly IMeasurementImporter _importer;
        private readonly IMeasurementRepository _repository;
        private readonly IMeasurementStorage _storage;

        public ImportMeasurementsJob(ILogger<ImportMeasurementsJob> logger, IMeasurementImporter importer)
        {
            _logger = logger;
            _importer = importer;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("START Importing measurements");
            try
            {
                await _importer.ImportMeasurementsAsync();
                _logger.LogInformation("FINISHED importing measurements");
            }
            catch (Exception e)
            {
                _logger.LogError("FAILED importing measurements", e);
            }
        }
    }
}