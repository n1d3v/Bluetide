using Microsoft.AspNetCore.Mvc;

namespace Bluetide.Controllers
{
    [ApiController]
    [Route("/1.1/statuses")]
    public class StatusesController : Controller
    {
        [HttpGet("home_timeline.json")]
        public async Task<IActionResult> TwtHomeTimeline([FromQuery] TwtTimeline twtTimeline)
        {

            return Ok();
        }

        public class TwtTimeline
        {
            public int? count { get; set; }
            public int? since_id { get; set; }
            public int? max_id { get; set; }
            public bool? trim_user { get; set; }
            public bool? exclude_replies { get; set; }
            public bool? contributor_details { get; set; }
            public bool? include_entities { get; set; }
        }
    }
}