using System;

namespace Http.Server
{
    public class WeatherForecast
    {
        private int _temperatureF;
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF
        {
            get => (int)(32 + (TemperatureC / 0.5556));
            set => _temperatureF = value;
        }

        public string Summary { get; set; }


    }
}
