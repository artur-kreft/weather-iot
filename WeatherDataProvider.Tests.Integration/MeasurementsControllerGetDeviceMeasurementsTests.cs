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
    public class MeasurementsControllerGetDeviceMeasurementsTests : HostTest
    {
        private async Task<HttpResponseMessage> GetResponse(string device, DateTime date)
        {
            var route = ApiRoutes.WeatherForecast.GetDeviceMeasurements
                .Replace("{deviceId}", device)
                .Replace("{sampleDate}", date.ToString("yyyy-MM-dd"));
            var response = await Client.GetAsync(route);
            return response;
        }
        private async Task<HttpResponseMessage> GetResponse(string device, string date)
        {
            var route = ApiRoutes.WeatherForecast.GetDeviceMeasurements
                .Replace("{deviceId}", device)
                .Replace("{sampleDate}", date);
            var response = await Client.GetAsync(route);
            return response;
        }

        [Fact]
        public async Task should_return_measurements_for_valid_device_if_exists()
        {
            var device = "dockan";
            var date = new DateTime(2019, 1, 1);
            var response = await GetResponse(device, date);

            var body = await response.Content.ReadFromJsonAsync<List<SensorData>>();
            Assert.NotNull(body);

            var expected = ContextInitializer
                .Measurements
                .Where(it => it.Device == device && it.Date == date)
                .GroupBy(it => it.Sensor)
                .Select(it => it.First())
                .ToList();
            Assert.Equal(body.Count, expected.Count);

            foreach (var m in expected)
            {
                var found = body.Find(it => it.Device == m.Device && it.Sensor == m.Sensor && it.Date == m.Date);
                Assert.NotNull(found);
                for (int i = 0; i < m.Values.Count; ++i)
                {
                    Assert.Equal(m.Values[i], found.Values[i]);
                }
            }
        }

        [Fact]
        public async Task should_return_empty_list_for_valid_device_if_not_exists()
        {
            var device = "dockan";
            var date = new DateTime(2018, 1, 1);

            var response = await GetResponse(device, date);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task should_return_not_found_if_device_not_exists()
        {
            var device = "non-existing-device";
            var date = new DateTime(2018, 1, 1);

            var response = await GetResponse(device, date);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task should_return_not_found_if_device_is_new()
        {
            var device = "gdansk";
            var date = new DateTime(2018, 1, 1);

            var response = await GetResponse(device, date);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("2019-02-30")]
        [InlineData("2019-13-01")]
        [InlineData("2019-20-1564")]
        public async Task should_return_not_found_if_date_value_is_out_of_range(string date)
        {
            var device = "dockan";
            var response = await GetResponse(device, date);
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
            var response = await GetResponse(device, date);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
