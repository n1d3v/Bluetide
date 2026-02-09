using CobaltSky.Classes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Bluetide.Controllers
{
    [ApiController]
    [Route("/oauth")]
    public class AuthenticationController : Controller
    {
        private static readonly ConcurrentDictionary<string, OAuthTempToken> TempTokens = new();
        private readonly API api = new API();

        [HttpPost("request_token")]
        public IActionResult TwtReqToken()
        {
            bool isTimestampValid = false;
            var oauthHeader = this.HttpContext.Request.Headers["Authorization"].ToString();
            var oauthParams = ParseOAuthAuthorization(oauthHeader);

            if (long.TryParse(oauthParams.Timestamp, out long timestamp))
            {
                DateTime timestampDateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                DateTime currentUtcTime = DateTime.UtcNow;

                if ((currentUtcTime - timestampDateTime).TotalSeconds <= 300)
                {
                    isTimestampValid = true;
                }
                else
                {
                    isTimestampValid = false;
                }
            }

            var tokenParams = new OAuthTempToken();

            if (isTimestampValid)
            {
                string tempToken = Guid.NewGuid().ToString();
                string tempSecret = Guid.NewGuid().ToString();

                tokenParams.Token = tempToken;
                tokenParams.Secret = tempSecret;
                tokenParams.CreatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
                tokenParams.ExpiresAt = ((DateTimeOffset)(DateTime.UtcNow.AddMinutes(10))).ToUnixTimeSeconds();
                tokenParams.Callback = oauthParams.Callback;

                TempTokens[tempToken] = tokenParams;
            }
            else
            {
                return StatusCode(403, "Your timestamp is invalid now.");
            }

            string resString = $"oauth_token={tokenParams.Token}&oauth_token_secret={tokenParams.Secret}&oauth_callback_confirmed=true";
            return Ok(resString);
        }

        [HttpPost("access_token")]
        public async Task<IActionResult> TwtAccessToken()
        {
            Debug.WriteLine("Twitter wants to sign in, continuing flow.");

            var form = await Request.ReadFormAsync();
            var username = form["x_auth_username"].ToString();
            var password = form["x_auth_password"].ToString();
            var authMode = form["x_auth_mode"].ToString();

            var login = new LoginRequest
            {
                identifier = username,
                password = password
            };

            BskySessionResponse? session = null;
            var tcs = new TaskCompletionSource<bool>();

            await api.SendAPI("/com.atproto.server.createSession", "POST", login,
                callback: (response) =>
                {
                    session = JsonConvert.DeserializeObject<BskySessionResponse>(response);
                    tcs.SetResult(true);
                });
            await tcs.Task;

            Debug.WriteLine("Finished API call! Continuing further...");

            string resString = $"oauth_token={session.accessJwt}&user_id={session.did}&screen_name={session.handle}&x_auth_expires=0";
            return Content(resString, "application/x-www-form-urlencoded");
        }

        [HttpGet("authorize")]
        public IActionResult TwtAuthorizeOAuth([FromQuery] OAuthAuthorize oAuthAuth)
        {
            if (!TempTokens.TryGetValue(oAuthAuth.oauth_token!, out var tokenData))
            {
                return View("authorize", new
                {
                    RedirectUrl = "http://127.0.0.1/",
                    OauthToken = oAuthAuth.oauth_token
                });
            }
            else
            {
                return StatusCode(403, "oauth_token is invalid or empty.");
            }
        }

        // Helper methods
        public OAuthParams ParseOAuthAuthorization(string oauthHeader)
        {
            if (!oauthHeader.Contains("OAuth"))
            {
                // Just in case somehow the data gets parsed wrongly and it sends something else.
                throw new ArgumentException("Invalid OAuth header! It must contain 'OAuth'");
            }

            var paramsDict = new OAuthParams(); // Creates a basis to go off of for the values
            var headerContent = oauthHeader.Substring(6); // Trims "OAuth" off of the header
            var pairs = headerContent.Split(','); // Splits it based on the commas in the header string

            foreach (var pair in pairs)
            {
                var trimmedPair = pair.Trim();
                var keyValue = trimmedPair.Split('=');

                if (keyValue.Length != 2)
                {
                    continue;
                }

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim(' ', '"');
                value = Uri.UnescapeDataString(value);

                switch (key)
                {
                    case "oauth_callback":
                        paramsDict.Callback = value;
                        break;
                    case "oauth_consumer_key":
                        paramsDict.ConsumerKey = value;
                        break;
                    case "oauth_nonce":
                        paramsDict.Nonce = value;
                        break;
                    case "oauth_signature":
                        paramsDict.Signature = value;
                        break;
                    case "oauth_signature_method":
                        paramsDict.SignatureMethod = value;
                        break;
                    case "oauth_timestamp":
                        paramsDict.Timestamp = value;
                        break;
                    case "oauth_version":
                        paramsDict.Version = value;
                        break;
                    case "oauth_token":
                        paramsDict.Token = value;
                        break;
                    case "oauth_verifier":
                        paramsDict.Verifier = value;
                        break;
                }
            }
            return paramsDict;
        }

        // OAuth helper classes
        public class OAuthTempToken
        {
            public string? Token { get; set; }
            public string? Secret { get; set; }
            public long? CreatedAt { get; set; }
            public long? ExpiresAt { get; set; }
            public string? Callback { get; set; }
            public string? Verifier { get; set; }
            public BskySessionResponse? Session { get; set; }
        }

        public class OAuthParams
        {
            public string? Callback { get; set; }
            public string? ConsumerKey { get; set; }
            public string? Nonce { get; set; }
            public string? Signature { get; set; }
            public string? SignatureMethod { get; set; }
            public string? Timestamp { get; set; }
            public string? Version { get; set; }
            public string? Token { get; set; }
            public string? Verifier { get; set; }
        }

        public class OAuthAuthorize
        {
            public string? oauth_token { get; set; }
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