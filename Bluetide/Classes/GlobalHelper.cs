namespace Bluetide.Classes
{
    public class GlobalHelper
    {
        public static string ExtractTokenFromHeader(string oauthHeader)
        {
            if (!oauthHeader.Contains("OAuth"))
            {
                // Just in case somehow the data gets parsed wrongly and it sends something else.
                throw new ArgumentException("Invalid OAuth header! It must contain 'OAuth'");
            }

            string? bskyToken = null;
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
                    case "oauth_token":
                        bskyToken = value;
                        break;
                }
            }
            return bskyToken ?? string.Empty;
        }
    }
}
