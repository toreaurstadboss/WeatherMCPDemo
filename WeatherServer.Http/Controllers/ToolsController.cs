using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace WeatherServer.Web.Http.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class ToolsController : ControllerBase
    {
        private readonly IMcpClient _client;
        private readonly IOptions<ModelContextProtocol.Protocol.Implementation> _mcpServerOptions;

        public ToolsController(IMcpClient client, IOptions<ModelContextProtocol.Protocol.Implementation> mcpServerOptions)
        {
            _client = client;
            _mcpServerOptions = mcpServerOptions;
        }

        [HttpGet(Name = "Overview")]
        [Produces("application/json")]
        public async Task<IActionResult> GetOverview()
        {
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "tools/list",
                id = 1
            };

            var tools = await _client.ListToolsAsync();
            //var prompts = await _client.ListPromptsAsync();
            //var resources = await _client.ListResourcesAsync();
            return Ok(new
            {
                ServerName = _mcpServerOptions.Value.Title,
                Version = _mcpServerOptions.Value.Version,
                Tools = tools,
                //Prompts = prompts,
                // Resources = resources
            });
        }

    }

}
