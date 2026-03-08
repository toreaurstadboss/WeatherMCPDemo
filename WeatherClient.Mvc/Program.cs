using Anthropic.SDK;
using Microsoft.Extensions.AI;
using System.Security.Cryptography.X509Certificates;

namespace WeatherClient.Mvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add logging configuration
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            //builder.Logging.SetMinimumLevel(LogLevel.Trace);

            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>(); // Get logger

            builder.Configuration
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>();

            // Set up Swagger
            builder.Services.AddSwaggerGen();
            builder.Services.AddEndpointsApiExplorer();

            // Set up Anthropic client
            builder.Services.AddChatClient(_ =>
                new ChatClientBuilder(new AnthropicClient(new APIAuthentication(builder.Configuration["ANTHROPIC_API_KEY"])).Messages)
                    .UseFunctionInvocation()
                    .Build());

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Setup SSL certificate for Kestrel on port 7049
            string subjectName = builder.Configuration["McpServer:CertificateSettings:SubjectName"]!;
            string? portnumberMcpRaw = builder.Configuration["McpServer:Port"];

            int portnumberMcp = 7049; // Default to 7049 if not set
            if (int.TryParse(portnumberMcpRaw, out var portnumberMcpFromConfig))
            {
                portnumberMcp = portnumberMcpFromConfig + 100; //offset the mcp port by 100 so we do not collide (Http project uses this port, so Mvc must choose another)
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
                        throw new InvalidOperationException($"Certificate with subject '{subjectName}' not found in LocalMachine\\My store.");
                    }

                    listenOptions.UseHttps(certificate);
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=chat}/{action=Index}/{id?}");

            // add swagger ui
            app.UseSwagger();
            app.UseSwaggerUI();

            app.Run();
        }
    }
}
