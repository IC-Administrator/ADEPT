namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Service for accessing application configuration values
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>The configuration value or default if not found</returns>
        Task<string> GetConfigurationValueAsync(string key, string? defaultValue = null);

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        Task SetConfigurationValueAsync(string key, string value);

        /// <summary>
        /// Gets a typed configuration value
        /// </summary>
        /// <typeparam name="T">The type to convert the value to</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">Default value if key is not found or conversion fails</param>
        /// <returns>The typed configuration value or default if not found or conversion fails</returns>
        Task<T> GetConfigurationValueAsync<T>(string key, T defaultValue);

        /// <summary>
        /// Sets a typed configuration value
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        Task SetConfigurationValueAsync<T>(string key, T value);
    }
}
