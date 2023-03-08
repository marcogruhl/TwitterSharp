using static System.Formats.Asn1.AsnWriter;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;

namespace TwitterSharp
{
    public class OAuthGrant
    {
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string access_token { get; set; }
        public string scope { get; set; }
        public string error { get; set; }
        public string error_description { get; set; }
    }

    public class OAuthHelper
    {
        private const string AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize";
        private const string TokenEndpoint = "https://api.twitter.com/2/oauth2/token";

        /// <summary>
        /// Does the user OAuth2 authentication described at <see cref="https://developer.twitter.com/en/docs/authentication/oauth-2-0/authorization-code"/>
        /// </summary>
        /// <param name="clientId">generate this in the Developer Portal under Projects & Apps - [your project] - [your app] - User authentication settings - Set up</param>
        /// <param name="clientSecret">generate this in the Developer Portal under Projects & Apps - [your project] - [your app] - User authentication settings - Set up</param>
        /// <param name="scopes">All the scopes needed for your endpoints <see cref="https://developer.twitter.com/en/docs/authentication/guides/v2-authentication-mapping"/> <seealso cref="https://developer.twitter.com/en/docs/authentication/oauth-2-0/authorization-code"/></param>
        /// <param name="redirectUri"> Your callback URL. This value must correspond to one of the Callback URLs defined in your App’s settings. For OAuth 2.0, you will need to have exact match validation for your callback URL.</param>
        /// <param name="successUri">URI the user gets redirect after successful authentication</param>
        /// <returns></returns>
        public static async Task<string> Auth(string clientId, string clientSecret, List<string> scopes, string? redirectUri = null, string? successUri = null)
        {
            // with the help of https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
            
            // Generates state and PKCE values.
            string state = RandomDataBase64Url(32);
            string codeVerifier = RandomDataBase64Url(32);
            string codeChallenge = Base64UrlencodeNoPadding(Sha256(codeVerifier));
            // string code_challenge = "challenge";
            const string codeChallengeMethod = "s256";

            // Creates a redirect URI using an available port on the loopback address.
            redirectUri ??= $"http://{IPAddress.Loopback}:{GetRandomUnusedPort()}/";
            Debug.WriteLine("redirect URI: " + redirectUri);

            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectUri);
            Debug.WriteLine("Listening..");
            http.Start();

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format(
                "{0}?response_type=code&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}&scope={6}",
                AuthorizationEndpoint,
                Uri.EscapeDataString(redirectUri),
                clientId,
                state,
                codeChallenge,
                codeChallengeMethod,
                string.Join("%20", scopes));

            // Opens request in the browser.
            try
            {
                Process.Start(authorizationRequest);
            }
            catch(Exception ex)
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
                    throw new Exception("Could not start browser", ex);
                }
            }

            // Waits for the OAuth authorization response.
            var context = http.GetContextAsync().Result;

            // Brings this app back to the foreground.
            // this.Activate();

            // Sends an HTTP response to the browser.
            var response = context.Response;
            string responseString =
                "<html>" +
                (successUri != null ? $"<head><meta http-equiv='refresh' content='10;url={successUri}'></head>" : "") +
                "<body>Please return to the app.</body>" +
                "</html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
#pragma warning disable CS4014
            Task.Run(() => 
#pragma warning restore CS4014
                responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith(_ =>
                {
                    responseOutput.Close();
                    http.Stop();
                    Console.WriteLine("HTTP server stopped.");
                })
             );

            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                throw new Exception($"OAuth authorization error: {context.Request.QueryString.Get("error")}.");
            }

            if (context.Request.QueryString.Get("code") == null || context.Request.QueryString.Get("state") == null)
            {
                throw new Exception($"Malformed authorization response. {context.Request.QueryString}");
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incomingState = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incomingState != state)
            {
                Debug.WriteLine($"Received request with invalid state ({incomingState})");
                throw new Exception($"Received request with invalid state ({incomingState})");
            }

            Debug.WriteLine("Authorization code: " + code);

            // Starts the code exchange at the Token Endpoint.
            return await GetToken(code, codeVerifier, redirectUri, clientId, clientSecret);
        }

        // with help from https://github.com/jamescarter-le
        private static async Task<string> GetToken(string? code, string codeVerifier, string redirectUri, string clientId, string clientSecret)
        {
            try
            {
                var httpClient = new HttpClient();
                var ourAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(clientId + ":" + clientSecret));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + ourAuth);

                var nvc = new List<KeyValuePair<string, string>>
                {
                    new("code", code),
                    new("grant_type", "authorization_code"),
                    new("client_id", clientId),
                    new("redirect_uri", redirectUri),
                    new("code_verifier", codeVerifier)
                };

                var response = await httpClient.PostAsync(TokenEndpoint, new FormUrlEncodedContent(nvc));
                var responseString = await response.Content.ReadAsStringAsync();

                // // Using the above auth to read from protected.
                OAuthGrant grant = JsonSerializer.Deserialize<OAuthGrant>(responseString);
                if (!string.IsNullOrEmpty(grant.error))
                {
                    throw new Exception($"Error on token request. {grant.error}: {grant.error_description}");
                }

                return grant.access_token;
            }
            catch (Exception ex)
            {
                throw new Exception("Error on token request", ex);
            }
        }

        // ref http://stackoverflow.com/a/3978040
        private static int GetRandomUnusedPort()
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
        private static string RandomDataBase64Url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return Base64UrlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        private static byte[] Sha256(string inputStirng)
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
        private static string Base64UrlencodeNoPadding(byte[] buffer)
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
}
