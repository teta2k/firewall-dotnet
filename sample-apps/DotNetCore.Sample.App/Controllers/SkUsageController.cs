using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace DotNetCore.Sample.App.Controllers
{
    [ApiController]
    [Route("sk-usage")]
    public class SkUsageController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SkUsageController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Example: GET /sk-usage/model/gemini-2.5-flash?input=Hello
        [HttpGet("model/{model}")]
        public async Task<IActionResult> GetSkUsage([FromRoute] string model, [FromQuery] string input)
        {
            var apiKey = _configuration["AI:GeminiApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return BadRequest("Missing Gemini API key.");
            if (string.IsNullOrWhiteSpace(input))
                return BadRequest("Missing input text.");

            var kernel = Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion(modelId: model, apiKey: apiKey)
                .Build();

            var response = await kernel.InvokePromptAsync(input);
            return Ok(response.GetValue<string>());
        }
    }
}
