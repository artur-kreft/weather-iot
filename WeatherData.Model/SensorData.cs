using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WeatherData.Model.Json;

namespace WeatherData.Model
{
    public record SensorData
    {
        public SensorData(DateTime date, string device, string sensor, List<string[]> values)
        {
            Date = date;
            Device = device;
            Sensor = sensor;
            Values = values;
        }

        [JsonConverter(typeof(DateFormatConverter))]
        [JsonPropertyName("t")]
        public DateTime Date { get; }

        [JsonPropertyName("d")]
        public string Device { get; }

        [JsonPropertyName("s")]
        public string Sensor { get; }

        [JsonPropertyName("v")]
        public List<string[]> Values { get; }
    }
}