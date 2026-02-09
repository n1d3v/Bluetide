using Microsoft.AspNetCore.Mvc;

namespace Bluetide.Controllers
{
    [ApiController]
    [Route("/1.1/help")]
    public class HelpController : Controller
    {
        [HttpGet("configuration.json")]
        public async Task<IActionResult> TwtSendConfig()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "twt-config.json");
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return Ok(json);
        }

        [HttpGet("languages.json")]
        public async Task<IActionResult> TwtSendLanguages()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "twt-language-config.json");
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return Ok(json);
        }

        [HttpGet("privacy.json")]
        public async Task<IActionResult> TwtSendPrivacy()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "bt-privacy-policy.json");
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return Ok(json);
        }

        [HttpGet("tos.json")]
        public async Task<IActionResult> TwtSendTOS()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "bt-terms-of-service.json");
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return Ok(json);
        }
    }

    [ApiController]
    [Route("/1.1/application")]
    public class HelpAppController : Controller
    {
        [HttpGet("rate_limit_status.json")]
        public IActionResult TwtSendRLSettings()
        {
            return Ok();
        }
    }
}