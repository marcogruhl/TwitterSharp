using System;

namespace TwitterSharp.Response.RTweet
{
    public class TweetCountInRange : IEquatable<TweetCountInRange>
    {
        public DateTime? Start { init; get; }
        public DateTime? End { init; get; }
        public int TweetCount { init; get; }

        public bool Equals(TweetCountInRange other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Nullable.Equals(Start, other.Start) && Nullable.Equals(End, other.End) && TweetCount == other.TweetCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TweetCountInRange)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End, TweetCount);
        }
    }
}
