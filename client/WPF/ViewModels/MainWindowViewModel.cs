﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    private Controller _controller { get; }
    public DelegateCommand<StreamInfo> DeleteRuleCommand { get; set; }
    public DelegateCommand GetRecentCommand { get; set; }

    private string _bearerToken = ConfigHelper.GetValue(nameof(BearerToken), Environment.GetEnvironmentVariable("TWITTER_TOKEN"));

    public string BearerToken
    {
        get => _bearerToken;
        set 
        {
            Error = String.Empty;
            ConfigHelper.SetValue(ref _bearerToken, value);
            _controller.InitializeAsync(_bearerToken);
        }
    }

    #region Search Expression
    
    private string _keyword = ConfigHelper.GetValue(nameof(Keyword), String.Empty);
    [IsExpressionProperty]
    public string Keyword
    {
        get => _keyword;
        set => ConfigHelper.SetValue(ref _keyword, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _hashtag = ConfigHelper.GetValue(nameof(Hashtag), "Anime");
    [IsExpressionProperty]
    public string Hashtag
    {
        get => _hashtag;
        set => ConfigHelper.SetValue(ref _hashtag, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _mention = ConfigHelper.GetValue(nameof(Mention), String.Empty);
    [IsExpressionProperty]
    public string Mention
    {
        get => _mention;
        set => ConfigHelper.SetValue(ref _mention, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _cashtag = ConfigHelper.GetValue(nameof(Cashtag), String.Empty);
    [IsExpressionProperty]
    public string Cashtag
    {
        get => _cashtag;
        set => ConfigHelper.SetValue(ref _cashtag, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _author = ConfigHelper.GetValue(nameof(Author), String.Empty);
    [IsExpressionProperty]
    public string Author
    {
        get => _author;
        set => ConfigHelper.SetValue(ref _author, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _recipient = ConfigHelper.GetValue(nameof(Recipient), String.Empty);
    [IsExpressionProperty]
    public string Recipient
    {
        get => _recipient;
        set => ConfigHelper.SetValue(ref _recipient, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _url = ConfigHelper.GetValue(nameof(Url), String.Empty);
    [IsExpressionProperty]
    public string Url
    {
        get => _url;
        set => ConfigHelper.SetValue(ref _url, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _retweet = ConfigHelper.GetValue(nameof(Retweet), String.Empty);
    [IsExpressionProperty]
    public string Retweet
    {
        get => _retweet;
        set => ConfigHelper.SetValue(ref _retweet, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _context = ConfigHelper.GetValue(nameof(Context), String.Empty);
    [IsExpressionProperty]
    public string Context
    {
        get => _context;
        set => ConfigHelper.SetValue(ref _context, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _entity = ConfigHelper.GetValue(nameof(Entity), String.Empty);
    [IsExpressionProperty]
    public string Entity
    {
        get => _entity;
        set => ConfigHelper.SetValue(ref _entity, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _conversionId = ConfigHelper.GetValue(nameof(ConversionId), String.Empty);
    [IsExpressionProperty]
    public string ConversionId
    {
        get => _conversionId;
        set => ConfigHelper.SetValue(ref _conversionId, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _bio = ConfigHelper.GetValue(nameof(Bio), String.Empty);
    [IsExpressionProperty]
    public string Bio
    {
        get => _bio;
        set => ConfigHelper.SetValue(ref _bio, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _bioLocation = ConfigHelper.GetValue(nameof(BioLocation), String.Empty);
    [IsExpressionProperty]
    public string BioLocation
    {
        get => _bioLocation;
        set => ConfigHelper.SetValue(ref _bioLocation, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _place = ConfigHelper.GetValue(nameof(Place), String.Empty);
    [IsExpressionProperty]
    public string Place
    {
        get => _place;
        set => ConfigHelper.SetValue(ref _place, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _placeCountry = ConfigHelper.GetValue(nameof(PlaceCountry), String.Empty);
    [IsExpressionProperty]
    public string PlaceCountry
    {
        get => _placeCountry;
        set => ConfigHelper.SetValue(ref _placeCountry, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _isRetweet = ConfigHelper.GetValue(nameof(IsRetweet), false);
    [IsExpressionProperty]
    public bool? IsRetweet
    {
        get => _isRetweet;
        set => ConfigHelper.SetValue(ref _isRetweet, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _isReply = ConfigHelper.GetValue(nameof(IsReply), (bool?)null);
    [IsExpressionProperty]
    public bool? IsReply
    {
        get => _isReply;
        set => ConfigHelper.SetValue(ref _isReply, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _isQuote = ConfigHelper.GetValue(nameof(IsQuote), (bool?)null);
    [IsExpressionProperty]
    public bool? IsQuote
    {
        get => _isQuote;
        set => ConfigHelper.SetValue(ref _isQuote, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _isVerified = ConfigHelper.GetValue(nameof(IsVerified), true);
    [IsExpressionProperty]
    public bool? IsVerified
    {
        get => _isVerified;
        set => ConfigHelper.SetValue(ref _isVerified, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _isNotNullcast = ConfigHelper.GetValue(nameof(IsNotNullcast), (bool?)null);
    [IsExpressionProperty]
    public bool? IsNotNullcast
    {
        get => _isNotNullcast;
        set => ConfigHelper.SetValue(ref _isNotNullcast, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _hasHashtags = ConfigHelper.GetValue(nameof(HasHashtags), (bool?)null);
    [IsExpressionProperty]
    public bool? HasHashtags
    {
        get => _hasHashtags;
        set => ConfigHelper.SetValue(ref _hasHashtags, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _hasCashtags = ConfigHelper.GetValue(nameof(HasCashtags), (bool?)null);
    [IsExpressionProperty]
    public bool? HasCashtags
    {
        get => _hasCashtags;
        set => ConfigHelper.SetValue(ref _hasCashtags, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _hasLinks = ConfigHelper.GetValue(nameof(HasLinks), (bool?)null);
    [IsExpressionProperty]
    public bool? HasLinks
    {
        get => _hasLinks;
        set => ConfigHelper.SetValue(ref _hasLinks, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _hasMentions = ConfigHelper.GetValue(nameof(HasMentions), (bool?)null);
    [IsExpressionProperty]
    public bool? HasMentions
    {
        get => _hasMentions;
        set => ConfigHelper.SetValue(ref _hasMentions, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _hasMedia = ConfigHelper.GetValue(nameof(HasMedia), (bool?)null);
    [IsExpressionProperty]
    public bool? HasMedia
    {
        get => _hasMedia;
        set => ConfigHelper.SetValue(ref _hasMedia, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _hasImages = ConfigHelper.GetValue(nameof(HasImages), (bool?)null);
    [IsExpressionProperty]
    public bool? HasImages
    {
        get => _hasImages;
        set => ConfigHelper.SetValue(ref _hasImages, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _hasVideos = ConfigHelper.GetValue(nameof(HasVideos), (bool?)null);
    [IsExpressionProperty]
    public bool? HasVideos
    {
        get => _hasVideos;
        set => ConfigHelper.SetValue(ref _hasVideos, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private bool? _hasGeo = ConfigHelper.GetValue(nameof(HasGeo), (bool?)null);
    [IsExpressionProperty]
    public bool? HasGeo
    {
        get => _hasGeo;
        set => ConfigHelper.SetValue(ref _hasGeo, value, propertyChangedAction: () => OnPropertyChanged());
    }

    private string _lang = ConfigHelper.GetValue(nameof(Lang), "ko,ja,en");
    [IsExpressionProperty]
    public string Lang
    {
        get => _lang;
        set => ConfigHelper.SetValue(ref _lang, value, propertyChangedAction: () => OnPropertyChanged());
    }

    #endregion Search Expression

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
        _controller = new((s) => Error = s);

        TweetsCollectionView = new ListCollectionView(_controller.Tweets);
        TweetsCollectionView.SortDescriptions.Add(new SortDescription("Tweet.CreatedAt", ListSortDirection.Descending));
        RulesCollectionView = new ListCollectionView(_controller.Rules);
        RateLimitsCollectionView = new ListCollectionView(_controller.RateLimits);
        DeleteRuleCommand = new DelegateCommand<StreamInfo>(DeleteRuleAction);
        GetRecentCommand = new DelegateCommand(GetRecentAction);

        PropertyChanged += OnPropertyChanged;

        RefreshRule();
    }

    private void GetRecentAction()
    {
        Error = String.Empty;
        _controller.GetRecentTweets(BuildExpression());
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