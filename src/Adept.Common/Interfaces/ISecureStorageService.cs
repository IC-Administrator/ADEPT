namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Service for securely storing sensitive information like API keys
    /// </summary>
    public interface ISecureStorageService
    {
        /// <summary>
        /// Stores a value securely
        /// </summary>
        /// <param name="key">The key to identify the value</param>
        /// <param name="value">The value to store securely</param>
        Task StoreSecureValueAsync(string key, string value);

        /// <summary>
        /// Retrieves a securely stored value
        /// </summary>
        /// <param name="key">The key identifying the value</param>
        /// <returns>The decrypted value, or null if not found</returns>
        Task<string?> RetrieveSecureValueAsync(string key);

        /// <summary>
        /// Removes a securely stored value
        /// </summary>
        /// <param name="key">The key identifying the value to remove</param>
        Task RemoveSecureValueAsync(string key);

        /// <summary>
        /// Checks if a key exists in secure storage
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        Task<bool> KeyExistsAsync(string key);
    }
}
