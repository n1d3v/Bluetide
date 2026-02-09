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
      - Used for the login flow, it is implemented but right now the Twitter app on Windows Phone crashes trying to login and the iOS app (iOS 6) results in an error saying "Error authenticating with Twitter. Please try again.", if you know anything about this, please let me know!
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
