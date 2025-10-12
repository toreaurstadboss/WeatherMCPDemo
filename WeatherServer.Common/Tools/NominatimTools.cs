namespace WeatherServer.Tools;

using System.ComponentModel;
using ModelContextProtocol.Server;
using WeatherServer.Common;

[McpServerToolType]
public sealed class NominatimTols
{

    public string ToolId => "OpenStreetMap Nominatim tool";

    [McpServerTool(Name = "NominatimLookupLatLongForPlace"), Description("Get latitude and longitude for a place using Nominatim service of OpenStreetMap.")]
    public static async Task<string> GetLatitudeAndLongitude(
        IHttpClientFactory clientFactory,
        [Description("The place to get latitude and longitude for. Will use Nominatim service of OpenStreetMap")] string place)
    {
        var client = clientFactory.CreateClient(WeatherServerApiClientNames.OpenStreetmapApiClientName);

        using var jsonDocument = await client.ReadJsonDocumentAsync($"/search?q={Uri.EscapeDataString(place)}&format=geojson&limit=1");
        var features = jsonDocument.RootElement.GetProperty("features").EnumerateArray();

        if (!features.Any())
        {
            return $"No location data found for '{place}'. Try another place to query?";
        }

        var feature = features.First();

        var geometry = feature.GetProperty("geometry");

        var geometryType = geometry.GetProperty("type").GetString();

        if (string.Equals(geometryType, "point", StringComparison.OrdinalIgnoreCase))
        {
            var pointCoordinates = geometry.GetProperty("coordinates").EnumerateArray();
            if (pointCoordinates.Any())
            {
                return $"Latitude: {pointCoordinates.ElementAt(0)}, Longitude: {pointCoordinates.ElementAt(1)}";
            }
        }

        return $"No location data found for '{place}'. Try another place to query?";
    }

}