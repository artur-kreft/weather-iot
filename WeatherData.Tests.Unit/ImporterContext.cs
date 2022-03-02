using System;
using System.Collections.Generic;

namespace WeatherData.Tests.Unit
{
    public record ImporterContext
    {
        public List<string> Meta { get; }
        public RawSeries History { get; }
        public RawSeries LastFiles { get; }
        public int HistoryPageSize { get; }
        public int UpdateRetriesLimit { get; }
        public int TryToReenableDeviceDaysPeriod { get; }

        public ImporterContext(List<string> meta, RawSeries history, RawSeries lastFiles, int pageSize = 100, int updateRetriesLimit = 10, int tryToReenableDeviceDaysPeriod = 10)
        {
            Meta = meta;
            History = history;
            LastFiles = lastFiles;
            HistoryPageSize = pageSize;
            UpdateRetriesLimit = updateRetriesLimit;
            TryToReenableDeviceDaysPeriod = tryToReenableDeviceDaysPeriod;
        }
    }
}