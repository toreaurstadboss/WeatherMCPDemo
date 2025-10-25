using System.Text.Json;

namespace WeatherServer.Tools;

public static class HttpClientExtensions
{

    /// <summary>
    /// Sends a GET request to the specified <paramref name="requestUri"/> and parses the response content as a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance used to send the request.</param>
    /// <param name="requestUri">The uri (string) to which the GET request is sent.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the parsed <see cref="JsonDocument"/> from the response content.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP response indicates an unsuccessful status code.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the response content cannot be parsed as JSON.
    /// </exception>
    public static async Task<JsonDocument> ReadJsonDocumentAsync(this HttpClient client, string requestUri)
    {
        using var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }

}