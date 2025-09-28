namespace WeatherServer.Tools;

using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using WeatherServer.Common;
using WeatherServer.Models;

[McpServerToolType]
public sealed class YrTools
{

    public string ToolId => "Yr tool";

    [McpServerTool(Name = "YrWeatherCurrentWeather")]
    [Description(
 $@"""
     Description of this tool method:
     Retrieves the current weather conditions for a specified location using the YrTools CurrentWeather API.

    Usage Instructions:
    1. Use the 'NominatimLookupLatLongForPlace' tool to resolve the latitude and longitude of the provided location.
    2. Pass the resolved coordinates from the tool above and pass them into to this method.
    3. If coordinates cannot be resolved, use latitude = 0 and longitude = 0. In this case, the method will return a message indicating no results were found.
    4. In case the place passed in is for a place in United States, use instead the tool 'UsWeatherForecastLocation'.
    5. This method is to be used when asked about the current weather right now.

    Response Requirements:
    - Always include the latitude and longitude used.
    - Always inform about which url was used to get the data here.
    - Inform about the time when the weather is.
    - Always include the 'time' field from the result to indicate when the weather data is valid.
    - Clearly state that the data was retrieved using 'YrWeatherCurrentWeather'.
    - Do not modify or reformat the result; return it exactly as received.
    - Do not show the data in Json format, instead sum it up using a dashed list.
    - Inform the time the weather is for
    - Append the raw json data also
    - If the weather time has passed current time by say two days, inform that you could not retrieve weather conditions for now. Look at the 'time' value you got and compare it with today. This is probably due to API usage conditions are limits the service.
""")]
    public static async Task<string> GetCurrentWeatherForecast(
        IHttpClientFactory clientFactory,
        [Description("Provide current weather. State the location, latitude and longitude used. Return precisely the data given. Return ALL the data you were given.")] string location, decimal latitude, decimal longitude)
    {
        if (latitude == 0 && longitude == 0)
        {
            return $"No current weather data found for '{location}'. Try another location to query?";
        }

        var client = clientFactory.CreateClient(WeatherServerApiClientNames.YrApiClientName);

        string url = $"weatherapi/locationforecast/2.0/compact?lat={latitude}&lon={longitude}";
        
        Console.WriteLine($"Accessing Yr Current Weather with url: {url} with client base address {client.BaseAddress}");

        using var jsonDocument = await client.ReadJsonDocumentAsync(url);
        var timeseries = jsonDocument.RootElement.GetProperty("properties").GetProperty("timeseries").EnumerateArray();

        if (!timeseries.Any())
        {
            return $"No current weather data found for '{location}'. Try another place to query?";
        }

        var currentWeatherJson = GetInformationForTimeSeries(timeseries, onlyFirst: true);

        return $"Current weather : {currentWeatherJson}";
    }

    [McpServerTool(Name = "YrWeatherTenDayForecast")]
    [Description(
$@"""
     Description of this tool method:
     Retrieves the ten days forecast weather for a specified location using the YrTools Forecast API.

    Usage Instructions:
    1. Use the 'NominatimLookupLatLongForPlace' tool to resolve the latitude and longitude of the provided location.
    2. Pass the resolved coordinates from the tool above and pass them into to this method.
    3. If coordinates cannot be resolved, use latitude = 0 and longitude = 0. In this case, the method will return a message indicating no results were found.
    4. In case the place passed in is for a place in United States, use instead the tool 'UsWeatherForecastLocation'.
    5. Usually, only ten days forecast will be available, but output all data you get here. In case you are asked to provide even further into the future weather information and
    there are no available data for that, inform about that in the output.
    6. In case asked for a forecast weather, use this method. In case asked about current weather, use instead tool 'YrWeatherCurrentWeather'

    Response Requirements:
    - Always include the latitude and longitude used.
    - Always include the 'time' field from the result to indicate when the weather data is valid.
    - Clearly state that the data was retrieved using 'YrWeatherTenDayForecast'.
    - Any information about the weather must precisely give the scalar values provided. However, you are allowed to do a qualitative summary of the weather in 4-5 sentences first. Also,
      the time series is a bit long for a possible 10 day forecast hour by hour. Therefore sum up the trends such as maximum and minimum temperature and precipitation and wind patterns in the summary.
      Plus also give some precise examples of the weather.
    - Inform about the start and end time of the forecast. In case asked for forecast further into the future and there is no data available, inform that only data is available 
      until the given end time.
    - If the weather time has passed current time by say two days, inform that you could not retrieve weather conditions for now. Look at the 'time' value you got and compare it with today. This is probably due to API usage conditions are limits the service.
""")]
    public static async Task<string> GetTenDaysWeatherForecast(
     IHttpClientFactory clientFactory,
     [Description("Provide ten day forecast weather. State the location, latitude and longitude used. Return the data given. Return ALL the data you were given.")] string location, decimal latitude, decimal longitude)
    {
        if (latitude == 0 && longitude == 0)
        {
            return $"No current weather data found for '{location}'. Try another location to query?";
        }

        var client = clientFactory.CreateClient(WeatherServerApiClientNames.YrApiClientName);

        var url = $"/weatherapi/locationforecast/2.0/compact?lat={latitude}&lon={longitude}";

        Console.WriteLine($"Accessing Yr Current Weather with url: {url} with client base address {client.BaseAddress}");

        using var jsonDocument = await client.ReadJsonDocumentAsync(url);
        var timeseries = jsonDocument.RootElement.GetProperty("properties").GetProperty("timeseries").EnumerateArray();

        if (!timeseries.Any())
        {
            return $"No current weather data found for '{location}'. Try another place to query?";
        }

        var currentWeatherJson = GetInformationForTimeSeries(timeseries, onlyFirst: false);

        return $"Current weather : {currentWeatherJson}";
    }

    private static List<YrWeatherInfoItem> GetInformationForTimeSeries(JsonElement.ArrayEnumerator timeseries, bool onlyFirst)
    {
        var result = new List<YrWeatherInfoItem>();

        foreach (var timeseriesItem in timeseries)
        {
            var currentWeather = timeseriesItem;
            var currentWeatherData = currentWeather.GetProperty("data");
            var instant = currentWeatherData.GetProperty("instant");
            string? nextOneHourWeatherSymbol = null;
            double? nextOneHourPrecipitationAmount = null;
            if (currentWeatherData.TryGetProperty("next_1_hours", out JsonElement nextOneHours))
            {
                nextOneHourWeatherSymbol = nextOneHours.GetProperty("summary").GetProperty("symbol_code").GetString();
                nextOneHourPrecipitationAmount = nextOneHours.GetProperty("details").GetProperty("precipitation_amount").GetDouble();
            }

            string? nextSixHourWeatherSymbol = null;
            double? nextSixHourPrecipitationAmount = null;
            if (currentWeatherData.TryGetProperty("next_6_hours", out JsonElement nextSixHours))
            {
                nextSixHourWeatherSymbol = nextSixHours.GetProperty("summary").GetProperty("symbol_code").GetString();
                nextSixHourPrecipitationAmount = nextSixHours.GetProperty("details").GetProperty("precipitation_amount").GetDouble();
            }

            string? nextTwelveHourWeatherSymbol = null;
            if (currentWeatherData.TryGetProperty("next_12_hours", out JsonElement nextTwelveHours))
            {
                nextTwelveHourWeatherSymbol = nextTwelveHours.GetProperty("summary").GetProperty("symbol_code").GetString();
            }

            string timeRaw = currentWeather.GetProperty("time").GetString()!;
            string format = "yyyy-MM-ddTHH:mm:ssZ";
            DateTime parsedDate = DateTime.Parse(timeRaw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            var instantDetails = instant.GetProperty("details");

            var airPressureAtSeaLevel = instantDetails.GetProperty("air_pressure_at_sea_level");
            var airTemperature = instantDetails.GetProperty("air_temperature");
            var cloudAreaFraction = instantDetails.GetProperty("cloud_area_fraction");
            var relativeHumidity = instantDetails.GetProperty("relative_humidity");
            var windFromDirection = instantDetails.GetProperty("wind_from_direction");
            var windSpeed = instantDetails.GetProperty("wind_speed");

            var weatherItem = new YrWeatherInfoItem
            {
                AirPressureAtSeaLevel = airPressureAtSeaLevel.GetDouble(),
                AirTemperature = airTemperature.GetDouble(),
                CloudAreaFraction = cloudAreaFraction.GetDouble(),
                RelativeHumidity = relativeHumidity.GetDouble(),
                WindFromDirection = windFromDirection.GetDouble(),
                WindSpeed = windSpeed.GetDouble(),
                Time = parsedDate,
                NextHourPrecipitationAmount = nextOneHourPrecipitationAmount,
                NextHourWeatherSymbol = nextOneHourWeatherSymbol,
                NextSixHoursPrecipitationAmount = nextSixHourPrecipitationAmount,
                NextSixHoursWeatherSymbol = nextOneHourWeatherSymbol,
                NextTwelveHoursWeatherSymbol = nextTwelveHourWeatherSymbol
            };

            if (parsedDate.Subtract(DateTime.Today).TotalDays > 2)
            {
                continue; //do not accept forecast that is two days older than today
            }
            result.Add(weatherItem);

            if (onlyFirst)
            {
                break;
            }
        }

        return result;
    }

}