using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bluetide.Controllers
{
    [ApiController]
    [Route("/1.1/friends")]
    public class FriendsController : Controller
    {
        [HttpGet("ids.json")]
        public async Task<IActionResult> TwtFriendIds([FromQuery] TwtId twtId) {
            Debug.WriteLine($"The user ID from the query is {twtId.user_id}");
            
            // Returns a dummy JSON file, this is all we need really.
            if (twtId.user_id == 0)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "friend-json-start.json");
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                return Ok(json);
            }

            return Ok();
        }

        public class TwtId
        {
            public int? user_id { get; set; }
        }
    }
}
