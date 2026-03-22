using Anthropic.SDK;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
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

            // Add MCP support
            builder.Services
                .AddMcpServer()
                .WithHttpTransport()
                .WithTools<YrTools>()
                .WithTools<UnitedStatesWeatherTools>()
                .WithTools<NominatimTols>();

            builder.Services.Configure<McpClientOptions>(options =>
            {
                builder.Configuration.GetSection("McpClient");
            });

            var mcpEndpoint = builder.Configuration.GetSection("McpClient:Endpoint").Value;

            builder.Services.AddSingleton<McpClient>(_ =>
            {
                return McpClient.CreateAsync(
                    new HttpClientTransport(new HttpClientTransportOptions
                    {
                        Endpoint = new Uri(mcpEndpoint!),
                        TransportMode = HttpTransportMode.StreamableHttp
                    })
                ).GetAwaiter().GetResult();
            });

            builder.Services.Configure<ModelContextProtocol.Protocol.Implementation>(options =>
            {
                options.Title = "WeatherMcpDemoServer";
                options.Version = "1.2";
            });

            // Add swagger support
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
            });

            builder.Services.AddHttpClient(WeatherServerApiClientNames.OpenStreetmapApiClientName, client =>
            {
                client.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("nominatim-openstreetmap-client2-api-tool", "1.0"));
            });

            // Set up Anthropic client
            builder.Services.AddChatClient(_ =>
                new ChatClientBuilder(new AnthropicClient(new APIAuthentication(builder.Configuration["ANTHROPIC_API_KEY"])).Messages)
                    .UseFunctionInvocation()
                    .Build());

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapMcp("/mcp"); // MCP Streamable HTTP endpoint

            app.Run();
        }
    }
}
