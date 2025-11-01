using Anthropic.SDK;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
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

            builder.Services.Configure<McpClientOptions>(options =>
            {
                builder.Configuration.GetSection("McpClient");
            });

            // Setup SSL certificate

            string subjectName = builder.Configuration["McpServer:CertificateSettings:SubjectName"]!;
            string? portnumberMcpRaw = builder.Configuration["McpServer:Portnumber"]!;

            int portnumberMcp = 7145;
            if (int.TryParse(portnumberMcpRaw, out var portnumberMcpFromConfig))
            {
                portnumberMcp = portnumberMcpFromConfig;
            }

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(portnumberMcp, listenOptions =>
                {
                    using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);

                    var certs = store.Certificates.Find(
                        X509FindType.FindBySubjectName,
                        subjectName,
                        validOnly: false);

                    var certificate = certs.FirstOrDefault();
                    if (certificate == null)
                    {
                        throw new InvalidOperationException("Certificate not found.");
                    }

                    listenOptions.UseHttps(certificate);                    
                });
            });


            var mcpEndpoint = builder.Configuration.GetSection("McpClient:Endpoint").Value;

            builder.Services.AddSingleton<IMcpClient>(provider =>
            {
                return McpClientFactory.CreateAsync(
                    new SseClientTransport(
                        new SseClientTransportOptions
                        {
                            Endpoint = new Uri(mcpEndpoint!)
                        }
                    )).GetAwaiter().GetResult();
            });            

            builder.Services.Configure<ModelContextProtocol.Protocol.Implementation>(options =>
            {
                options.Title = "WeatherMcpDemoServer";
                options.Version = "1.1";
            });

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

            // Set up Anthropic client

            builder.Services.AddChatClient(_ =>
                new ChatClientBuilder(new AnthropicClient(new APIAuthentication(builder.Configuration["ANTHROPIC_API_KEY"])).Messages)
                    .UseFunctionInvocation()
                    .Build());

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
