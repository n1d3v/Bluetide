using Bluetide.Classes;
using CobaltSky.Classes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using static Bluetide.Controllers.StatusesController;

namespace Bluetide.Controllers
{
    [ApiController]
    [Route("1.1/statuses")]
    public class StatusesController : Controller
    {
        private readonly API api = new API();

        [HttpGet("home_timeline.json")]
        public async Task<IActionResult> TwtHomeTimeline([FromQuery] TwtTimeline twtTimeline)
        {
            Debug.WriteLine("Twitter requested for the home page!");
            var authHeader = Request.Headers["Authorization"].ToString();
            var authToken = GlobalHelper.ExtractTokenFromHeader(authHeader);
            Debug.WriteLine($"The extracted token is: {authToken}");
            string? bskyFeed = null;

            var headers = new Dictionary<string, string>
            {
                { "Accept", "*/*" },
                { "Accept-Language", "en" },
                { "authorization", $"Bearer {authToken}" }
            };

            await api.SendAPI("/app.bsky.actor.getPreferences", "GET", null, res =>
            {
                try
                {
                    var root = JsonConvert.DeserializeObject<PreferencesRoot>(res);

                    var feed = root?.Preferences?
                        .Where(p => p.Items != null)
                        .SelectMany(p => p?.Items)
                        .FirstOrDefault(i => i.Type == "feed");

                    bskyFeed = feed?.ToString();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Preferences parse failed: {ex}");
                }
            }, headers);


            await api.SendAPI($"/app.bsky.feed.getFeed?feed={bskyFeed}&limit={twtTimeline.count}", "GET", null, res =>
            {
                try
                {
                    Debug.WriteLine($"The response from Bluesky was: {res}");
                }
                catch (Exception ex)
                {
                }
            }, headers);
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

        class PreferencesRoot
        {
            [JsonProperty("preferences")]
            public List<PreferenceItem>? Preferences { get; set; }
        }

        class PreferenceItem
        {
            [JsonProperty("$type")]
            public string? Type { get; set; }

            [JsonProperty("items")]
            public List<FeedItem>? Items { get; set; }
        }

        class FeedItem
        {
            [JsonProperty("type")]
            public string? Type { get; set; }

            [JsonProperty("value")]
            public string? Value { get; set; }

            [JsonProperty("pinned")]
            public bool Pinned { get; set; }

            [JsonProperty("id")]
            public string? Id { get; set; }
        }
    }

    [ApiController]
    [Route("1/statuses")]
    public class StatusesControllerAPIOne : Controller
    {
        [HttpGet("update.json")]
        public async Task<IActionResult> TwtPost([FromQuery] TwtTimeline twtTimeline)
        {
            return Ok();
        }
    }
}