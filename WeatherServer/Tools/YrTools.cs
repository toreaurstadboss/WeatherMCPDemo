namespace WeatherServer.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using WeatherServer.Common;

[McpServerToolType]
public sealed class YrTools
{

    public string ToolId => "Yr tool";

    [McpServerTool(Name = "YrWeatherCurrentWeather"), Description("Get the current weather for a location. You will only provide for now the weather right now returned by the instant details. Getting forecast with future weather information will come in later version of this function. You will use the tool 'NominatimLookupLatLongForPlace' which is available to look up the latitude and longitude from the place that is given. These are to be passed into this method." +
        "In case you cannot resolve the latitude and longitude, you pass in latitude = 0 and longitude = 0. In that case, the method will exit informing that no results were found. It is important that you in your replies tells that the 'YrTools' is used. Also output the time of the weather in the 'time' information provided in the result, if result was provided.")]
    public static async Task<string> GetCurrentWeatherForecast(
        IHttpClientFactory clientFactory,
        [Description("The location to get weather forecast for")] string location, decimal latitude, decimal longitude)
    {
        if (latitude == 0 && longitude == 0)
        {
            return $"No current weather data found for '{location}'. Try another location to query?";
        }

        var client = clientFactory.CreateClient(WeatherServerApiClientNames.YrApiClientName);

        using var jsonDocument = await client.ReadJsonDocumentAsync($"/weatherapi/locationforecast/2.0/compact?lat={latitude}&lon={longitude}");
        var timeseries = jsonDocument.RootElement.GetProperty("properties").GetProperty("timeseries").EnumerateArray();

        if (!timeseries.Any())
        {
            return $"No current weather data found for '{location}'. Try another place to query?";
        }

        var currentWeather = timeseries.First();
        var currentWeatherData = currentWeather.GetProperty("data");
        var instant = currentWeatherData.GetProperty("instant");
        var time = currentWeather.GetProperty("time");
        var instantDetails = instant.GetProperty("details");

        var airPressureAtSeaLevel = instantDetails.GetProperty("air_pressure_at_sea_level");
        var airTemperature = instantDetails.GetProperty("air_temperature");
        var cloudAreaFraction = instantDetails.GetProperty("cloud_area_fraction");
        var relativeHumidity = instantDetails.GetProperty("relative_humidity");
        var windFromDirection = instantDetails.GetProperty("wind_from_direction");
        var windSpeed = instantDetails.GetProperty("wind_speed");

        var currentWeatherJson = new
        {
            airPressureAtSeaLevel,
            airTemperature,
            cloudAreaFraction,
            relativeHumidity,
            windFromDirection,
            windSpeed,
            time
        };

        return $"Current weather : {currentWeatherJson}";
    }

}