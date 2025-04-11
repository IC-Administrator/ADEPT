namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Extension of ILlmService interface for model refresh functionality
    /// </summary>
    public partial interface ILlmService
    {
        /// <summary>
        /// Refreshes the available models for all providers with valid API keys
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RefreshModelsAsync();

        /// <summary>
        /// Refreshes the available models for a specific provider
        /// </summary>
        /// <param name="providerName">The name of the provider to refresh</param>
        /// <returns>True if the provider was found and refreshed, false otherwise</returns>
        Task<bool> RefreshModelsForProviderAsync(string providerName);
    }
}
