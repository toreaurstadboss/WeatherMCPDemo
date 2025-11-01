using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);

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

//builder.Logging.ClearProviders();

IClientTransport clientTransport;

var (command, arguments) = GetCommandAndArguments(args);

logger.LogInformation("Starting MCP client with command: {Command} and arguments: {Arguments}", command, string.Join(' ', arguments));

if (command == "http")
{
    logger.LogInformation("Using SSE transport to connect to MCP server at http://localhost:3001");
    clientTransport = new SseClientTransport(new()
    {
        Endpoint = new Uri("http://localhost:3001")
    });
}
else
{
    logger.LogInformation("Using Stdio transport to start MCP server with command: {Command} and arguments: {Arguments}", command, string.Join(' ', arguments));
    clientTransport = new StdioClientTransport(new()
    {
        Name = "Demo Server",
        Command = command,
        Arguments = arguments
    });
}

try
{
    // Client Initialization setting up the transport type and commands to run the server 
    await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(clientTransport);

    var tools = await mcpClient.ListToolsAsync();

    foreach (var tool in tools)
    {
        logger.LogInformation("Connected to server with tool: {ToolName}", tool.Name);
        Console.WriteLine($"Connected to server with tools: {tool.Name}");
    }

    using var anthropicClient = new AnthropicClient(new APIAuthentication(builder.Configuration["ANTHROPIC_API_KEY"]))
            .Messages
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

    var options = new ChatOptions
    {
        MaxOutputTokens = 1000,
        ModelId = "claude-3-haiku-20240307",
        Tools = [.. tools]
    };

    Console.ForegroundColor = ConsoleColor.Green;
    logger.LogInformation("MCP Client Started");
    Console.WriteLine("MCP Client Started");
    Console.ResetColor();

    var messages = new List<ChatMessage>();
    var sb = new StringBuilder();

    PromptForInput();

    while (Console.ReadLine() is string query && query.Equals("exit", StringComparison.Ordinal) is false)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            PromptForInput();
            continue;
        }

        messages.Add(new ChatMessage(ChatRole.User, query));
        await foreach (var message in anthropicClient.GetStreamingResponseAsync(messages, options))
        {
            //logger.LogTrace("Received message: {Message}", message.ToString());
            Console.Write(message);
            sb.Append(message.ToString());
        }

        Console.WriteLine();
        sb.AppendLine();
        messages.Add(new ChatMessage(ChatRole.Assistant, sb.ToString()));
        sb.Clear();

        PromptForInput();
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while running the MCP client.");
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {ex.Message}");
    Console.ResetColor();
}

static void PromptForInput()
{
    Console.WriteLine("Enter a command (or 'exit' to quit):");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("> ");
    Console.ResetColor();
}

/// <summary>
/// Represents an instance of the MCP client used to communicate with the server.
/// The client is created asynchronously using the specified transport and should be disposed asynchronously when no longer needed.
/// </summary>
/// <remarks>
/// This method uses the file extension of the first argument to determine the command, if it's py, it'll run python,
/// if it's js, it'll run node, if it's a directory or a csproj file, it'll run dotnet.
///
/// If no arguments are provided, it defaults to running the QuickstartWeatherServer project from the current repo.
///
/// This method would only be required if you're creating a generic client, such as we use for the quickstart.
/// </remarks>
static (string command, string[] arguments) GetCommandAndArguments(string[] args)
{
    return args switch
    {
        [var mode] when mode.Equals("http", StringComparison.OrdinalIgnoreCase) => ("http", args),
        [var script] when script.EndsWith(".py") => ("python", args),
        [var script] when script.EndsWith(".js") => ("node", args),
        [var script] when Directory.Exists(script) || (File.Exists(script) && script.EndsWith(".csproj")) => ("dotnet", ["run", "--project", script]),
        _ => ("dotnet", ["run", "--project", Path.Combine(GetCurrentSourceDirectory(), @"..\WeatherServer"), "--no-build"])
    };
}

static string GetCurrentSourceDirectory([CallerFilePath] string? currentFile = null)
{
    Debug.Assert(!string.IsNullOrWhiteSpace(currentFile));
    var currentSourceDirectory = Path.GetDirectoryName(currentFile) ?? throw new InvalidOperationException("Unable to determine source directory.");
    //bool foundServer = Directory.Exists(Path.Combine(currentSourceDirectory, "../WeatherServer"));
    return currentSourceDirectory;
}

