using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeatherServer.Common;
using WeatherServer.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<YrTools>()
    .WithTools<UnitedStatesWeatherTools>()
    .WithTools<NominatimTols>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Warning;
});
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddHttpClient(WeatherServerApiClientNames.WeatherGovApiClientName, client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("us-weather-democlient1-tool", "1.0"));
});

builder.Services.AddHttpClient(WeatherServerApiClientNames.YrApiClientName, client =>
{
    client.BaseAddress = new Uri("https://api.met.no");
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("yrweather-mcpdemoclient-tore-tool", "1.0"));
    //client.DefaultRequestHeaders.UserAgent.ParseAdd("ToresMcpDemo/1.0 (+https://github.com/toreaurstadboss)");
});

builder.Services.AddHttpClient(WeatherServerApiClientNames.OpenStreetmapApiClientName, client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("nominatim-openstreetmap-api-tool", "1.0"));
});

var app = builder.Build();
await app.RunAsync();
