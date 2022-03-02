using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;

namespace WeatherData.Domain
{
    public interface IMeasurementImporter
    {
        Task<Result> ImportMeasurementsAsync();
    }
}