using System.Text.Json.Serialization;

namespace WeatherServer.Models
{

    public class YrWeatherInfoItem
    {

        [JsonPropertyName("time")]
        public DateTime? Time { get; set; }

        [JsonPropertyName("data.instant.details.air_pressure_at_sea_level")]
        public double? AirPressureAtSeaLevel { get; set; }

        [JsonPropertyName("data.instant.details.air_temperature")]
        public double? AirTemperature { get; set; }

        [JsonPropertyName("data.instant.details.cloud_area_fraction")]
        public double? CloudAreaFraction { get; set; }

        [JsonPropertyName("data.instant.details.relative_humidity")]
        public double? RelativeHumidity { get; set; }

        [JsonPropertyName("data.instant.details.wind_from_direction")]
        public double? WindFromDirection { get; set; }

        [JsonPropertyName("data.instant.details.wind_speed")]
        public double? WindSpeed { get; set; }

        [JsonPropertyName("data.next_1_hours.summary.symbol_code")]
        public string? NextHourWeatherSymbol { get; set; }

        [JsonPropertyName("data.next_1_hours.summary.precipitation_amount")]
        public double? NextHourPrecipitationAmount { get; set; }

        [JsonPropertyName("data.next_6_hours.summary.symbol_code")]
        public string? NextSixHoursWeatherSymbol { get; set; }

        [JsonPropertyName("data.next6_hours.summary.precipitation_amount")]
        public double? NextSixHoursPrecipitationAmount { get; set; }

        [JsonPropertyName("data.next_12_hours.summary.symbol_code")]
        public string? NextTwelveHoursWeatherSymbol { get; set; }

        //[JsonPropertyName("data.next12_hours.summary.precipitation_amount")]
        //public double? NextTwelveHoursPrecipitationAmount { get; set; }

        public override string ToString()
        {
            return
$@"""
Time = {Time},
AirpressureAtSeaLevel = {AirPressureAtSeaLevel},
AirTemperature = {AirTemperature},
CloudAreaFraction = {CloudAreaFraction},
RelativeHumidity = {RelativeHumidity},
WindFromDirection = {WindFromDirection},
WindSpeed = {WindSpeed}
NextHourWeatherSymbol = {NextHourWeatherSymbol}
NextHourPrecipitationAmount = {NextHourPrecipitationAmount}
NextSixHoursWeatherSymbol = {NextSixHoursWeatherSymbol}
NextSixHoursPrecipitationAmount = {NextSixHoursPrecipitationAmount}
NextTwelveHoursWeatherSymbol = {NextTwelveHoursWeatherSymbol}
""";
        } //tostring override

    }

}
