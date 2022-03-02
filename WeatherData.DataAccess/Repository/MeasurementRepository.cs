using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherData.DataAccess.Db;
using WeatherData.DataAccess.Utils;
using WeatherData.Model;

namespace WeatherData.DataAccess.Repository
{
    public class MeasurementRepository : IMeasurementRepository
    {
        private readonly ILogger _logger;
        private readonly IDbContextFactory<WeatherDbContext> _dbContextFactory;
        private readonly IMapper _mapper;
        private readonly SemaphoreSlim _semaphore;

        public MeasurementRepository(ILogger<MeasurementRepository> logger, IConfiguration configuration, IDbContextFactory<WeatherDbContext> dbContextFactory, IMapper mapper)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _mapper = mapper;
            
            _semaphore = new SemaphoreSlim(Int32.Parse(configuration["Threads:SemaphoreCount"]));
        }
        
        public async Task<Result<string>> GetJsonAsync(DateTime date, string deviceId)
        {
            try
            {
                await using var db = _dbContextFactory.CreateDbContext();
                var entities = await db.Measurements
                    .Where(it => it.Date == date && it.DeviceId == deviceId)
                    .ToListAsync();
                entities = entities.Distinct(new DistinctMeasurementComparer()).ToList();
                var decompressed = new List<string>();
                foreach (var entity in entities)
                {
                    decompressed.Add(await entity.Series.FromBrotliAsync());
                }

                var resultJson = $"[{string.Join(", ", decompressed)}]";
                if (!decompressed.Any())
                {
                    resultJson = "";
                }

                return Result.Ok(resultJson);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not fetch data for {deviceId}: {date.ToShortDateString()}");
                return Result.Fail("Could not fetch data");
            }
        }

        public async Task<Result<string>> GetJsonAsync(DateTime date, string deviceId, string sensor)
        {
            try
            {
                await using var db = _dbContextFactory.CreateDbContext();
                var entity = await db.Measurements
                    .Where(it => it.Date == date && it.DeviceId == deviceId && it.SensorType == sensor)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    return Result.Ok("");
                }

                var decompressed = await entity.Series.FromBrotliAsync();

                return Result.Ok(decompressed);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not fetch data for {deviceId}/{sensor}: {date.ToShortDateString()}");
                return Result.Fail("Could not fetch data");
            }
        }

        public async Task<Result> AddAsync(List<SensorData> sensorData)
        {
            List<MeasurementEntity> toSave = new List<MeasurementEntity>();
            foreach (var s in sensorData)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(s);
                var compressed = await json.ToBrotliAsync();
                var entity = new MeasurementEntity()
                {
                    Date = s.Date,
                    DeviceId = s.Device,
                    SensorType = s.Sensor,
                    Series = compressed
                };
                toSave.Add(entity);
            }
            try
            {
                await _semaphore.WaitAsync();
                await using var db = _dbContextFactory.CreateDbContext();
                await db.Measurements.AddRangeAsync(toSave);
                await db.SaveChangesAsync();
                _semaphore.Release();
                return Result.Ok();
            }
            catch (Exception e)
            {
                _semaphore.Release();
                _logger.LogError(e, $"Failed to add data");
                return Result.Fail("Failed to add data");
            }
        }

        private class DistinctMeasurementComparer : IEqualityComparer<MeasurementEntity>
        {
            public bool Equals(MeasurementEntity x, MeasurementEntity y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.SensorType == y.SensorType;
            }

            public int GetHashCode(MeasurementEntity obj)
            {
                return (obj.SensorType != null ? obj.SensorType.GetHashCode() : 0);
            }
        }
    }
}