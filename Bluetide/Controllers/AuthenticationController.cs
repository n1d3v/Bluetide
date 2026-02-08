using CobaltSky.Classes;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace Bluetide.Controllers
{
    [ApiController]
    [Route("/oauth")]
    public class AuthenticationController : Controller
    {
        private readonly API api = new API();
        private string? bskyAccessJwt = null;

        [HttpPost("access_token")]
        public async Task<IActionResult> TwtLogin([FromForm] BskyLogin bskyLogin)
        {
            Debug.WriteLine("Twitter is trying to sign in!");

            // This is the data Twitter sends to us from the application
            // http://web.archive.org/web/20130508171323/https://dev.twitter.com/docs/api/1/post/oauth/access_token
            string? authMode = bskyLogin.x_auth_mode;
            string? handle = bskyLogin.x_auth_username;
            string? password = bskyLogin.x_auth_password;

            Debug.WriteLine($"The device sent us the handle: {handle} and password: {password}");

            var login = new LoginRequest
            {
                identifier = handle,
                password = password
            };

            string? bskyHandle = null;
            string? bskyDid = null;
            bool failedLogin = false;
            BskySessionResponse? bskySession = null;

            await api.SendAPI("/com.atproto.server.createSession", "POST", login,
                callback: (response) => {
                    if (response.Contains("Unauthorized")) 
                    {
                        failedLogin = true;
                    }

                    if (failedLogin)
                    {
                        // Continue, later we'll return 403 later.
                    }
                    else
                    {
                        bskySession = JsonConvert.DeserializeObject<BskySessionResponse>(response);
                    }
                }
            );

            if (failedLogin)
            {
                // Return forbidden manually since we can't use Forbid
                return StatusCode(403, "Your handle or password is incorrect, please try again!");
            }
            else
            {
                if (bskySession != null)
                {
                    bskyAccessJwt = bskySession.accessJwt;
                    bskyHandle = bskySession.handle;
                    bskyDid = bskySession.did.Substring(8); // We remove the first 8 characters (did:plc:) to match a Twitter user ID
                }
            }

            // GenerateSecret is kind of like a placeholder, Bluesky doesn't have secrets so this is our way of implementing them.
            string resString = $"oauth_token={bskyDid}-{bskyAccessJwt}&oauth_token_secret={GenerateSecret()}&user_id={bskyDid}&screen_name={bskyHandle}";
            Debug.WriteLine($"The string that was created is: {resString}");

            return Ok(resString);
        }

        // Helper functions
        public string GenerateSecret()
        {
            Random random = new Random();
            string secretString = new string(Enumerable.Range(0, 40)
                                                       .Select(_ => bskyAccessJwt[random.Next(bskyAccessJwt.Length)])
                                                       .ToArray());
            return secretString;
        }

        // OAuth login classes
        public class BskyLogin
        {
            public string? x_auth_mode { get; set; }
            public string? x_auth_username { get; set; }
            public string? x_auth_password { get; set; }
        }

        public class LoginRequest
        {
            public string? identifier { get; set; }
            public string? password { get; set; }
        }

        public class BskySessionResponse
        {
            public string? did { get; set; }
            public string? accessJwt { get; set; }
            public string? handle { get; set; }
        }
    }
}
