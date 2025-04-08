using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Adept.Services.Security
{
    /// <summary>
    /// Service for securely storing sensitive information like API keys
    /// </summary>
    public class SecureStorageService : ISecureStorageService
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly ICryptographyService _cryptographyService;
        private readonly ILogger<SecureStorageService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureStorageService"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="cryptographyService">The cryptography service</param>
        /// <param name="logger">The logger</param>
        public SecureStorageService(
            IDatabaseContext databaseContext,
            ICryptographyService cryptographyService,
            ILogger<SecureStorageService> logger)
        {
            _databaseContext = databaseContext;
            _cryptographyService = cryptographyService;
            _logger = logger;
        }

        /// <summary>
        /// Stores a value securely
        /// </summary>
        /// <param name="key">The key to identify the value</param>
        /// <param name="value">The value to store securely</param>
        public async Task StoreSecureValueAsync(string key, string value)
        {
            try
            {
                var (encryptedValue, iv) = _cryptographyService.Encrypt(value);

                await _databaseContext.ExecuteNonQueryAsync(
                    @"INSERT INTO SecureStorage (key, encrypted_value, iv, last_modified) 
                      VALUES (@Key, @EncryptedValue, @Iv, CURRENT_TIMESTAMP)
                      ON CONFLICT(key) DO UPDATE SET 
                      encrypted_value = @EncryptedValue, 
                      iv = @Iv, 
                      last_modified = CURRENT_TIMESTAMP",
                    new { Key = key, EncryptedValue = encryptedValue, Iv = iv });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing secure value for key {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a securely stored value
        /// </summary>
        /// <param name="key">The key identifying the value</param>
        /// <returns>The decrypted value, or null if not found</returns>
        public async Task<string?> RetrieveSecureValueAsync(string key)
        {
            try
            {
                var result = await _databaseContext.QuerySingleOrDefaultAsync<SecureStorageEntry>(
                    "SELECT encrypted_value, iv FROM SecureStorage WHERE key = @Key",
                    new { Key = key });

                if (result == null || result.encrypted_value == null || result.iv == null)
                {
                    return null;
                }

                return _cryptographyService.Decrypt(result.encrypted_value, result.iv);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secure value for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Removes a securely stored value
        /// </summary>
        /// <param name="key">The key identifying the value to remove</param>
        public async Task RemoveSecureValueAsync(string key)
        {
            try
            {
                await _databaseContext.ExecuteNonQueryAsync(
                    "DELETE FROM SecureStorage WHERE key = @Key",
                    new { Key = key });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing secure value for key {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Checks if a key exists in secure storage
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public async Task<bool> KeyExistsAsync(string key)
        {
            try
            {
                var count = await _databaseContext.ExecuteScalarAsync<long>(
                    "SELECT COUNT(*) FROM SecureStorage WHERE key = @Key",
                    new { Key = key });

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key {Key} exists in secure storage", key);
                return false;
            }
        }

        /// <summary>
        /// Class representing a secure storage entry
        /// </summary>
        private class SecureStorageEntry
        {
            /// <summary>
            /// The encrypted value
            /// </summary>
            public byte[]? encrypted_value { get; set; }

            /// <summary>
            /// The initialization vector
            /// </summary>
            public byte[]? iv { get; set; }
        }
    }
}
