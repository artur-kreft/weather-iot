using Microsoft.EntityFrameworkCore;

namespace WeatherData.DataAccess.Db
{
    public class WeatherDbContext : DbContext
    {
        public DbSet<DeviceEntity> Devices { get; set; }
        public DbSet<MeasurementEntity> Measurements { get; set; }

        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceEntity>()
                .Property(c => c.Status)
                .HasConversion<int>();

            base.OnModelCreating(modelBuilder);

        }
    }
}