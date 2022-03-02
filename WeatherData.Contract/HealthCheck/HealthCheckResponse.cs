using System;
using System.Collections.Generic;

namespace WeatherData.Contract.HealthCheck
{
    public class HealthCheckResponse
    {
        public string Status { get; set; }
        public IEnumerable<HealthCheck> Types { get; set; }
        public TimeSpan Duration { get; set; }
    }
}