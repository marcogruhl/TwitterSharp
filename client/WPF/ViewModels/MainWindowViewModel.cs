using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using TwitterSharp.Response.RStream;
using TwitterSharp.Response.RTweet;
using TwitterSharp.Rule;
using TwitterSharp.WpfClient.Helper;

namespace TwitterSharp.WpfClient.ViewModels;

internal class MainWindowViewModel : BindableBaseLight
{
    public ListCollectionView TweetsCollectionView { get; }
    public ListCollectionView RulesCollectionView { get; }
    public ListCollectionView RateLimitsCollectionView { get; }
    private Controller _controller = new();
    public DelegateCommand<StreamInfo> DeleteRuleCommand { get; set; }

    #region Search Expression
    
    private string _keyword;
    [IsExpressionProperty]
    public string Keyword
    {
        get => _keyword;
        set => SetProperty(ref _keyword, value);
    }

    private string _hashtag;
    [IsExpressionProperty]
    public string Hashtag
    {
        get => _hashtag;
        set => SetProperty(ref _hashtag, value);
    }

    private string _mention;
    [IsExpressionProperty]
    public string Mention
    {
        get => _mention;
        set => SetProperty(ref _mention, value);
    }

    private string _cashtag;
    [IsExpressionProperty]
    public string Cashtag
    {
        get => _cashtag;
        set => SetProperty(ref _cashtag, value);
    }

    private string _author;
    [IsExpressionProperty]
    public string Author
    {
        get => _author;
        set => SetProperty(ref _author, value);
    }

    private string _recipient;
    [IsExpressionProperty]
    public string Recipient
    {
        get => _recipient;
        set => SetProperty(ref _recipient, value);
    }

    private string _url;
    [IsExpressionProperty]
    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    private string _retweet;
    [IsExpressionProperty]
    public string Retweet
    {
        get => _retweet;
        set => SetProperty(ref _retweet, value);
    }

    private string _context;
    [IsExpressionProperty]
    public string Context
    {
        get => _context;
        set => SetProperty(ref _context, value);
    }

    private string _entity;
    [IsExpressionProperty]
    public string Entity
    {
        get => _entity;
        set => SetProperty(ref _entity, value);
    }

    private string _conversionId;
    [IsExpressionProperty]
    public string ConversionId
    {
        get => _conversionId;
        set => SetProperty(ref _conversionId, value);
    }

    private string _bio;
    [IsExpressionProperty]
    public string Bio
    {
        get => _bio;
        set => SetProperty(ref _bio, value);
    }

    private string _bioLocation;
    [IsExpressionProperty]
    public string BioLocation
    {
        get => _bioLocation;
        set => SetProperty(ref _bioLocation, value);
    }

    private string _place;
    [IsExpressionProperty]
    public string Place
    {
        get => _place;
        set => SetProperty(ref _place, value);
    }

    private string _placeCountry;
    [IsExpressionProperty]
    public string PlaceCountry
    {
        get => _placeCountry;
        set => SetProperty(ref _placeCountry, value);
    }

    private bool? _isRetweet;
    [IsExpressionProperty]
    public bool? IsRetweet
    {
        get => _isRetweet;
        set => SetProperty(ref _isRetweet, value);
    }

    private bool? _isReply;
    [IsExpressionProperty]
    public bool? IsReply
    {
        get => _isReply;
        set => SetProperty(ref _isReply, value);
    }

    private bool? _IsQuote;
    [IsExpressionProperty]
    public bool? IsQuote
    {
        get => _IsQuote;
        set => SetProperty(ref _IsQuote, value);
    }

    private bool? _IsVerified;
    [IsExpressionProperty]
    public bool? IsVerified
    {
        get => _IsVerified;
        set => SetProperty(ref _IsVerified, value);
    }

    private bool? _isNotNullcast;
    [IsExpressionProperty]
    public bool? IsNotNullcast
    {
        get => _isNotNullcast;
        set => SetProperty(ref _isNotNullcast, value);
    }

    private bool? _hasHashtags;
    [IsExpressionProperty]
    public bool? HasHashtags
    {
        get => _hasHashtags;
        set => SetProperty(ref _hasHashtags, value);
    }

    private bool? _hasCashtags;
    [IsExpressionProperty]
    public bool? HasCashtags
    {
        get => _hasCashtags;
        set => SetProperty(ref _hasCashtags, value);
    }

    private bool? _hasLinks;
    [IsExpressionProperty]
    public bool? HasLinks
    {
        get => _hasLinks;
        set => SetProperty(ref _hasLinks, value);
    }

    private bool? _hasMentions;
    [IsExpressionProperty]
    public bool? HasMentions
    {
        get => _hasMentions;
        set => SetProperty(ref _hasMentions, value);
    }

    private bool? _hasMedia;
    [IsExpressionProperty]
    public bool? HasMedia
    {
        get => _hasMedia;
        set => SetProperty(ref _hasMedia, value);
    }

    private bool? _hasImages;
    [IsExpressionProperty]
    public bool? HasImages
    {
        get => _hasImages;
        set => SetProperty(ref _hasImages, value);
    }

    private bool? _hasVideos;
    [IsExpressionProperty]
    public bool? HasVideos
    {
        get => _hasVideos;
        set => SetProperty(ref _hasVideos, value);
    }

    private bool? _hasGeo;
    [IsExpressionProperty]
    public bool? HasGeo
    {
        get => _hasGeo;
        set => SetProperty(ref _hasGeo, value);
    }

    private int _sample;
    [IsExpressionProperty]
    public int Sample
    {
        get => _sample;
        set => SetProperty(ref _sample, value);
    }

    private string _lang;
    [IsExpressionProperty]
    public string Lang
    {
        get => _lang;
        set => SetProperty(ref _lang, value);
    }

    #endregion

    private bool _updateFilterNeeded;

    public bool UpdateFilterNeeded
    {
        get => _updateFilterNeeded;
        set => SetProperty(ref _updateFilterNeeded, value);
    }

    private string _expressionString;

    public string ExpressionString
    {
        get => _expressionString;
        private set
        {
            if (SetProperty(ref _expressionString, value))
            {
                OnPropertyChanged(nameof(ExpressionLength));
                OnPropertyChanged(nameof(ExpressionLengthLimit));
            }
        }
    }

    public const int RuleCharacterLimit = 500;

    public int ExpressionLength => ExpressionString == null ? 0 : ExpressionString.Length;
    public bool ExpressionLengthLimit => ExpressionLength < RuleCharacterLimit;

    private string _error;

    public string Error
    {
        get => _error;
        set => SetProperty(ref _error, value);
    }

    public MainWindowViewModel()
    {
        TweetsCollectionView = new ListCollectionView(_controller.Tweets);
        TweetsCollectionView.SortDescriptions.Add(new SortDescription(nameof(Tweet.CreatedAt), ListSortDirection.Descending));
        RulesCollectionView = new ListCollectionView(_controller.Rules);
        RateLimitsCollectionView = new ListCollectionView(_controller.RateLimits);
        DeleteRuleCommand = new DelegateCommand<StreamInfo>(DeleteRuleAction);

        PropertyChanged += OnPropertyChanged;
    }

    private void DeleteRuleAction(StreamInfo obj)
    {

    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (IsExpressionPropertyAttribute.IsExpressionProperty(e.PropertyName, this))
        {
            RefreshRule();
        }
    }

    private async void RefreshRule(bool force = false)
    {
        var expression = BuildExpression();
        var expressionString = ExpressionString = expression.ToString();
    }
    
    private Expression BuildExpression()
    {
        List<Expression> expressions = new List<Expression>();
        List<Expression> langExpressions = new List<Expression>();

        Keyword?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)?.ForEach(x => expressions.Add(Expression.Keyword(x)));
        Hashtag?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)?.ForEach(x => expressions.Add(Expression.Hashtag(x)));
        Mention?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)?.ForEach(x => expressions.Add(Expression.Mention(x)));
        Cashtag?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)?.ForEach(x => expressions.Add(Expression.Cashtag(x)));

        Lang?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)?.ForEach(x => langExpressions.Add(Expression.Lang(x)));

        if(langExpressions.Any())
            expressions.Add(langExpressions[0].Or(langExpressions.Skip(1).ToArray()));

        if (IsReply != null) expressions.Add(IsReply.Value ? Expression.IsReply() : Expression.IsReply().Negate());
        if (IsRetweet != null) expressions.Add(IsRetweet.Value ? Expression.IsRetweet() : Expression.IsRetweet().Negate());
        if (IsQuote != null) expressions.Add(IsQuote.Value ? Expression.IsQuote() : Expression.IsQuote().Negate());
        if (IsVerified != null) expressions.Add(IsVerified.Value ? Expression.IsVerified() : Expression.IsVerified().Negate());
        // if (IsNotNullcast != null) expressions.Add(IsNotNullcast.Value ? Expression.IsNotNullcast() : Expression.IsNotNullcast().Negate());
        if (HasHashtags != null) expressions.Add(HasHashtags.Value ? Expression.HasHashtags() : Expression.HasHashtags().Negate());
        if (HasCashtags != null) expressions.Add(HasCashtags.Value ? Expression.HasCashtags() : Expression.HasCashtags().Negate());
        if (HasLinks != null) expressions.Add(HasLinks.Value ? Expression.HasLinks() : Expression.HasLinks().Negate());
        if (HasMentions != null) expressions.Add(HasMentions.Value ? Expression.HasMentions() : Expression.HasMentions().Negate());
        if (HasMedia != null) expressions.Add(HasMedia.Value ? Expression.HasMedia() : Expression.HasMedia().Negate());
        if (HasImages != null) expressions.Add(HasImages.Value ? Expression.HasImages() : Expression.HasImages().Negate());
        if (HasVideos != null) expressions.Add(HasVideos.Value ? Expression.HasVideos() : Expression.HasVideos().Negate());
        if (HasGeo != null) expressions.Add(HasGeo.Value ? Expression.HasGeo() : Expression.HasGeo().Negate());

        var expression = expressions.Any()
            ? expressions.First().And(expressions.Skip(1).ToArray())
            : Expression.Keyword("");
        // Error-Test:
        // expression = Expression.Keyword(Keyword).And(Expression.IsReply().Negate(), Expression.IsRetweet().Negate(), Expression.PlaceCountry("xx"));

        return expression;
    }
}