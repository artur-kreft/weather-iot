using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherData.DataAccess.Db;
using WeatherData.DataAccess.Repository;
using WeatherData.Model;

namespace WeatherData.Tests.Integration
{
    public static class ContextInitializer
    {
        public static async Task InitializeDb(IMeasurementRepository measurementRepository, IDeviceRepository deviceRepository)
        {
            foreach (var device in DeviceNew)
            {
                await deviceRepository.GetOrAddAsync(device[0], device[1]);
            }

            foreach (var device in DeviceUpdated)
            {
                await deviceRepository.UpdateAsync(device);
            }

            await measurementRepository.AddAsync(Measurements);
        }

        public static List<string[]> DeviceNew = new List<string[]>()
        {
            new [] {"gdansk", "humidity"},
            new [] {"dockan", "humidity"},
            new [] {"dockan", "temperature"},
            new [] {"dockan", "pressure"},
            new [] {"jordan", "michael"},
            new [] {"jordan", "speed"},
        };

        public static List<Device> DeviceUpdated = new List<Device>()
        {
            new Device()
            {
                Id = "dockan/temperature",
                LastTried = new DateTime(2020, 2, 1),
                LastUpdated = new DateTime(2019, 9, 10),
                Status = DeviceStatusEnum.Initializing
            },
            new Device()
            {
                Id = "dockan/pressure",
                LastTried = new DateTime(2020, 2, 1),
                LastUpdated = new DateTime(2020, 9, 10),
                Status = DeviceStatusEnum.Enabled
            },
            new Device()
            {
                Id = "dockan/humidity",
                LastTried = new DateTime(2021, 2, 1),
                LastUpdated = new DateTime(2019, 9, 10),
                Status = DeviceStatusEnum.Disabled
            },
            new Device()
            {
                Id = "jordan/michael",
                LastTried = new DateTime(2021, 2, 1),
                LastUpdated = new DateTime(2021, 9, 10),
                Status = DeviceStatusEnum.Enabled
            }
        };

        public static DateTime Date = new DateTime(2019, 1, 1);
        
        private static readonly List<string[]> _series = new List<string[]>()
        {
            new [] { "01:12:04", ",07" },
            new [] { "01:13:06", "1,07" },
            new [] { "01:14:59", "100,07" },
            new [] { "02:44:09", "1,87"},
            new [] { "03:47:02", "208647,07" },
            new [] { "04:49:01", "2435543,07" },
            new [] { "04:50:04", "0,007" },
            new [] { "04:50:05", "0,17" },
        };

        public static List<SensorData> Measurements =
            new List<SensorData>
            {
                new(Date, "dockan", "temperature", _series),
                new(Date, "dockan", "temperature", _series),
                new(Date, "dockan", "temperature", _series),
                new(Date, "dockan", "pressure", _series),
                new(Date, "dockan", "pressure", _series),
                new(Date, "dockan", "humidity", _series),
                new(Date, "jordan", "michael", _series)
            };
    }
}