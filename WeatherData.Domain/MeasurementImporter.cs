using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherData.BlobAccess;
using WeatherData.DataAccess.Db;
using WeatherData.DataAccess.Repository;
using WeatherData.Model;

namespace WeatherData.Domain
{
    public class MeasurementImporter : IMeasurementImporter
    {
        private readonly IMeasurementStorage _storage;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<MeasurementImporter> _logger;

        private static int _updateRetriesDaysLimit = 7;
        private static int _tryToReenableDeviceDaysPeriod = 7;

        public MeasurementImporter(IMeasurementStorage storage, IMeasurementRepository measurementRepository, IDeviceRepository deviceRepository, ILogger<MeasurementImporter> logger, IConfiguration configuration)
        {
            _storage = storage;
            _measurementRepository = measurementRepository;
            _deviceRepository = deviceRepository;
            _logger = logger;

            _updateRetriesDaysLimit = Int32.Parse(configuration["Importer:UpdateRetriesDaysLimit"]);
            _tryToReenableDeviceDaysPeriod = Int32.Parse(configuration["Importer:TryToReenableDeviceDaysPeriod"]);
        }

        public async Task<Result> ImportMeasurementsAsync()
        {
            var devicesResult = await _storage.GetDevicesAsync();

            if (devicesResult.IsFailed)
            {
                return Result.Fail(devicesResult.Errors[0]);
            }

            List<IError> errors = new List<IError>();
            foreach (var device in devicesResult.Value)
            {
                var result = await ImportSensorData(device);
                if (result.IsFailed)
                {
                    errors.AddRange(result.Errors);
                }
            }

            if (errors.Any())
            {
                return Result.Fail(string.Join(";", errors));
            }

            return Result.Ok();
        }

        private async Task<Result> ImportSensorData(DeviceType device)
        {
            var result = await _deviceRepository.GetOrAddAsync(device.DeviceId, device.SensorType);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors[0]);
            }

            var savedDevice = result.Value;

            if (savedDevice.Status == DeviceStatusEnum.New)
            {
                await ImportFromHistory(device, savedDevice);

                bool nothingImported = savedDevice.Status == DeviceStatusEnum.New;
                if (nothingImported)
                {
                    _logger.LogError("Missing history file");
                    return Result.Fail("Missing history file");
                }
            }

            if (savedDevice.Status == DeviceStatusEnum.Initializing)
            {
                await ImportFromHistory(device, savedDevice, savedDevice.LastUpdated);
            }

            if (savedDevice.Status == DeviceStatusEnum.Enabled)
            {
                await ImportFromLastFiles(device, savedDevice);
                await TryDisableDevice(savedDevice);
            }

            if (savedDevice.Status == DeviceStatusEnum.Disabled)
            {
                await TryReenableDevice(savedDevice);
            }

            return Result.Ok();
        }

        private async Task<Result> ImportFromHistory(DeviceType device, Device savedDevice, DateTime? lastUpdated = null)
        {
            var result = await _storage.ParseHistoryAsync(device.DeviceId, device.SensorType, lastUpdated,
                async (date, parsed) =>
                {
                    await _measurementRepository.AddAsync(parsed);
                    await TouchDevice(savedDevice, DeviceStatusEnum.Initializing, date);
                });

            if (result.IsSuccess && savedDevice.LastUpdated != new DateTime())
            {
                await TouchDevice(savedDevice, DeviceStatusEnum.Enabled);
            }

            return result;
        }

        private async Task<Result> ImportFromLastFiles(DeviceType device, Device savedDevice)
        {
            var cursor = savedDevice.LastUpdated;
            var end = DateTime.Today.AddDays(-1);
            List<SensorData> parsed = new List<SensorData>();
            List<IError> errors = new List<IError>();

            while (cursor < end)
            {
                cursor = cursor.AddDays(1);

                var result = await _storage.ParseFileAsync(device.DeviceId, device.SensorType, cursor);
                if (result.IsFailed)
                {
                    errors.AddRange(result.Errors);
                    continue;
                }

                parsed.Add(result.Value);
            }

            if (parsed.Any())
            {
                await _measurementRepository.AddAsync(parsed);
                await TouchDevice(savedDevice, DeviceStatusEnum.Enabled, parsed.Max(it => it.Date));
            }

            if (errors.Any())
            {
                return Result.Fail(errors[0]);
            }

            return Result.Ok();
        }

        private async Task<Result> TryReenableDevice(Device savedDevice)
        {
            var diff = (DateTime.Today - savedDevice.LastTried).Days;
            if (diff % _tryToReenableDeviceDaysPeriod == 0)
            {
                savedDevice.UpdateRetries = 0;
                savedDevice.Status = DeviceStatusEnum.Enabled;
                var result = await _deviceRepository.UpdateAsync(savedDevice);
                return result;
            }

            return Result.Ok();
        }

        private async Task<Result> TryDisableDevice(Device savedDevice)
        {
            if (savedDevice.LastTried < DateTime.Today)
            {
                savedDevice.UpdateRetries++;
                if (savedDevice.UpdateRetries > _updateRetriesDaysLimit)
                {
                    savedDevice.Status = DeviceStatusEnum.Disabled;
                    var result = await _deviceRepository.UpdateAsync(savedDevice);
                    return result;
                }
            }

            return Result.Ok();
        }

        private async Task<Result> TouchDevice(Device savedDevice, DeviceStatusEnum newStatus, DateTime lastUpdated)
        {
            savedDevice.LastUpdated = lastUpdated;
            savedDevice.Status = newStatus;
            savedDevice.LastTried = DateTime.Now;
            savedDevice.UpdateRetries = 0;
            var result = await _deviceRepository.UpdateAsync(savedDevice);
            return result;
        }

        private async Task<Result> TouchDevice(Device savedDevice, DeviceStatusEnum newStatus)
        {
            savedDevice.Status = newStatus;
            savedDevice.LastTried = DateTime.Now;
            savedDevice.UpdateRetries = 0;
            var result = await _deviceRepository.UpdateAsync(savedDevice);

            return result;
        }
    }
}
