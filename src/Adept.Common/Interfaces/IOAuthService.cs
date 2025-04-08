namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Interface for OAuth authentication service
    /// </summary>
    public interface IOAuthService
    {
        /// <summary>
        /// Starts the OAuth authentication process
        /// </summary>
        /// <returns>The OAuth token</returns>
        Task<OAuthToken> AuthenticateAsync();

        /// <summary>
        /// Gets a valid OAuth token
        /// </summary>
        /// <returns>The OAuth token</returns>
        Task<OAuthToken> GetValidTokenAsync();

        /// <summary>
        /// Refreshes an OAuth token
        /// </summary>
        /// <param name="refreshToken">The refresh token</param>
        /// <returns>The new OAuth token</returns>
        Task<OAuthToken> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Revokes an OAuth token
        /// </summary>
        /// <param name="token">The token to revoke</param>
        /// <returns>True if the token was revoked, false otherwise</returns>
        Task<bool> RevokeTokenAsync(string token);

        /// <summary>
        /// Checks if the service is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        Task<bool> IsAuthenticatedAsync();
    }

    /// <summary>
    /// Represents an OAuth token
    /// </summary>
    public class OAuthToken
    {
        /// <summary>
        /// The access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// The refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// The token type
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// The expiry time
        /// </summary>
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// The scopes
        /// </summary>
        public string Scope { get; set; } = string.Empty;
    }
}
