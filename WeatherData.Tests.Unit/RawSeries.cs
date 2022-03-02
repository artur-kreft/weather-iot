using System;
using System.Collections.Generic;

namespace WeatherData.Tests.Unit
{
    public record RawSeries
    {
        public List<string> Data { get; }
        public DateTime Start { get; }
        public DateTime End { get; }

        public RawSeries(List<string> series, DateTime start, DateTime end)
        {
            Data = series;
            Start = start;
            End = end;
        }
    }
}