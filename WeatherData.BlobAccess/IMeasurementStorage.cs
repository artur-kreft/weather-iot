using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using WeatherData.DataAccess.Repository;
using WeatherData.Model;

namespace WeatherData.BlobAccess
{
    public interface IMeasurementStorage
    {
        Task<Result<List<DeviceType>>> GetDevicesAsync();
        Task<Result> ParseHistoryAsync(string device, string sensor, DateTime? lastUpdated,
            Func<DateTime, List<SensorData>, Task> onFileParsed);
        Task<Result<SensorData>> ParseFileAsync(string device, string sensor, DateTime date);
    }
}