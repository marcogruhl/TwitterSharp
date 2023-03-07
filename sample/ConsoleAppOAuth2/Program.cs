using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using TwitterSharp.Client;

namespace ConsoleAppOAuth2;

public class OAuthGrant
{
    public string token_type { get; set; }
    public int expires_in { get; set; }
    public string access_token { get; set; }
    public string scope { get; set; }
}

internal class Program
{
    static void Main(string[] args)
    {
        var token = Auth().Result;
        var client = new TwitterClient(token);
        var answer = client.GetMeAsync().Result;
        var timeline = client.GetTimelineForUserAsync(Environment.GetEnvironmentVariable("TWITTER_OWN_ID")).Result;
    }


private static readonly string clientID = Environment.GetEnvironmentVariable("TWITTER_CLIENT_ID");
private static readonly string clientSecret = Environment.GetEnvironmentVariable("TWITTER_CLIENT_SECRET");
const string authorizationEndpoint = "https://twitter.com/i/oauth2/authorize";
const string tokenEndpoint = "https://api.twitter.com/2/oauth2/token";
const string userInfoEndpoint = "https://api.twitter.com/2/users/me";
private static readonly List<string> scopes = new List<string>
    {
        "tweet.read",
        "users.read",
        // "follows.read",
        // "like.read",
        // "list.read",
        // "offline.access"
    };


// https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
private static async Task<string> Auth()
{
    // Generates state and PKCE values.
    // string state = randomDataBase64url(32);
    string state = "state";
    string code_verifier = randomDataBase64url(32);
    string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));
    // string code_challenge = "challenge";
    const string code_challenge_method = "s256";

    // Creates a redirect URI using an available port on the loopback address.
    // string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, GetRandomUnusedPort());
    string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, 5057);
    Debug.WriteLine("redirect URI: " + redirectURI);

    // Creates an HttpListener to listen for requests on that redirect URI.
    var http = new HttpListener();
    http.Prefixes.Add(redirectURI);
    Debug.WriteLine("Listening..");
    http.Start();

    // Creates the OAuth 2.0 authorization request.
    string authorizationRequest = string.Format(
        "{0}?response_type=code&scope={6}&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
        authorizationEndpoint,
        System.Uri.EscapeDataString(redirectURI),
        clientID,
        state,
        code_challenge,
        code_challenge_method,
        string.Join("%20", scopes));

    // Opens request in the browser.

    try
    {
        Process.Start(authorizationRequest);
    }
    catch
    {
        // hack because of this: https://github.com/dotnet/corefx/issues/10361
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            authorizationRequest = authorizationRequest.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {authorizationRequest}"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", authorizationRequest);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", authorizationRequest);
        }
        else
        {
            throw;
        }
    }
    // System.Diagnostics.Process.Start(authorizationRequest);

    // Waits for the OAuth authorization response.
    var context = http.GetContextAsync().Result;

    // Brings this app back to the foreground.
    // this.Activate();

    // Sends an HTTP response to the browser.
    var response = context.Response;
    string responseString =
        string.Format(
            "<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>");
    var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
    response.ContentLength64 = buffer.Length;
    var responseOutput = response.OutputStream;
    Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
    {
        responseOutput.Close();
        http.Stop();
        Console.WriteLine("HTTP server stopped.");
    });

    // Checks for errors.
    if (context.Request.QueryString.Get("error") != null)
    {
        Debug.WriteLine(String.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
        return String.Empty;
    }

    if (context.Request.QueryString.Get("code") == null
        || context.Request.QueryString.Get("state") == null)
    {
        Debug.WriteLine("Malformed authorization response. " + context.Request.QueryString);
        return String.Empty;
    }

    // extracts the code
    var code = context.Request.QueryString.Get("code");
    var incoming_state = context.Request.QueryString.Get("state");

    // Compares the receieved state to the expected value, to ensure that
    // this app made the request which resulted in authorization.
    if (incoming_state != state)
    {
        Debug.WriteLine(String.Format("Received request with invalid state ({0})", incoming_state));
        return String.Empty;
    }

    Debug.WriteLine("Authorization code: " + code);

    // Starts the code exchange at the Token Endpoint.

    return await getToken(code, code_verifier, redirectURI);

}

private static async Task<string> getToken(string? code, string codeVerifier, string redirectUri)
{
    try
    {

        var _httpClient = new HttpClient();
        var ourAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(clientID + ":" + clientSecret));
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + ourAuth);


        // await GetUsersSemaphore(userId).WaitAsync();
        //     
        // ........

        // string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, 5057);

        var nvc = new List<KeyValuePair<string, string>>
            {
                new("code", code),
                new("grant_type", "authorization_code"),
                new("client_id", clientID!),
                new("redirect_uri", redirectUri),
                new("code_verifier", codeVerifier)
            };

        // Creates an HttpListener to listen for requests on that redirect URI.
        // var http = new HttpListener();
        // http.Prefixes.Add(redirectURI);
        // Debug.WriteLine("Listening..");
        // http.Start();

        var response = await _httpClient.PostAsync("https://api.twitter.com/2/oauth2/token", new FormUrlEncodedContent(nvc));
        var responseString = await response.Content.ReadAsStringAsync();

        // // Using the above auth to read from protected.
        OAuthGrant grant = JsonSerializer.Deserialize<OAuthGrant>(responseString);
        // if (!string.IsNullOrEmpty(grant.Error))
        // {
        //     throw new TwitterAuthError(grant.Error);
        // }

        return grant.access_token;

        // return responseString;

        // user.TwitterRefreshToken = grant.RefreshToken;
        // user.TwitterAccessTokenExpiry = DateTime.UtcNow.AddSeconds(grant.ExpiresIn);
        //
        // var twitterClient = new TwitterClient(user.TwitterAccessToken);
        // var me = await twitterClient.GetMeAsync();
        // user.TwitterId = me.Id;
        // user.TwitterUsername = me.Username;
    }
    finally
    {
        // GetUsersSemaphore(userId).Release();
    }
}

static async Task<string> performCodeExchange(string code, string code_verifier, string redirectURI)
{
    Debug.WriteLine("Exchanging code for tokens...");

    // builds the  request
    string tokenRequestURI = tokenEndpoint;
    string tokenRequestBody = string.Format(
        "code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code",
        code,
        System.Uri.EscapeDataString(redirectURI),
        clientID,
        code_verifier,
        clientSecret
    );

    // sends the request
    HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(tokenRequestURI);
    tokenRequest.Method = "POST";
    tokenRequest.ContentType = "application/x-www-form-urlencoded";
    tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
    byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
    tokenRequest.ContentLength = _byteVersion.Length;
    Stream stream = tokenRequest.GetRequestStream();
    await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
    stream.Close();

    try
    {
        // gets the response
        WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
        using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
        {
            // reads response body
            string responseText = await reader.ReadToEndAsync();
            Debug.WriteLine(responseText);

            // converts to dictionary
            Dictionary<string, string> tokenEndpointDecoded = JsonSerializer.Deserialize<Dictionary<string, string>>(responseText);

            string access_token = tokenEndpointDecoded["access_token"];

            return access_token;
            // userinfoCall(access_token);
        }
    }
    catch (WebException ex)
    {
        if (ex.Status == WebExceptionStatus.ProtocolError)
        {
            var response = ex.Response as HttpWebResponse;
            if (response != null)
            {
                Debug.WriteLine("HTTP: " + response.StatusCode);
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    Debug.WriteLine(responseText);
                }
            }

        }
    }

    return null;
}


static async void userinfoCall(string access_token)
{
    Debug.WriteLine("Making API Call to Userinfo...");

    // builds the  request
    string userinfoRequestURI = "https://www.googleapis.com/oauth2/v3/userinfo";

    // sends the request
    HttpWebRequest userinfoRequest = (HttpWebRequest)WebRequest.Create(userinfoRequestURI);
    userinfoRequest.Method = "GET";
    userinfoRequest.Headers.Add(string.Format("Authorization: Bearer {0}", access_token));
    userinfoRequest.ContentType = "application/x-www-form-urlencoded";
    userinfoRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

    // gets the response
    WebResponse userinfoResponse = await userinfoRequest.GetResponseAsync();
    using (StreamReader userinfoResponseReader = new StreamReader(userinfoResponse.GetResponseStream()))
    {
        // reads response body
        string userinfoResponseText = await userinfoResponseReader.ReadToEndAsync();
        Debug.WriteLine(userinfoResponseText);
    }
}

// ref http://stackoverflow.com/a/3978040
public static int GetRandomUnusedPort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

    /// <summary>
    /// Returns URI-safe data with a given input length.
    /// </summary>
    /// <param name="length">Input length (nb. output will be longer)</param>
    /// <returns></returns>
public static string randomDataBase64url(uint length)
{
    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
    byte[] bytes = new byte[length];
    rng.GetBytes(bytes);
    return base64urlencodeNoPadding(bytes);
}

/// <summary>
/// Returns the SHA256 hash of the input string.
/// </summary>
/// <param name="inputStirng"></param>
/// <returns></returns>
public static byte[] sha256(string inputStirng)
{
    byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
    SHA256Managed sha256 = new SHA256Managed();
    return sha256.ComputeHash(bytes);
}

/// <summary>
/// Base64url no-padding encodes the given input buffer.
/// </summary>
/// <param name="buffer"></param>
/// <returns></returns>
public static string base64urlencodeNoPadding(byte[] buffer)
{
    string base64 = Convert.ToBase64String(buffer);

    // Converts base64 to base64url.
    base64 = base64.Replace("+", "-");
    base64 = base64.Replace("/", "_");
    // Strips padding.
    base64 = base64.Replace("=", "");

    return base64;
}
}