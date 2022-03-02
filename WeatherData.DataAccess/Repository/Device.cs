using System;
using System.ComponentModel.DataAnnotations;
using WeatherData.DataAccess.Db;

namespace WeatherData.DataAccess.Repository
{
    public class Device
    {
        public string Id { get; set; }
        public DeviceStatusEnum Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastTried { get; set; }
        public int UpdateRetries { get; set; }
    }
}