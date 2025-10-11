using System.Net.Http.Headers;
using WeatherServer.Common;
using WeatherServer.Tools;

namespace WeatherServer.Http
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Add MCP support
            builder.Services
                .AddMcpServer()
                .WithHttpTransport()
                .WithTools<YrTools>()
                .WithTools<UnitedStatesWeatherTools>()
                .WithTools<NominatimTols>();

            //Add swagger support
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Warning;
            });
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            // Add named Http clients that fetches more data from external APIs

            builder.Services.AddHttpClient(WeatherServerApiClientNames.WeatherGovApiClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.weather.gov");
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("us-weather-democlient2-tool", "1.0"));
            });

            builder.Services.AddHttpClient(WeatherServerApiClientNames.YrApiClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.met.no");
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("yrweather-mcpdemoclient2-tore-tool", "1.0"));
                //client.DefaultRequestHeaders.UserAgent.ParseAdd("ToresMcpDemo/1.0 (+https://github.com/toreaurstadboss)");
            });

            builder.Services.AddHttpClient(WeatherServerApiClientNames.OpenStreetmapApiClientName, client =>
            {
                client.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("nominatim-openstreetmap-client2-api-tool", "1.0"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapMcp("/sse"); // This exposes the SSE endpoint at /sse

            app.Run();
        }
    }
}
