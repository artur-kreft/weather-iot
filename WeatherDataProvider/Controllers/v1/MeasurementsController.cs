using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WeatherData.Api.Host.Attributes;
using WeatherData.Contract.v1;
using WeatherData.DataAccess.Repository;
using static System.Globalization.DateTimeStyles;

namespace WeatherData.Api.Host.Controllers.v1
{
    [ApiController]
    public class MeasurementsController : ControllerBase
    {
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<MeasurementsController> _logger;

        public MeasurementsController(IMeasurementRepository measurementRepository, IDeviceRepository deviceRepository, ILogger<MeasurementsController> logger)
        {
            _measurementRepository = measurementRepository;
            _deviceRepository = deviceRepository;
            _logger = logger;
        }
        
        [Cached(10)]
        [HttpGet(ApiRoutes.WeatherForecast.GetDeviceMeasurements)]
        public async Task<IActionResult> GetDeviceMeasurements([FromRoute]string deviceId, [FromRoute] string sampleDate)
        {
            var isDeviceValid = await _deviceRepository.IsValid(deviceId);
            if (isDeviceValid.IsFailed)
            {
                return BadRequest(isDeviceValid.Errors);
            }

            if (false == isDeviceValid.Value)
            {
                return NotFound($"Wrong device id: {deviceId}");
            }

            if (false == DateTime.TryParseExact(sampleDate, "yyyy-MM-dd", new DateTimeFormatInfo(), None, out DateTime date))
            {
                return NotFound("Date was not formatted properly. Please use [yyyy-MM-dd] format");
            }

            var data = await _measurementRepository.GetJsonAsync(date, deviceId);
            if (data.IsFailed)
            {
                return BadRequest(data.Errors);
            }

            if (string.IsNullOrEmpty(data.Value))
            {
                return NoContent();
            }

            return Ok(data.Value);
        }

        [Cached(10)]
        [HttpGet(ApiRoutes.WeatherForecast.GetDeviceSensorMeasurements)]
        public async Task<IActionResult> GetDeviceMeasurements([FromRoute]string deviceId, [FromRoute] string sampleDate, [FromRoute] string sensor)
        {
            var isDeviceValid = await _deviceRepository.IsValid(deviceId, sensor);
            if (isDeviceValid.IsFailed)
            {
                return BadRequest(isDeviceValid.Errors);
            }

            if (false == isDeviceValid.Value)
            {
                return NotFound($"Wrong device id: {deviceId} and/or sensor type {sensor}");
            }

            if (false == DateTime.TryParseExact(sampleDate, "yyyy-MM-dd", new DateTimeFormatInfo(), None, out DateTime date))
            {
                return NotFound("Date was not formatted properly. Please use [yyyy-MM-dd] format");
            }

            var data = await _measurementRepository.GetJsonAsync(date, deviceId, sensor);
            if (data.IsFailed)
            {
                return BadRequest(data.Errors);
            }

            if (string.IsNullOrEmpty(data.Value))
            {
                return NoContent();
            }

            return Ok(data.Value);
        }
    }
}
