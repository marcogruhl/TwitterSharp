namespace TwitterSharp.Response.RTweet
{
    public class RTweet : Tweet, IRateLimit
    {
        public RateLimit RateLimit { get; set; }
    }
}
