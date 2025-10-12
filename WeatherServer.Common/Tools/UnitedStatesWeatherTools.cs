using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using WeatherServer.Common;

namespace WeatherServer.Tools;

/// <summary>
/// MCP Server Tool that uses National Weather Service API https://api.weather.gov/ to provide weather information.
/// </summary>
[McpServerToolType]
public sealed class UnitedStatesWeatherTools
{

    public string ToolId => "Us weather tool";

    [McpServerTool(Name = "UsWeatherAlerts"), Description("Get weather alerts for a US state.")]
    public static async Task<string> GetAlerts(
        IHttpClientFactory clientFactory,
        [Description("The US state to get alerts for. Use the 2 letter abbreviation for the state (e.g. NY).")] string state)
    {
        var client = clientFactory.CreateClient(WeatherServerApiClientNames.WeatherGovApiClientName);
        using var jsonDocument = await client.ReadJsonDocumentAsync($"/alerts/active/area/{state}");
        var jsonElement = jsonDocument.RootElement;
        var features = jsonElement.GetProperty("features").EnumerateArray();

        if (!features.Any())
        {
            return "No active alerts for this state.";
        }

        return string.Join("\n--\n", features.Select(alert =>
        {
            JsonElement properties = alert.GetProperty("properties");
            return $"""
                    Headline: {properties.GetProperty("headline").GetString()}
                    Event: {properties.GetProperty("event").GetString()}
                    Area: {properties.GetProperty("areaDesc").GetString()}
                    Severity: {properties.GetProperty("severity").GetString()}
                    Description: {properties.GetProperty("description").GetString()}
                    Instruction: {properties.GetProperty("instruction").GetString()}
                    Certainty: {properties.GetProperty("certainty").GetString()}
                    """;
        }));
    }

    [McpServerTool(Name = "UsWeatherForecastLocation"), Description("Get weather forecast for a location.")]
    public static async Task<string> GetForecast(
        IHttpClientFactory clientFactory,
        [Description("Latitude of the location.")] double latitude,
        [Description("Longitude of the location.")] double longitude)
    {
        var client = clientFactory.CreateClient(WeatherServerApiClientNames.WeatherGovApiClientName);
        var pointUrl = string.Create(CultureInfo.InvariantCulture, $"/points/{latitude},{longitude}");
        using var locationDocument = await client.ReadJsonDocumentAsync(pointUrl);
        var forecastUrl = locationDocument.RootElement.GetProperty("properties").GetProperty("forecast").GetString()
            ?? throw new McpException($"No forecast URL provided by {client.BaseAddress}points/{latitude},{longitude}");

        using var forecastDocument = await client.ReadJsonDocumentAsync(forecastUrl);
        var periods = forecastDocument.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray();

        return string.Join("\n---\n", periods.Select(period => $"""
                {period.GetProperty("name").GetString()}
                Temperature: {period.GetProperty("temperature").GetInt32()}°F
                Wind: {period.GetProperty("windSpeed").GetString()} {period.GetProperty("windDirection").GetString()}
                Forecast: {period.GetProperty("detailedForecast").GetString()}
                """));
    }
}