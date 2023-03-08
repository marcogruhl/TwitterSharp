using System.Net;
using TwitterSharp;
using TwitterSharp.Client;

namespace ConsoleAppOAuth2;

internal class Program
{
    static void Main(string[] args)
    {
        string clientId = Environment.GetEnvironmentVariable("TWITTER_CLIENT_ID");
        string clientSecret = Environment.GetEnvironmentVariable("TWITTER_CLIENT_SECRET");
        List<string> scopes = new List<string>
        {
            "tweet.read",
            "users.read",
            // "follows.read",
            // "like.read",
            // "list.read",
            // "offline.access"
        };

        var token = OAuthHelper.Auth(clientId, clientSecret, scopes, $"http://{IPAddress.Loopback}:5057/", "https://github.com/Xwilarg/TwitterSharp").Result;
        var client = new TwitterClient(token);
        var answer = client.GetMeAsync().Result;
        var timeline = client.GetTimelineForUserAsync(Environment.GetEnvironmentVariable("TWITTER_OWN_ID")).Result;
    }
}