using Adept.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Adept.Services.OAuth
{
    /// <summary>
    /// Google OAuth service implementation
    /// </summary>
    public class GoogleOAuthService : IOAuthService
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleOAuthService> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly string[] _scopes;
        private readonly int _callbackPort;
        private HttpListener? _httpListener;
        private TaskCompletionSource<string>? _authCodeTaskSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleOAuthService"/> class
        /// </summary>
        /// <param name="secureStorage">The secure storage service</param>
        /// <param name="httpClient">The HTTP client</param>
        /// <param name="logger">The logger</param>
        /// <param name="configuration">The configuration</param>
        public GoogleOAuthService(
            ISecureStorageService secureStorage,
            HttpClient httpClient,
            ILogger<GoogleOAuthService> logger,
            IConfiguration configuration)
        {
            _secureStorage = secureStorage;
            _httpClient = httpClient;
            _logger = logger;
            
            // Get configuration values
            _clientId = configuration["OAuth:Google:ClientId"] ?? string.Empty;
            _clientSecret = configuration["OAuth:Google:ClientSecret"] ?? string.Empty;
            _callbackPort = int.Parse(configuration["OAuth:Google:CallbackPort"] ?? "8080");
            _redirectUri = $"http://localhost:{_callbackPort}";
            _scopes = new[] { "https://www.googleapis.com/auth/calendar" };
        }

        /// <summary>
        /// Starts the OAuth authentication process
        /// </summary>
        /// <returns>The OAuth token</returns>
        public async Task<OAuthToken> AuthenticateAsync()
        {
            try
            {
                // Check if we have client credentials
                if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
                {
                    _logger.LogError("Google OAuth client credentials not configured");
                    throw new InvalidOperationException("Google OAuth client credentials not configured");
                }

                // Start the HTTP listener for the callback
                _authCodeTaskSource = new TaskCompletionSource<string>();
                await StartHttpListenerAsync();

                // Generate the authorization URL
                var authUrl = GenerateAuthorizationUrl();

                // Open the browser for user authentication
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                _logger.LogInformation("Waiting for OAuth callback...");

                // Wait for the authorization code
                var authCode = await _authCodeTaskSource.Task;

                // Stop the HTTP listener
                StopHttpListener();

                if (string.IsNullOrEmpty(authCode))
                {
                    _logger.LogError("Failed to get authorization code");
                    throw new InvalidOperationException("Failed to get authorization code");
                }

                _logger.LogInformation("Authorization code received, exchanging for tokens...");

                // Exchange the authorization code for tokens
                var token = await ExchangeCodeForTokensAsync(authCode);

                // Store the tokens
                await StoreTokensAsync(token);

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google OAuth authentication");
                StopHttpListener();
                throw;
            }
        }

        /// <summary>
        /// Gets a valid OAuth token
        /// </summary>
        /// <returns>The OAuth token</returns>
        public async Task<OAuthToken> GetValidTokenAsync()
        {
            try
            {
                // Get the stored tokens
                var accessToken = await _secureStorage.RetrieveSecureValueAsync("google_access_token");
                var refreshToken = await _secureStorage.RetrieveSecureValueAsync("google_refresh_token");
                var expiryTimeStr = await _secureStorage.RetrieveSecureValueAsync("google_token_expiry");

                // If we don't have tokens, authenticate
                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogInformation("No stored tokens found, starting authentication");
                    return await AuthenticateAsync();
                }

                // Check if the token is expired
                if (DateTime.TryParse(expiryTimeStr, out var expiryTime) && expiryTime <= DateTime.UtcNow.AddMinutes(5))
                {
                    _logger.LogInformation("Access token expired, refreshing");
                    return await RefreshTokenAsync(refreshToken);
                }

                // Return the valid token
                return new OAuthToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiryTime = expiryTime,
                    TokenType = "Bearer"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting valid OAuth token");
                throw;
            }
        }

        /// <summary>
        /// Refreshes an OAuth token
        /// </summary>
        /// <param name="refreshToken">The refresh token</param>
        /// <returns>The new OAuth token</returns>
        public async Task<OAuthToken> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // Check if we have client credentials
                if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
                {
                    _logger.LogError("Google OAuth client credentials not configured");
                    throw new InvalidOperationException("Google OAuth client credentials not configured");
                }

                // Prepare the token request
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _clientId,
                    ["client_secret"] = _clientSecret,
                    ["refresh_token"] = refreshToken,
                    ["grant_type"] = "refresh_token"
                });

                // Send the request
                var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Create the token
                var token = new OAuthToken
                {
                    AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty,
                    RefreshToken = refreshToken, // Refresh token doesn't change
                    TokenType = tokenResponse.GetProperty("token_type").GetString() ?? "Bearer",
                    ExpiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.GetProperty("expires_in").GetInt32())
                };

                if (tokenResponse.TryGetProperty("scope", out var scopeElement))
                {
                    token.Scope = scopeElement.GetString() ?? string.Empty;
                }

                // Store the new access token
                await _secureStorage.StoreSecureValueAsync("google_access_token", token.AccessToken);
                await _secureStorage.StoreSecureValueAsync("google_token_expiry", token.ExpiryTime.ToString("o"));

                _logger.LogInformation("OAuth token refreshed successfully");
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing OAuth token");
                throw;
            }
        }

        /// <summary>
        /// Revokes an OAuth token
        /// </summary>
        /// <param name="token">The token to revoke</param>
        /// <returns>True if the token was revoked, false otherwise</returns>
        public async Task<bool> RevokeTokenAsync(string token)
        {
            try
            {
                // Prepare the revoke request
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["token"] = token
                });

                // Send the request
                var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/revoke", content);
                
                // Clear the stored tokens
                await _secureStorage.RemoveSecureValueAsync("google_access_token");
                await _secureStorage.RemoveSecureValueAsync("google_refresh_token");
                await _secureStorage.RemoveSecureValueAsync("google_token_expiry");

                _logger.LogInformation("OAuth token revoked successfully");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking OAuth token");
                return false;
            }
        }

        /// <summary>
        /// Checks if the service is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var refreshToken = await _secureStorage.RetrieveSecureValueAsync("google_refresh_token");
                return !string.IsNullOrEmpty(refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication status");
                return false;
            }
        }

        /// <summary>
        /// Generates the authorization URL
        /// </summary>
        /// <returns>The authorization URL</returns>
        private string GenerateAuthorizationUrl()
        {
            var scopeString = string.Join(" ", _scopes);
            return $"https://accounts.google.com/o/oauth2/auth" +
                $"?client_id={Uri.EscapeDataString(_clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(scopeString)}" +
                $"&access_type=offline" +
                $"&prompt=consent";
        }

        /// <summary>
        /// Exchanges an authorization code for tokens
        /// </summary>
        /// <param name="authCode">The authorization code</param>
        /// <returns>The OAuth token</returns>
        private async Task<OAuthToken> ExchangeCodeForTokensAsync(string authCode)
        {
            // Prepare the token request
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = authCode,
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["redirect_uri"] = _redirectUri,
                ["grant_type"] = "authorization_code"
            });

            // Send the request
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            response.EnsureSuccessStatusCode();

            // Parse the response
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Create the token
            var token = new OAuthToken
            {
                AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty,
                RefreshToken = tokenResponse.GetProperty("refresh_token").GetString() ?? string.Empty,
                TokenType = tokenResponse.GetProperty("token_type").GetString() ?? "Bearer",
                ExpiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.GetProperty("expires_in").GetInt32())
            };

            if (tokenResponse.TryGetProperty("scope", out var scopeElement))
            {
                token.Scope = scopeElement.GetString() ?? string.Empty;
            }

            return token;
        }

        /// <summary>
        /// Stores the OAuth tokens
        /// </summary>
        /// <param name="token">The OAuth token</param>
        private async Task StoreTokensAsync(OAuthToken token)
        {
            await _secureStorage.StoreSecureValueAsync("google_access_token", token.AccessToken);
            await _secureStorage.StoreSecureValueAsync("google_refresh_token", token.RefreshToken);
            await _secureStorage.StoreSecureValueAsync("google_token_expiry", token.ExpiryTime.ToString("o"));
            _logger.LogInformation("OAuth tokens stored in secure storage");
        }

        /// <summary>
        /// Starts the HTTP listener for the OAuth callback
        /// </summary>
        private async Task StartHttpListenerAsync()
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"{_redirectUri}/");
                _httpListener.Start();

                _logger.LogInformation("HTTP listener started on {RedirectUri}", _redirectUri);

                // Start listening for the callback asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Wait for the callback
                        var context = await _httpListener.GetContextAsync();
                        var request = context.Request;
                        var response = context.Response;

                        // Get the authorization code from the query string
                        var code = request.QueryString["code"];

                        // Send a response to the browser
                        var responseString = "<html><head><title>Authentication Successful</title></head>" +
                            "<body><h1>Authentication Successful</h1>" +
                            "<p>You can now close this window and return to the application.</p></body></html>";
                        var buffer = Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "text/html";
                        var output = response.OutputStream;
                        await output.WriteAsync(buffer, 0, buffer.Length);
                        output.Close();

                        // Complete the task with the authorization code
                        _authCodeTaskSource?.TrySetResult(code ?? string.Empty);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in HTTP listener callback");
                        _authCodeTaskSource?.TrySetException(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting HTTP listener");
                throw;
            }
        }

        /// <summary>
        /// Stops the HTTP listener
        /// </summary>
        private void StopHttpListener()
        {
            try
            {
                if (_httpListener != null && _httpListener.IsListening)
                {
                    _httpListener.Stop();
                    _logger.LogInformation("HTTP listener stopped");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping HTTP listener");
            }
        }
    }
}
