using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CsvHelper;
using CsvHelper.Configuration;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Configuration;
using WeatherData.DataAccess.Repository;
using WeatherData.Model;

namespace WeatherData.BlobAccess
{
    public class MeasurementStorage : IMeasurementStorage
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _client;
        private readonly CsvConfiguration _csvConfig;

        private static string _containerId;
        private static string _metadataFileName;
        private static string _historyFileName;
        private static string _timestampFormat;
        private static string _csvFilenameFormat;
        private static int _historyPageSize = 100;

        public MeasurementStorage(ILogger<MeasurementStorage> logger, IConfiguration configuration, BlobServiceClient client)
        {
            _logger = logger;
            _client = client;

            _containerId = configuration["BlobStorage:ContainerId"];
            _metadataFileName = configuration["BlobStorage:MetadataFileName"];
            _historyFileName = configuration["BlobStorage:HistoryFileName"];
            _timestampFormat = configuration["BlobStorage:TimestampFormat"];
            _csvFilenameFormat = configuration["BlobStorage:CsvFilenameFormat"];
            _historyPageSize = Int32.Parse(configuration["BlobStorage:HistoryPageSize"]);

            _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = configuration["BlobStorage:CsvDelimiter"],
                DetectDelimiter = false
            };
        }

        public async Task<Result<List<DeviceType>>> GetDevicesAsync()
        {
            var downloadResult = await DownloadFile(_metadataFileName);

            if (downloadResult.IsFailed)
            {
                return Result.Fail(downloadResult.Errors[0]);
            }

            var download = downloadResult.Value.Value.Content;
            
            using var reader = new StreamReader(download);
            using var csv = new CsvReader(reader, _csvConfig);

            List<DeviceType> records = new List<DeviceType>();
            int line = -1;
            while (await csv.ReadAsync())
            {
                line++;

                if (csv.Parser.Record.Length != 2)
                {
                    _logger.LogWarning($"{_metadataFileName} line {line} is malformed");
                    continue;
                }

                records.Add(new DeviceType(csv.Parser.Record[0], csv.Parser.Record[1]));
            }
            
            return Result.Ok(records);
        }

        public async Task<Result> ParseHistoryAsync(string device, string sensor, DateTime? lastUpdated,
            Func<DateTime, List<SensorData>, Task> onFileParsed)
        {
            var filePath = $"{device}/{sensor}/{_historyFileName}";
            var downloadResult = await DownloadFile(filePath);

            if (downloadResult.IsFailed)
            {
                return Result.Fail(downloadResult.Errors[0]);
            }

            var download = downloadResult.Value.Value.Content;
            using ZipArchive archive = new ZipArchive(download, ZipArchiveMode.Read);

            int startFromIndex = 0;
            if (lastUpdated != null)
            {
                var dateString = $"{((DateTime)lastUpdated).ToString(_csvFilenameFormat)}";
                var lastProcessed = archive.Entries.FirstOrDefault(it => it.Name.StartsWith(dateString));
                if (lastProcessed == null)
                {
                    return Result.Fail($"There is no file '{dateString}.csv' in {filePath}");
                }

                startFromIndex = archive.Entries.IndexOf(lastProcessed) + 1;
            }

            List<SensorData> parsed = new List<SensorData>();

            for (int i = startFromIndex; i < archive.Entries.Count; ++i)
            {
                var entry = archive.Entries[i];
                _logger.LogInformation($"Start parsing {filePath}/{entry.Name}");
                
                var records = await ParseCsv(entry.Open(), $"{filePath}/{entry.Name}");

                var filename = Path.GetFileNameWithoutExtension(entry.Name);
                if (false == DateTime.TryParseExact(filename, _csvFilenameFormat, new DateTimeFormatInfo(), DateTimeStyles.None, out DateTime date))
                {
                    _logger.LogWarning($"Failed to parse file name in historical file: {entry.Name}");
                    if (records.Any())
                    {
                        date = DateTime.Parse(records[0][0]);
                    }
                }

                _logger.LogInformation($"Parsed {filePath}/{entry.Name}");
                if (parsed.Any() && (i % _historyPageSize == 0))
                {
                    await onFileParsed(date, new List<SensorData>(parsed));
                    parsed.Clear();
                }

                var sensorData = new SensorData(date, device, sensor, records);
                parsed.Add(sensorData);
            }

            if (parsed.Any())
            {
                await onFileParsed(parsed.Max(it => it.Date), new List<SensorData>(parsed));
                parsed.Clear();
            }
            
            return Result.Ok();
        }

        private async Task<List<string[]>> ParseCsv(Stream fileStream, string filePath)
        {
            CultureInfo enUS = new CultureInfo("en-US");
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, _csvConfig);

            List<string[]> records = new List<string[]>();
            int line = 0;
            while (await csv.ReadAsync())
            {
                var row = csv.Parser.Record;

                if (false == DateTime.TryParseExact(row[0], _timestampFormat, enUS, DateTimeStyles.None, out DateTime dateTime))
                {
                    _logger.LogWarning($"Failed to parse timestamp: {row[0]} in: {filePath} (line: {line})");
                    continue;
                }

                if (false == decimal.TryParse(row[1], out decimal val))
                {
                    _logger.LogWarning($"Failed to parse value: {row[1]} in: {filePath} (line: {line})");
                    continue;
                }
                
                records.Add(new [] { $"{dateTime:HH:mm:ss}", row[1] });
                line++;
            }

            return records;
        }

        public async Task<Result<SensorData>> ParseFileAsync(string device, string sensor, DateTime date)
        {
            var filePath = $"{device}/{sensor}/{date.ToString(_csvFilenameFormat)}.csv";
            var downloadResult = await DownloadFile(filePath);

            if (downloadResult.IsFailed)
            {
                return Result.Fail(downloadResult.Errors[0]);
            }

            var download = downloadResult.Value.Value.Content;
            List<string[]> records = await ParseCsv(download, filePath);

            return Result.Ok(new SensorData(date, device, sensor, records));
        }


        private async Task<Result<Response<BlobDownloadStreamingResult>>> DownloadFile(string filePath)
        {
            Response<BlobDownloadStreamingResult> download;

            try
            {
                var containerClient = _client.GetBlobContainerClient(_containerId);
                var file = containerClient.GetBlobClient(filePath);
                download = await file.DownloadStreamingAsync();
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to retrieve {filePath}", e);
                return Result.Fail("Could not get requested data file from storage");
            }

            return Result.Ok(download);
        }
    }
}
