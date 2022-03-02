using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WeatherData.Contract.v1;
using WeatherData.Model;
using Xunit;

namespace WeatherData.Tests.Integration
{
    public class MeasurementsControllerGetDeviceSensorMeasurementsTests : HostTest
    {
        private async Task<HttpResponseMessage> GetResponse(string device, string sensor, DateTime date)
        {
            var route = ApiRoutes.WeatherForecast.GetDeviceSensorMeasurements
                .Replace("{deviceId}", device)
                .Replace("{sampleDate}", date.ToString("yyyy-MM-dd"))
                .Replace("{sensor}", sensor);
            var response = await Client.GetAsync(route);
            return response;
        }
        private async Task<HttpResponseMessage> GetResponse(string device, string sensor, string date)
        {
            var route = ApiRoutes.WeatherForecast.GetDeviceSensorMeasurements
                .Replace("{deviceId}", device)
                .Replace("{sampleDate}", date)
                .Replace("{sensor}", sensor);
            var response = await Client.GetAsync(route);
            return response;
        }

        [Fact]
        public async Task should_return_measurements_for_valid_device_and_sensor_if_exists()
        {
            var device = "dockan";
            var sensor = "humidity";
            var date = new DateTime(2019, 1, 1);
            var response = await GetResponse(device, sensor, date);

            var body = await response.Content.ReadFromJsonAsync<SensorData>();
            Assert.NotNull(body);

            var expected = ContextInitializer.Measurements.FirstOrDefault(it => it.Device == device && it.Date == date && it.Sensor == sensor);
            Assert.NotNull(expected);

            Assert.Equal(expected.Device, body.Device);
            Assert.Equal(expected.Sensor, body.Sensor);
            Assert.Equal(expected.Date, body.Date);
            for (int i = 0; i < expected.Values.Count; ++i)
            {
                Assert.Equal(expected.Values[i], body.Values[i]);
            }
        }

        [Fact]
        public async Task should_return_empty_list_for_valid_device_and_sensor_if_not_exists()
        {
            var device = "dockan";
            var sensor = "humidity";
            var date = new DateTime(2018, 1, 1);

            var response = await GetResponse(device, sensor, date);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task should_return_not_found_if_sensor_not_exists()
        {
            var device = "dockan";
            var sensor = "non-existing";
            var date = new DateTime(2018, 1, 1);

            var response = await GetResponse(device, sensor, date);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task should_return_not_found_if_device_not_exists()
        {
            var device = "non-existing";
            var sensor = "humidity";
            var date = new DateTime(2018, 1, 1);

            var response = await GetResponse(device, sensor, date);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task should_return_not_found_if_device_is_new()
        {
            var device = "gdansk";
            var sensor = "humidity";
            var date = new DateTime(2018, 1, 1);

            var response = await GetResponse(device, sensor, date);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("2019-02-30")]
        [InlineData("2019-13-01")]
        [InlineData("2019-20-1564")]
        public async Task should_return_not_found_if_date_value_is_out_of_range(string date)
        {
            var device = "dockan";
            var sensor = "humidity";
            var response = await GetResponse(device, sensor, date);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("2019/02/02")]
        [InlineData("2019")]
        [InlineData("20190202")]
        [InlineData("abc")]
        [InlineData("02-02-2019")]
        public async Task should_return_not_found_if_date_is_malformed(string date)
        {
            var device = "dockan";
            var sensor = "humidity";
            var response = await GetResponse(device, sensor, date);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}