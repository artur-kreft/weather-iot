using AutoMapper;
using Newtonsoft.Json;
using WeatherData.DataAccess.Db;
using WeatherData.DataAccess.Repository;

namespace WeatherData.DataAccess.Mappers
{
    public class EntityToDomainProfile : Profile
    {
        public EntityToDomainProfile()
        {
            CreateMap<DeviceEntity, Device>();
            CreateMap<MeasurementEntity, Measurement>();
        }
    }
}