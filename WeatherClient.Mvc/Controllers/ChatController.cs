using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WeatherClient.Mvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : Controller
    {

        public class ChatRequest
        {
            [Required]
            public string Message { get; set; } = string.Empty;
        }

        private readonly ILogger<ChatController> _logger;
        private readonly IChatClient _chatClient; public ChatController(ILogger<ChatController> logger, IChatClient chatClient)
        {
            _logger = logger;
            _chatClient = chatClient;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("Index");
        }


        [HttpPost(Name = "Chat")]
        [Produces("text/plain")]
        public async Task Chat([FromBody] ChatRequest chatRequest)
        {
            //TODO : Add support for 'chat history' to gradually build context here - repetively provide more info and context to the Claude LLM.

            Response.ContentType = "text/plain";
            Response.Headers.Append("Cache-Control", "no-cache");

            if (string.IsNullOrWhiteSpace(chatRequest.Message))
            {

                var error = Encoding.UTF8.GetBytes("Please provide your message.");
                await Response.Body.WriteAsync(error);
                await Response.Body.FlushAsync();
                return;
            }

            // Create MCP client connecting to our MCP server
            await using var mcpClient = await McpClient.CreateAsync(
                new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri("https://localhost:7145/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp
                })
            );
            // Get available tools from the MCP server
            var tools = await mcpClient.ListToolsAsync();
            // Set up the chat messages
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, "You are a helpful assistant.")
            };
            messages.Add(new(ChatRole.User, chatRequest.Message));
            // Get streaming response and collect updates
            List<ChatResponseUpdate> updates = [];

            await foreach (var update in _chatClient.GetStreamingResponseAsync(
                messages,
                new ChatOptions
                {
                    ModelId = "claude-3-haiku-20240307",
                    MaxOutputTokens = 1000,
                    Tools = [.. tools]
                }

            ))
            {
                var text = update.ToString();
                var bytes = Encoding.UTF8.GetBytes(text);
                await Response.Body.WriteAsync(bytes);
                await Response.Body.FlushAsync();
            }
        }

    }
}