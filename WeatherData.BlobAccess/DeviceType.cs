namespace WeatherData.BlobAccess
{
    public class DeviceType
    {
        public DeviceType(string deviceId, string sensorType)
        {
            DeviceId = deviceId;
            SensorType = sensorType;
        }

        public string DeviceId { get; }
        public string SensorType { get; }
    }
}