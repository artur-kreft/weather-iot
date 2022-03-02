using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

namespace WeatherData.DataAccess.Db
{
    public enum DeviceStatusEnum
    {
        New = 0,
        Initializing = 1,
        Enabled = 2,
        Disabled = 3
    }

    [Index(nameof(Id))]
    public class DeviceEntity
    {
        [Key]
        [MaxLength(200)]
        public string Id { get; set; }

        [EnumDataType(typeof(DeviceStatusEnum))]
        public DeviceStatusEnum Status { get; set; }

        public DateTime LastUpdated { get; set; }
        public DateTime LastTried { get; set; }
        public int UpdateRetries { get; set; }
    }
}