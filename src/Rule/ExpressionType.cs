﻿namespace TwitterSharp.Rule
{
    public enum ExpressionType
    {
        None, Or, And, Negate, Keyword, Hashtag, Cashtag, Mention, Author, Recipient, Url, Retweet, Context, Entity, 
        ConversationId, Bio, BioLocation, Place, PlaceCountry, PointRadius, BoundingBox, IsRetweet, IsReply, IsQuote, 
        IsVerified, IsNotNullcast, HasHashtags, HasCashtags, HasLinks, HasMentions, HasMedia, HasImages, HasVideos,
        HasGeo, Sample, Lang
    }
}
