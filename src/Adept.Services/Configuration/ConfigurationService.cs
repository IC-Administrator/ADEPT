using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Adept.Services.Configuration
{
    /// <summary>
    /// Service for accessing application configuration values
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly ILogger<ConfigurationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public ConfigurationService(IDatabaseContext databaseContext, ILogger<ConfigurationService> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;

            // Initialize default configuration values
            InitializeDefaultConfigurationAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes default configuration values
        /// </summary>
        private async Task InitializeDefaultConfigurationAsync()
        {
            try
            {
                // Create the configuration table if it doesn't exist
                await _databaseContext.ExecuteNonQueryAsync(
                    "CREATE TABLE IF NOT EXISTS Configuration (key TEXT PRIMARY KEY, value TEXT)");

                // Set default voice providers
                await SetDefaultConfigurationValueAsync("wake_word_detector", "vosk");
                await SetDefaultConfigurationValueAsync("speech_to_text_provider", "whisper");
                await SetDefaultConfigurationValueAsync("text_to_speech_provider", "fishaudio");

                // Set default Fish Audio TTS settings
                await SetDefaultConfigurationValueAsync("fish_audio_voice_id", "default");
                await SetDefaultConfigurationValueAsync("fish_audio_model", "speech-1.6");
                await SetDefaultConfigurationValueAsync("fish_audio_speed", "1.0");
                await SetDefaultConfigurationValueAsync("fish_audio_volume", "0.0");
                await SetDefaultConfigurationValueAsync("fish_audio_clarity", "0.0");
                await SetDefaultConfigurationValueAsync("fish_audio_emotion", "0.0");
                await SetDefaultConfigurationValueAsync("fish_audio_use_disk_cache", "true");
                await SetDefaultConfigurationValueAsync("fish_audio_max_cache_items", "100");
                await SetDefaultConfigurationValueAsync("fish_audio_max_retries", "3");
                await SetDefaultConfigurationValueAsync("fish_audio_reconnect_delay_ms", "1000");

                _logger.LogInformation("Default configuration values initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default configuration values");
            }
        }

        /// <summary>
        /// Sets a configuration value if it doesn't already exist
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The configuration value</param>
        private async Task SetDefaultConfigurationValueAsync(string key, string value)
        {
            try
            {
                // Check if the key already exists
                var existingValue = await GetConfigurationValueAsync(key);

                if (string.IsNullOrEmpty(existingValue))
                {
                    // Set the default value
                    await SetConfigurationValueAsync(key, value);
                    _logger.LogInformation("Set default configuration value for {Key}: {Value}", key, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default configuration value for key {Key}", key);
            }
        }

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>The configuration value or default if not found</returns>
        public async Task<string> GetConfigurationValueAsync(string key, string? defaultValue = null)
        {
            try
            {
                var value = await _databaseContext.ExecuteScalarAsync<string>(
                    "SELECT value FROM Configuration WHERE key = @Key",
                    new { Key = key });

                return value ?? defaultValue ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration value for key {Key}", key);
                return defaultValue ?? string.Empty;
            }
        }

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        public async Task SetConfigurationValueAsync(string key, string value)
        {
            try
            {
                await _databaseContext.ExecuteNonQueryAsync(
                    @"INSERT INTO Configuration (key, value, last_modified)
                      VALUES (@Key, @Value, CURRENT_TIMESTAMP)
                      ON CONFLICT(key) DO UPDATE SET
                      value = @Value,
                      last_modified = CURRENT_TIMESTAMP",
                    new { Key = key, Value = value });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting configuration value for key {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Gets a typed configuration value
        /// </summary>
        /// <typeparam name="T">The type to convert the value to</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">Default value if key is not found or conversion fails</param>
        /// <returns>The typed configuration value or default if not found or conversion fails</returns>
        public async Task<T> GetConfigurationValueAsync<T>(string key, T defaultValue)
        {
            try
            {
                var value = await GetConfigurationValueAsync(key);
                if (string.IsNullOrEmpty(value))
                {
                    return defaultValue;
                }

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value;
                }
                else if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                {
                    if (int.TryParse(value, out var intValue))
                    {
                        return (T)(object)intValue;
                    }
                }
                else if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                {
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return (T)(object)boolValue;
                    }
                }
                else if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
                {
                    if (DateTime.TryParse(value, out var dateValue))
                    {
                        return (T)(object)dateValue;
                    }
                }
                else
                {
                    try
                    {
                        return JsonSerializer.Deserialize<T>(value) ?? defaultValue;
                    }
                    catch
                    {
                        // Ignore JSON deserialization errors
                    }
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting typed configuration value for key {Key}", key);
                return defaultValue;
            }
        }

        /// <summary>
        /// Sets a typed configuration value
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        public async Task SetConfigurationValueAsync<T>(string key, T value)
        {
            try
            {
                string stringValue;

                if (value == null)
                {
                    stringValue = string.Empty;
                }
                else if (typeof(T) == typeof(string))
                {
                    stringValue = value.ToString() ?? string.Empty;
                }
                else if (typeof(T).IsPrimitive || typeof(T) == typeof(DateTime))
                {
                    stringValue = value.ToString() ?? string.Empty;
                }
                else
                {
                    stringValue = JsonSerializer.Serialize(value);
                }

                await SetConfigurationValueAsync(key, stringValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting typed configuration value for key {Key}", key);
                throw;
            }
        }
    }
}
