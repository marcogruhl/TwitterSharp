using TwitterSharp.Response.RTweet;

namespace TwitterSharp.WpfClient.ViewModels
{
    internal class TweetViewModel
    {
        public string Type { get; set; }

        public Tweet Tweet { get; set; }

        public TweetViewModel(Tweet tweet, string type)
        {
            Tweet = tweet;
            Type = type;
        }
    }
}
