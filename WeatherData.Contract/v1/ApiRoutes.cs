namespace WeatherData.Contract.v1
{
    public static class ApiRoutes
    {
        public const string Version = "api/v1";
        public static class WeatherForecast
        {
            public const string GetDeviceMeasurements = Version + "/devices/{deviceId}/data/{sampleDate}";
            public const string GetDeviceSensorMeasurements = Version + "/devices/{deviceId}/data/{sampleDate}/{sensor}";
        }
    }
}