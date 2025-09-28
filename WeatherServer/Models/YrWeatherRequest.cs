namespace WeatherServer.Models
{

    public class YrWeatherRequest
    {
        public string Location { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

}
