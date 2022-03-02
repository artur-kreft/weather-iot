using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WeatherData.DataAccess.Db
{
    [Index(nameof(Date))]
    public class MeasurementEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SensorType { get; set; }

        public string Series { get; set; }
    }
}