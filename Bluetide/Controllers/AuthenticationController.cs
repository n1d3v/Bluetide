using System;
using Bluetide.Classes;
using CobaltSky.Classes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

            string? bskyAccessJwt = null;
            string? bskyHandle = null;
            string? bskyTempDid = null;
            string? bskyDid = null;
            bool failedLogin = false;
            BskySessionResponse? bskySession = null;

            await api.SendAPI("/com.atproto.server.createSession", "POST", login,
                callback: (response) =>
                {
                    if (response.Contains("Unauthorized"))
                    {
                        failedLogin = true;
                    }

                    if (failedLogin)
                    {
                        // Continue, later we'll return 401 later.
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
                return StatusCode(401, "Your handle or password is incorrect, please try again!");
            }
            else
            {
                if (bskySession != null)
                {
                    bskyAccessJwt = bskySession.accessJwt;
                    bskyHandle = bskySession.handle;
                    bskyTempDid = bskySession.did;
                    bskyDid = BaseEncoding.EncodeText(bskyTempDid);
                    Debug.WriteLine($"The final ID for Bluesky is {bskyDid}, which decodes into {BaseEncoding.DecodeText(bskyDid)}");
                }
            }

            string resString = $"oauth_token={bskyAccessJwt}&oauth_token_secret={GenerateToken()}&user_id={bskyDid}&screen_name={bskyHandle}";
            Debug.WriteLine($"The string that was created is: {resString}");

            return Ok(resString);
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


        public static string GenerateToken(int length = 40)
        {
            // Generates an oauth_token_secret, it isn't real but provides something along the lines of it.
            // Probably not the best way to do this to be fair, could've used something other than Cryptography.
            string Charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            char[] result = new char[length];
            byte[] buffer = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            for (int i = 0; i < length; i++)
            {
                result[i] = Charset[buffer[i] % Charset.Length];
            }

            return new string(result);
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

        // Bluesky login classes
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