using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherData.BlobAccess;
using WeatherData.DataAccess.Repository;
using WeatherData.Domain;

namespace WeatherData.Tests.Unit
{
    public class MeasurementImporterMock : IDisposable
    {
        private const string MetadataFilename = "metadata.csv";
        private const string HistoryFilename = "historical.zip";

        protected Mock<IMeasurementRepository> MeasurementRepository;
        protected Mock<IDeviceRepository> DeviceRepository;
        protected Mock<ILogger<MeasurementImporter>> LoggerImporter;
        protected Mock<ILogger<MeasurementStorage>> LoggerStorage;

        protected static Mock<IConfiguration> Config;
        protected Mock<BlobServiceClient> Client;
        private MemoryStream _historyStream;
        private List<MemoryStream> _lastFilesStream;

        public async Task Setup(ImporterContext context)
        {
            MeasurementRepository = new Mock<IMeasurementRepository>();
            DeviceRepository = new Mock<IDeviceRepository>();
            LoggerImporter = new Mock<ILogger<MeasurementImporter>>();
            LoggerStorage = new Mock<ILogger<MeasurementStorage>>();
            Client = new Mock<BlobServiceClient>();

            var containerClient = new Mock<BlobContainerClient>();
            var responseMock = new Mock<Response>();

            ConfigSetup(context.HistoryPageSize, context.UpdateRetriesLimit, context.TryToReenableDeviceDaysPeriod);
            SetupMetadataBlobClient(containerClient, context.Meta, responseMock);
            SetupHistoryBlobClient(containerClient, context.History.Data, context.History.Start, context.History.End, responseMock); 
            SetupLastFilesBlobClient(containerClient, context.LastFiles.Data, context.LastFiles.Start, context.LastFiles.End, responseMock);

            Client.Setup(it => it.GetBlobContainerClient(It.IsAny<string>())).Returns(containerClient.Object);
        }

        private void SetupLastFilesBlobClient(Mock<BlobContainerClient> containerClient, List<string> forDateSeries, DateTime startDate, DateTime endDate, Mock<Response> responseMock)
        {
            var cursor = startDate;

            _lastFilesStream = new List<MemoryStream>();
            while (cursor <= endDate)
            {
                var date = $"{cursor:yyyy-MM-dd}";

                var concated = string.Join("\n", forDateSeries.Select(it => $"{date}T{it}"));
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(concated));
                _lastFilesStream.Add(stream);

                var blobClient = new Mock<BlobClient>();
                var response = Task.FromResult(Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(stream),
                    responseMock.Object));

                containerClient.Setup(it => it.GetBlobClient(It.IsRegex($"({date})"))).Returns(blobClient.Object);
                blobClient.Setup(it => it.DownloadStreamingAsync(default, default, default, default)).Returns(response);

                cursor = cursor.AddDays(1);
            }
        }

        private void SetupHistoryBlobClient(Mock<BlobContainerClient> containerClient, List<string> historySeries, DateTime startHistory, DateTime endHistory,
            Mock<Response> responseMock)
        {
            var historicalBlobClient = new Mock<BlobClient>();

            var cursorHistory = startHistory;

            _historyStream = new MemoryStream();
            using (var archive = new ZipArchive(_historyStream, ZipArchiveMode.Create, true))
            {
                while (cursorHistory <= endHistory)
                {
                    var date = $"{cursorHistory:yyyy-MM-dd}";
                    var demoFile = archive.CreateEntry($"{date}.csv");

                    using var entryStream = demoFile.Open();
                    using var streamWriter = new StreamWriter(entryStream);
                    foreach (var s in historySeries)
                    {
                        streamWriter.WriteLine($"{date}T{s}");
                    }

                    cursorHistory = cursorHistory.AddDays(1);
                }
            }

            var responseHistory =
                Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(_historyStream), responseMock.Object);
            var historyResponse = Task.FromResult(responseHistory);

            containerClient.Setup(it => it.GetBlobClient(It.IsRegex($"({HistoryFilename})")))
                .Returns(historicalBlobClient.Object);
            historicalBlobClient.Setup(it => it.DownloadStreamingAsync(default, default, default, default))
                .Returns(historyResponse);
        }

        private static void SetupMetadataBlobClient(Mock<BlobContainerClient> containerClient,
            List<string> meta, Mock<Response> responseMock)
        {
            var concatedMeta = string.Join("\n", meta);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(concatedMeta));
            var response = Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(stream), responseMock.Object);
            var metadataResponse = Task.FromResult(response);
            var metadataBlobClient = new Mock<BlobClient>();
            metadataBlobClient.Setup(it => it.DownloadStreamingAsync(default, default, default, default))
                .Returns(metadataResponse);
            containerClient.Setup(it => it.GetBlobClient(MetadataFilename)).Returns(metadataBlobClient.Object);
        }

        private void ConfigSetup(int pageSize, int updateRetriesDaysLimit, int tryToReenableDeviceDaysPeriod)
        {
            Config = new Mock<IConfiguration>();
            Config.Setup(it => it["BlobStorage:ContainerId"]).Returns("iotcontainer");
            Config.Setup(it => it["BlobStorage:MetadataFileName"]).Returns(MetadataFilename);
            Config.Setup(it => it["BlobStorage:HistoryFileName"]).Returns(HistoryFilename);
            Config.Setup(it => it["BlobStorage:TimestampFormat"]).Returns("yyyy-MM-dd'T'HH:mm:ss");
            Config.Setup(it => it["BlobStorage:CsvFilenameFormat"]).Returns("yyyy-MM-dd");
            Config.Setup(it => it["BlobStorage:HistoryPageSize"]).Returns(pageSize.ToString);
            Config.Setup(it => it["BlobStorage:CsvDelimiter"]).Returns(";");
            Config.Setup(it => it["Importer:UpdateRetriesDaysLimit"]).Returns(updateRetriesDaysLimit.ToString);
            Config.Setup(it => it["Importer:TryToReenableDeviceDaysPeriod"]).Returns(tryToReenableDeviceDaysPeriod.ToString);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _historyStream?.Dispose();

                if (_lastFilesStream == null) return;
                foreach (var stream in _lastFilesStream)
                {
                    stream?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}