using System;

namespace WeatherData.Model
{
    public class Series
    {
        public Series(DateTime date, string values)
        {
            Date = date;
            Values = values;
        }

        public DateTime Date { get; }
        public string Values { get; }
    }
}
