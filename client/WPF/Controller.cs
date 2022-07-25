using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TwitterSharp.Client;
using TwitterSharp.Request.AdvancedSearch;
using TwitterSharp.Request.Option;
using TwitterSharp.Response;
using TwitterSharp.Response.RStream;
using TwitterSharp.Response.RTweet;
using TwitterSharp.WpfClient.ViewModels;

namespace TwitterSharp.WpfClient;

internal class Controller : INotifyPropertyChanged, IAsyncDisposable
{
    private TwitterClient _client { get; set; }
    private Task? _tweetStream { get; set; }
    private CancellationTokenSource _cancellationTokenSource = new ();
    public readonly ObservableCollection<StreamInfo> Rules = new();
    internal readonly ObservableCollection<Tweet> Tweets = new();
    public readonly ObservableCollection<RateLimitViewModel> RateLimits = new ();

    private bool? _isConnected { get; set; }
    
    public bool? IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            OnPropertyChanged();
        }
    }


    private TweetOption[] _tweetOptions = new[]
    {
        TweetOption.Created_At,
        TweetOption.In_Reply_To_User_Id,
        TweetOption.Referenced_Tweets,
        TweetOption.Reply_Settings,
        TweetOption.Lang,
        TweetOption.Public_Metrics
    };

    private UserOption[] _userOptions = new[]
    {
        UserOption.Verified,
        UserOption.Created_At,
        UserOption.Entities,
        UserOption.Public_Metrics,
        UserOption.Protected
    };

    public Controller()
    {
        _client = new TwitterClient(Environment.GetEnvironmentVariable("TWITTER_TOKEN"));
        _client.RateLimitChanged += UpdateRateLimits;

        Application.Current.Exit += async (_, _) => await DisposeAsync();
        
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await RefreshRules();
        await Connect();
    }

    public async Task Connect()
    {
        try
        {
            IsConnected = false;

            if (!Rules.Any())
            {
                // Subscriptions.Add("Every3Minutes");
                // await RefreshSubscriptions();
            }

            _cancellationTokenSource = new();

            // NextTweetStreamAsync will continue to run in background
            _tweetStream = Task.Run(async () =>
            {
                // Take in parameter a callback called for each new tweet
                // Since we want to get the basic info of the tweet author, we add an empty array of UserOption
                await _client.NextTweetStreamAsync((tweet) =>
                {
                    if(tweet == null)
                        return;

                    Debug.WriteLine($"From {tweet.Author.Name}: {tweet.Text}");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Tweets.Add(tweet);
                        Debug.WriteLine($"From {tweet.Author.Name}: {tweet.Text} (Rules: {string.Join(',', tweet.MatchingRules.Select(x => x.Tag))})");
                    });
                        
                }, new TweetSearchOptions
                {
                    TweetOptions = _tweetOptions,
                    UserOptions = _userOptions,

                });
            }, _cancellationTokenSource.Token).ContinueWith(t =>
            {
                if (t.Status == TaskStatus.Faulted)
                {
                    IsConnected = null;
                    // _logger.Twitter(t.Exception.GetType() + " " + t.Exception.Message, t.Exception);
                }

                _cancellationTokenSource.Dispose();
            });

            IsConnected = true;

            // _client.C

        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            IsConnected = null;
        }
    }

    private async Task RefreshRules()
    {
        Rules.Clear();

        var rules = await _client.GetInfoTweetStreamAsync();

        foreach (var rule in rules)
        {
            Rules.Add(rule);
        }

        UpdateRateLimits(new RateLimitViewModel("RulesPerStream", 25 - Rules.Count, 25));
    }

    private void UpdateRateLimits(object? sender, RateLimit rateLimit)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var viewModel = RateLimits.FirstOrDefault(x => x.Name == rateLimit.Endpoint.ToString());

            if (viewModel == null)
            {
                RateLimits.Add(new RateLimitViewModel(rateLimit));
            }
            else
            {
                viewModel.Value = rateLimit.Remaining;
                viewModel.Reset = rateLimit.Reset;
            }
        });
    }

    private void UpdateRateLimits(RateLimitViewModel rateLimit)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var viewModel = RateLimits.FirstOrDefault(x => x.Name == rateLimit.Name);

            if (viewModel == null)
            {
                RateLimits.Add(rateLimit);
            }
            else
            {
                viewModel.Value = rateLimit.Value;
                viewModel.Reset = rateLimit.Reset;
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async void Disconnect()
    {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        IsConnected = false;

        _cancellationTokenSource.Cancel();

        _client.RateLimitChanged -= UpdateRateLimits;
        _client.Dispose();

        if (_tweetStream != null)
        {
            await _tweetStream.WaitAsync(CancellationToken.None); // could take up to 20 sesonds (keep alive signal)
            _tweetStream.Dispose();
        }

        IsConnected = null;
    }
}