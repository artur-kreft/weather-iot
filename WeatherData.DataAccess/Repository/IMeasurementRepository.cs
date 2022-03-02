using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using WeatherData.Model;

namespace WeatherData.DataAccess.Repository
{
    public interface IMeasurementRepository
    {
        Task<Result<string>> GetJsonAsync(DateTime date, string deviceId);
        Task<Result<string>> GetJsonAsync(DateTime date, string deviceId, string sensor);
        Task<Result> AddAsync(List<SensorData> sensorData);
    }
}