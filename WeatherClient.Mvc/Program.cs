using Anthropic.SDK;
using Microsoft.Extensions.AI;

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
                pattern: "{controller=Chat}/{action=Index}/{id?}");

            // add swagger ui
            app.UseSwagger();
            app.UseSwaggerUI();

            app.Run();
        }
    }
}
