using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WeatherData.DataAccess.Db;

namespace WeatherData.DataAccess.Repository
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly ILogger _logger;
        private readonly IDbContextFactory<WeatherDbContext> _dbContextFactory;
        private readonly IMapper _mapper;
        private readonly SemaphoreSlim _semaphore;

        public DeviceRepository(ILogger<DeviceRepository> logger, IConfiguration configuration, IDbContextFactory<WeatherDbContext> dbContextFactory, IMapper mapper)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _mapper = mapper;

            _semaphore = new SemaphoreSlim(Int32.Parse(configuration["Threads:SemaphoreCount"]));
        }

        public async Task<Result<Device>> GetOrAddAsync(string device, string sensor)
        {
            try
            {
                await using var db = _dbContextFactory.CreateDbContext();
                string id = $"{device}/{sensor}";
                DeviceEntity dev = await db.Devices.FirstOrDefaultAsync(it => it.Id == id);
                if (dev == null)
                {
                    dev = new DeviceEntity() { Id = id, Status = DeviceStatusEnum.New };
                    await _semaphore.WaitAsync();
                    await db.Devices.AddAsync(dev);
                    await db.SaveChangesAsync();
                    _semaphore.Release();
                }
                
                return Result.Ok(_mapper.Map<Device>(dev));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not get device {device}/{sensor}");
                return Result.Fail($"Could not get device {device}/{sensor}");
            }
        }

        public async Task<Result> UpdateAsync(Device device)
        {
            try
            {
                await using var db = _dbContextFactory.CreateDbContext();
                DeviceEntity dev = await db.Devices.FirstOrDefaultAsync(it => it.Id == device.Id);
                if (dev == null)
                {
                    _logger.LogError($"Could not update device {device.Id}, because it does not exist");
                    return Result.Fail($"{device} does not exist");
                }

                dev.Status = device.Status;
                dev.LastUpdated = device.LastUpdated;
                dev.LastTried = device.LastTried;
                dev.UpdateRetries = device.UpdateRetries;

                await _semaphore.WaitAsync();
                await db.SaveChangesAsync();
                _semaphore.Release();

                return Result.Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to update device {device}");
                return Result.Fail($"Failed to update device {device}");
            }
        }

        public async Task<Result<bool>> IsValid(string deviceId)
        {
            try
            {
                await using var db = _dbContextFactory.CreateDbContext();
                var exists = await db.Devices.AnyAsync(it => it.Id.Contains(deviceId) && it.Status != DeviceStatusEnum.New);
                return Result.Ok(exists);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not fetch data for {deviceId}");
                return Result.Fail("Could not fetch data");
            }
        }

        public async Task<Result<bool>> IsValid(string deviceId, string sensor)
        {
            string id = $"{deviceId}/{sensor}";

            try
            {
                await using var db = _dbContextFactory.CreateDbContext();
                var exists = await db.Devices.AnyAsync(it => it.Id == id && it.Status != DeviceStatusEnum.New);
                return Result.Ok(exists);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not fetch data for {deviceId}");
                return Result.Fail("Could not fetch data");
            }
        }
    }
}