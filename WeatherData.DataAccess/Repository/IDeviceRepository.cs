using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;

namespace WeatherData.DataAccess.Repository
{
    public interface IDeviceRepository
    {
        Task<Result<Device>> GetOrAddAsync(string device, string sensor);
        Task<Result> UpdateAsync(Device savedDevice);
        Task<Result<bool>> IsValid(string deviceId);
        Task<Result<bool>> IsValid(string deviceId, string sensor);
    }
}