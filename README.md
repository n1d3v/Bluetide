# Bluetide
Bluetide is a Twitter 1.1 API to Bluesky converter, built using .NET.
# Why work on this?
I wanted to have a go at working with ASP.NET for an API, as previously I have always used Python and Flask and I wanted to make a change.

This has also never been done before, unlike the Twitter 1.0 API which has [Bluetweety](https://twitterbridge.loganserver.net/) which is what most people use on older mobile operating systems.
# What works right now?
> [!WARNING]
> The server is in alpha and as of now fails to reach the timeline as of now, it doesn't even authenticate properly, if you can help me out with this, anything is appreciated!

> [!NOTE]
> The server's API is based off of the 2013 [API v1.1 documentation](http://web.archive.org/web/20130508005540/https://dev.twitter.com/docs/api/1.1), support for newer endpoints and possibly clients will be added soon.

- [x] Twitter OAuth
   - [x] /oauth/access_token
      - Used for the login flow, is implemented and does work to an extent, scroll to the bottom to see what this means.
   - [x] /oauth/request_token
   - [x] /oauth/authorize
- [x] Twitter configuration (Developer)
   - [x] /1.1/help/configuration
      - Used to get the details required for Bluetide's server
   - [x] /1.1/help/languages
      - Used to get the supported languages from Bluetide's server
   - [x] /1.1/help/privacy
      - Used to get the current privacy policy from Bluetide's server
   - [x] /1.1/help/tos
      - Used to get the current terms of service from Bluetide's server
   - [ ] /1.1/application/rate_limit_status
      - Used to get the details of rate limiting from Bluetide's server, not required in our case as Bluesky handles rate limiting. This is implemented but all it does is send 200 OK back to the client.

# Issues with authentication
There is an issue with authentication on clients like Twitter version 5.7 on iOS and the Windows Phone Twitter client completely crashes signing in, however, we have found some interesting data to do with this:

- Twitter on iOS (iPhone 5 on iOS 6.1.4)
   - Twitter version 5.3.3: Signs in perfectly fine with no issues.
   - Twitter version 5.7: Fails with the error "xAuth migration failed - no token/secret handed back".
   - Twitter version 5.12: Fails with the same error as version 5.7.
   - Twitter version 6.11: Fails with a vague error along the lines of "Error authenticating with Twitter. Please try again.".
- Twitter on Windows Phone (Lumia 925 on Windows Phone 8.1 with a patched client)
   - Twitter version 3.2.3.0: Crashes when trying to authenticate, no error provided.
