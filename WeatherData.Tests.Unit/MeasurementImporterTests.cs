using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherData.BlobAccess;
using WeatherData.DataAccess.Db;
using WeatherData.DataAccess.Repository;
using WeatherData.Domain;
using WeatherData.Model;
using Xunit;

namespace WeatherData.Tests.Unit
{
    public class MeasurementImporterTests : MeasurementImporterMock
    {
        [Fact]
        public async Task should_import_if_format_is_correct_and_files_are_contiguous()
        {
            List<string> meta = new List<string>()
            {
                "dockan;humidity"
            };

            List<string> historySeries = new List<string>()
            {
                "01:02:03;1,87",
                "01:02:04;2,07",
                "01:02:06;232,007",
                "02:01:59;,004",
                "03:51:51;,0004",
                "03:51:52;,1264",
            };

            List<string> lastSeries = new List<string>()
            {
                "04:01:01;0,057",
                "05:01:04;12,17",
                "05:01:06;22,001",
                "05:02:59;12,004"
            };

            var histStart = DateTime.Today.AddDays(-10);
            var histEnd = DateTime.Today.AddDays(-5);
            var lastStart = DateTime.Today.AddDays(-4);
            var lastEnd = DateTime.Today.AddDays(-1);

            var history = new RawSeries(historySeries, histStart, histEnd);
            var lastFiles = new RawSeries(lastSeries, lastStart, lastEnd);
            var context = new ImporterContext(meta, history, lastFiles);

            await Setup(context);

            var savedDevice = new Device()
            {
                Id = $"dockan/humidity",
                Status = DeviceStatusEnum.New
            };
            DeviceRepository
                .Setup(it => it.GetOrAddAsync("dockan", "humidity"))
                .Returns(Task.FromResult(Result.Ok(savedDevice)));

            
            var addSensorDataCursor = histStart;
            MeasurementRepository
                .Setup(x => x.AddAsync(It.IsAny<List<SensorData>>()))
                .Callback<List<SensorData>>((data) =>
                {
                    foreach (var d in data)
                    {
                        Assert.Equal("dockan", d.Device);
                        Assert.Equal("humidity", d.Sensor);
                        Assert.Equal(addSensorDataCursor, d.Date);

                        var series = addSensorDataCursor < lastStart ? historySeries : lastSeries;
                        for (int i = 0; i < series.Count; ++i)
                        {
                            Assert.Equal(series[i], string.Join(";", d.Values[i]));
                        }

                        addSensorDataCursor = addSensorDataCursor.AddDays(1);
                    }
                });

            int deviceUpdated = 0;
            DeviceRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Device>()))
                .Callback<Device>((it) =>
                {
                    if (deviceUpdated == 0)
                    {
                        Assert.Equal("dockan/humidity", it.Id);
                        Assert.Equal(histEnd, it.LastUpdated);
                        Assert.Equal(DateTime.Today.Date, it.LastTried.Date);
                        Assert.Equal(DeviceStatusEnum.Initializing, it.Status);
                        Assert.Equal(0, it.UpdateRetries);
                    }
                    else if (deviceUpdated == 1)
                    {
                        Assert.Equal("dockan/humidity", it.Id);
                        Assert.Equal(histEnd, it.LastUpdated);
                        Assert.Equal(DateTime.Today.Date, it.LastTried.Date);
                        Assert.Equal(DeviceStatusEnum.Enabled, it.Status);
                        Assert.Equal(0, it.UpdateRetries);
                    }
                    else if (deviceUpdated == 2)
                    {
                        Assert.Equal("dockan/humidity", it.Id);
                        Assert.Equal(lastEnd, it.LastUpdated);
                        Assert.Equal(DateTime.Today.Date, it.LastTried.Date);
                        Assert.Equal(DeviceStatusEnum.Enabled, it.Status);
                        Assert.Equal(0, it.UpdateRetries);
                    }

                    deviceUpdated++;
                });

            var storage = new MeasurementStorage(LoggerStorage.Object, Config.Object, Client.Object);
            var importer = new MeasurementImporter(storage, MeasurementRepository.Object, DeviceRepository.Object, LoggerImporter.Object, Config.Object);
            
            var result = await importer.ImportMeasurementsAsync();
            Assert.True(result.IsSuccess);

            MeasurementRepository.Verify(x => x.AddAsync(It.IsAny<List<SensorData>>()), Times.Exactly(2));
            DeviceRepository.Verify(x => x.UpdateAsync(It.IsAny<Device>()), Times.Exactly(3));
        }

        [Fact]
        public async Task should_ignore_history_records_and_display_warning_if_line_is_malformed()
        {
            List<string> meta = new List<string>()
            {
                "dockan;humidity"
            };

            List<string> historySeries = new List<string>()
            {
                "01:02:03;1.87",
                "01-02:04;2,07",
                "01:04:03;0,99",
                "abc;232,007",
                "02:01:59;,004",
                "03:51:51,0004",
                "03:51:51,0.0004",
                "01:02:03;a",
                "03:51:52;,1264;,768",
                "03:52:55;,1264;08:01:01",
            };

            List<string> expectedHistorySeries = new List<string>()
            {
                "01:04:03;0,99",
                "02:01:59;,004",
                "03:51:52;,1264",
                "03:52:55;,1264"
            };
            

            List<string> lastSeries = new List<string>();

            var histStart = DateTime.Today.AddDays(-10);
            var histEnd = DateTime.Today.AddDays(-1);
            var histDiffDays = (histEnd - histStart).Days;
            var lastStart = DateTime.Today;
            var lastEnd = DateTime.Today;

            var history = new RawSeries(historySeries, histStart, histEnd);
            var lastFiles = new RawSeries(lastSeries, lastStart, lastEnd);
            var context = new ImporterContext(meta, history, lastFiles);

            await Setup(context);

            var savedDevice = new Device()
            {
                Id = $"dockan/humidity",
                Status = DeviceStatusEnum.New
            };
            DeviceRepository
                .Setup(it => it.GetOrAddAsync("dockan", "humidity"))
                .Returns(Task.FromResult(Result.Ok(savedDevice)));


            var addSensorDataCursor = histStart;
            MeasurementRepository
                .Setup(x => x.AddAsync(It.IsAny<List<SensorData>>()))
                .Callback<List<SensorData>>((data) =>
                {
                    foreach (var d in data)
                    {
                        Assert.Equal("dockan", d.Device);
                        Assert.Equal("humidity", d.Sensor);
                        Assert.Equal(addSensorDataCursor, d.Date);
                        Assert.Equal(expectedHistorySeries.Count, d.Values.Count);
                        
                        for (int i = 0; i < expectedHistorySeries.Count; ++i)
                        {
                            Assert.Equal(expectedHistorySeries[i], string.Join(";", d.Values[i]));
                        }

                        addSensorDataCursor = addSensorDataCursor.AddDays(1);
                    }
                });

            var storage = new MeasurementStorage(LoggerStorage.Object, Config.Object, Client.Object);
            var importer = new MeasurementImporter(storage, MeasurementRepository.Object, DeviceRepository.Object, LoggerImporter.Object, Config.Object);
            
            var result = await importer.ImportMeasurementsAsync();
            Assert.True(result.IsSuccess);

            var expectedTimestampWarnings = 4 * (1 + histDiffDays);
            var expectedDecimalWarnings = 2 * (1 + histDiffDays);
            LoggerStorage.VerifyLogging("Failed to parse timestamp", LogLevel.Warning, Times.Exactly(expectedTimestampWarnings));
            LoggerStorage.VerifyLogging("Failed to parse value", LogLevel.Warning, Times.Exactly(expectedDecimalWarnings));
            MeasurementRepository.Verify(x => x.AddAsync(It.IsAny<List<SensorData>>()), Times.Exactly(1));
            DeviceRepository.Verify(x => x.UpdateAsync(It.IsAny<Device>()), Times.Exactly(2));
        }

        [Fact]
        public async Task should_return_error_if_history_cannot_be_imported()
        {
            List<string> meta = new List<string>()
            {
                "dockan;humidity"
            };

            List<string> historySeries = new List<string>();
            List<string> lastSeries = new List<string>();

            var histStart = DateTime.Today;
            var histEnd = DateTime.Today.AddDays(-1); // disable history
            var lastStart = DateTime.Today.AddDays(-7);
            var lastEnd = DateTime.Today.AddDays(-1);

            var history = new RawSeries(historySeries, histStart, histEnd);
            var lastFiles = new RawSeries(lastSeries, lastStart, lastEnd);
            var context = new ImporterContext(meta, history, lastFiles);

            await Setup(context);

            var savedDevice = new Device()
            {
                Id = $"dockan/humidity",
                Status = DeviceStatusEnum.New
            };
            DeviceRepository
                .Setup(it => it.GetOrAddAsync("dockan", "humidity"))
                .Returns(Task.FromResult(Result.Ok(savedDevice)));
            
            var storage = new MeasurementStorage(LoggerStorage.Object, Config.Object, Client.Object);
            var importer = new MeasurementImporter(storage, MeasurementRepository.Object, DeviceRepository.Object, LoggerImporter.Object, Config.Object);
            var result = await importer.ImportMeasurementsAsync();
            
            LoggerImporter.VerifyLogging("Missing history file", LogLevel.Error, Times.Once());
            Assert.True(result.IsFailed);
            Assert.True(result.Errors[0].ToString().Contains("Missing history file"));
        }

        [Fact]
        public async Task should_ignore_last_file_records_and_display_warning_if_line_is_malformed()
        {
            List<string> meta = new List<string>()
            {
                "dockan;humidity"
            };

            List<string> historySeries = new List<string>();

            List<string> lastSeries = new List<string>()
            {
                "01:02:03;1.87",
                "01-02:04;2,07",
                "01:04:03;0,99",
                "abc;232,007",
                "02:01:59;,004",
                "03:51:51,0004",
                "03:51:51,0.0004",
                "01:02:03;a",
                "03:51:52;,1264;,768",
                "03:52:55;,1264;08:01:01",
            };

            List<string> expectedLastSeries = new List<string>()
            {
                "01:04:03;0,99",
                "02:01:59;,004",
                "03:51:52;,1264",
                "03:52:55;,1264"
            };

            var histStart = DateTime.Today;
            var histEnd = DateTime.Today.AddDays(-1); // disable history
            var lastStart = DateTime.Today.AddDays(-7);
            var lastEnd = DateTime.Today.AddDays(-1);
            var lastDiffDays = (lastEnd - lastStart).Days;

            var history = new RawSeries(historySeries, histStart, histEnd);
            var lastFiles = new RawSeries(lastSeries, lastStart, lastEnd);
            var context = new ImporterContext(meta, history, lastFiles);

            await Setup(context);

            var savedDevice = new Device()
            {
                Id = $"dockan/humidity",
                Status = DeviceStatusEnum.Enabled,
                LastUpdated = lastStart.AddDays(-1)
            };
            DeviceRepository
                .Setup(it => it.GetOrAddAsync("dockan", "humidity"))
                .Returns(Task.FromResult(Result.Ok(savedDevice)));


            var addSensorDataCursor = lastStart;
            MeasurementRepository
                .Setup(x => x.AddAsync(It.IsAny<List<SensorData>>()))
                .Callback<List<SensorData>>((data) =>
                {
                    foreach (var d in data)
                    {
                        Assert.Equal("dockan", d.Device);
                        Assert.Equal("humidity", d.Sensor);
                        Assert.Equal(addSensorDataCursor, d.Date);
                        Assert.Equal(expectedLastSeries.Count, d.Values.Count);

                        for (int i = 0; i < expectedLastSeries.Count; ++i)
                        {
                            Assert.Equal(expectedLastSeries[i], string.Join(";", d.Values[i]));
                        }

                        addSensorDataCursor = addSensorDataCursor.AddDays(1);
                    }
                });

            var storage = new MeasurementStorage(LoggerStorage.Object, Config.Object, Client.Object);
            var importer = new MeasurementImporter(storage, MeasurementRepository.Object, DeviceRepository.Object, LoggerImporter.Object, Config.Object);
            
            var result = await importer.ImportMeasurementsAsync();
            Assert.True(result.IsSuccess);

            var expectedTimestampWarnings = 4 * (1 + lastDiffDays);
            var expectedDecimalWarnings = 2 * (1 + lastDiffDays);
            LoggerStorage.VerifyLogging("Failed to parse timestamp", LogLevel.Warning, Times.Exactly(expectedTimestampWarnings));
            LoggerStorage.VerifyLogging("Failed to parse value", LogLevel.Warning, Times.Exactly(expectedDecimalWarnings));
            MeasurementRepository.Verify(x => x.AddAsync(It.IsAny<List<SensorData>>()), Times.Exactly(1));
            DeviceRepository.Verify(x => x.UpdateAsync(It.IsAny<Device>()), Times.Exactly(1));
        }

        [Fact]
        public async Task should_save_imported_history_paged()
        {
            List<string> meta = new List<string>()
            {
                "dockan;humidity"
            };

            List<string> historySeries = new List<string>()
            {
                "01:02:03;1,87",
            };

            List<string> lastSeries = new List<string>();

            var histStart = DateTime.Today.AddDays(-60);
            var histEnd = DateTime.Today.AddDays(-1);
            var histDiff = (histEnd - histStart).Days;
            var pageSize = 10;
            var pages = histDiff / pageSize + 1;
            var lastStart = DateTime.Today;
            var lastEnd = DateTime.Today;

            var history = new RawSeries(historySeries, histStart, histEnd);
            var lastFiles = new RawSeries(lastSeries, lastStart, lastEnd);
            var context = new ImporterContext(meta, history, lastFiles, pageSize);

            await Setup(context);

            var savedDevice = new Device()
            {
                Id = $"dockan/humidity",
                Status = DeviceStatusEnum.New
            };
            DeviceRepository
                .Setup(it => it.GetOrAddAsync("dockan", "humidity"))
                .Returns(Task.FromResult(Result.Ok(savedDevice)));

            var storage = new MeasurementStorage(LoggerStorage.Object, Config.Object, Client.Object);
            var importer = new MeasurementImporter(storage, MeasurementRepository.Object, DeviceRepository.Object, LoggerImporter.Object, Config.Object);
            
            var result = await importer.ImportMeasurementsAsync();
            Assert.True(result.IsSuccess);

            MeasurementRepository.Verify(x => x.AddAsync(It.IsAny<List<SensorData>>()), Times.Exactly(pages));
            DeviceRepository.Verify(x => x.UpdateAsync(It.IsAny<Device>()), Times.Exactly(pages + 1));
        }

        [Fact]
        public async Task should_return_error_if_missing_files_from_last_updated_until_yesterday()
        {
            List<string> meta = new List<string>()
            {
                "dockan;humidity"
            };

            List<string> historySeries = new List<string>();

            List<string> lastSeries = new List<string>()
            {
                "01:04:03;0,99"
            };

            var histStart = DateTime.Today;
            var histEnd = DateTime.Today.AddDays(-1); // disable history
            var lastStart = DateTime.Today.AddDays(-10);
            var lastEnd = DateTime.Today.AddDays(-5);
            var lastDiffDays = (DateTime.Today.AddDays(-1) - lastEnd).Days;
            var validDays = 2;

            var history = new RawSeries(historySeries, histStart, histEnd);
            var lastFiles = new RawSeries(lastSeries, lastStart, lastEnd);
            var context = new ImporterContext(meta, history, lastFiles);

            await Setup(context);

            var savedDevice = new Device()
            {
                Id = $"dockan/humidity",
                Status = DeviceStatusEnum.Enabled,
                LastUpdated = lastEnd.AddDays(-1 * validDays)
            };
            DeviceRepository
                .Setup(it => it.GetOrAddAsync("dockan", "humidity"))
                .Returns(Task.FromResult(Result.Ok(savedDevice)));

            MeasurementRepository
                .Setup(x => x.AddAsync(It.IsAny<List<SensorData>>()))
                .Callback<List<SensorData>>((data) =>
                {
                    foreach (var d in data)
                    {
                        Assert.True(d.Date <= lastEnd);
                    }
                });

            var storage = new MeasurementStorage(LoggerStorage.Object, Config.Object, Client.Object);
            var importer = new MeasurementImporter(storage, MeasurementRepository.Object, DeviceRepository.Object, LoggerImporter.Object, Config.Object);

            var result = await importer.ImportMeasurementsAsync();
            Assert.True(result.IsSuccess);
            LoggerStorage.VerifyLogging("Failed to retrieve", LogLevel.Error, Times.Exactly(lastDiffDays));
            MeasurementRepository.Verify(x => x.AddAsync(It.IsAny<List<SensorData>>()), Times.Exactly(1));
            DeviceRepository.Verify(x => x.UpdateAsync(It.IsAny<Device>()), Times.Exactly(1));
        }

        [Fact]
        public async Task should_disable_device_if_exceed_retries_limit()
        {
            List<string> meta = new List<string>()
            {
                "dockan;humidity"
            };

            List<string> historySeries = new List<string>();
            List<string> lastSeries = new List<string>();

            var histStart = DateTime.Today;
            var histEnd = DateTime.Today.AddDays(-1); // disable history
            var lastStart = DateTime.Today;
            var lastEnd = DateTime.Today.AddDays(-1); // disable last files
            var retriesLimit = 5;
            var lastUpdated = lastStart.AddDays(-3);
            var lastTried = lastStart.AddDays(-4);

            var history = new RawSeries(historySeries, histStart, histEnd);
            var lastFiles = new RawSeries(lastSeries, lastStart, lastEnd);
            var context = new ImporterContext(meta, history, lastFiles, 100, retriesLimit);

            await Setup(context);

            var savedDevice = new Device()
            {
                Id = $"dockan/humidity",
                Status = DeviceStatusEnum.Enabled,
                LastUpdated = lastUpdated,
                LastTried = lastTried,
                UpdateRetries = retriesLimit
            };
            DeviceRepository
                .Setup(it => it.GetOrAddAsync("dockan", "humidity"))
                .Returns(Task.FromResult(Result.Ok(savedDevice)));

            DeviceRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Device>()))
                .Callback<Device>((it) =>
                {
                    Assert.Equal("dockan/humidity", it.Id);
                    Assert.Equal(lastUpdated.Date, it.LastUpdated.Date);
                    Assert.Equal(lastTried.Date, it.LastTried.Date);
                    Assert.Equal(DeviceStatusEnum.Disabled, it.Status);
                    Assert.Equal(retriesLimit + 1, it.UpdateRetries);
                });

            var storage = new MeasurementStorage(LoggerStorage.Object, Config.Object, Client.Object);
            var importer = new MeasurementImporter(storage, MeasurementRepository.Object, DeviceRepository.Object, LoggerImporter.Object, Config.Object);

            var result = await importer.ImportMeasurementsAsync();
            Assert.True(result.IsSuccess);

            MeasurementRepository.Verify(x => x.AddAsync(It.IsAny<List<SensorData>>()), Times.Never);
            DeviceRepository.Verify(x => x.UpdateAsync(It.IsAny<Device>()), Times.Exactly(1));
        }

        [Fact]
        public async Task should_reenable_device_if_exceed_reenable_days_period()
        {
            List<string> meta = new List<string>()
            {
                "dockan;humidity"
            };

            List<string> historySeries = new List<string>();
            List<string> lastSeries = new List<string>();

            var histStart = DateTime.Today;
            var histEnd = DateTime.Today.AddDays(-1); // disable history
            var lastStart = DateTime.Today;
            var lastEnd = DateTime.Today.AddDays(-1); // disable last files
            var retriesLimit = 5;
            var reenableDaysPeriod = 8;
            var lastUpdated = lastStart.AddDays(-3);
            var lastTried = DateTime.Today.AddDays(-1 * reenableDaysPeriod);

            var history = new RawSeries(historySeries, histStart, histEnd);
            var lastFiles = new RawSeries(lastSeries, lastStart, lastEnd);
            var context = new ImporterContext(meta, history, lastFiles, 100, retriesLimit, reenableDaysPeriod);

            await Setup(context);

            var savedDevice = new Device()
            {
                Id = $"dockan/humidity",
                Status = DeviceStatusEnum.Disabled,
                LastUpdated = lastUpdated,
                LastTried = lastTried,
                UpdateRetries = retriesLimit + 1
            };
            DeviceRepository
                .Setup(it => it.GetOrAddAsync("dockan", "humidity"))
                .Returns(Task.FromResult(Result.Ok(savedDevice)));

            DeviceRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Device>()))
                .Callback<Device>((it) =>
                {
                    Assert.Equal("dockan/humidity", it.Id);
                    Assert.Equal(lastUpdated.Date, it.LastUpdated.Date);
                    Assert.Equal(lastTried.Date, it.LastTried.Date);
                    Assert.Equal(DeviceStatusEnum.Enabled, it.Status);
                    Assert.Equal(0, it.UpdateRetries);
                });

            var storage = new MeasurementStorage(LoggerStorage.Object, Config.Object, Client.Object);
            var importer = new MeasurementImporter(storage, MeasurementRepository.Object, DeviceRepository.Object, LoggerImporter.Object, Config.Object);

            var result = await importer.ImportMeasurementsAsync();
            Assert.True(result.IsSuccess);

            MeasurementRepository.Verify(x => x.AddAsync(It.IsAny<List<SensorData>>()), Times.Never);
            DeviceRepository.Verify(x => x.UpdateAsync(It.IsAny<Device>()), Times.Exactly(1));
        }
    }
}
