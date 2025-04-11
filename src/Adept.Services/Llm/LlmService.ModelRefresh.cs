using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Adept.Services.Llm
{
    /// <summary>
    /// Partial class for LlmService containing model refresh functionality
    /// </summary>
    public partial class LlmService
    {
        private Timer? _modelRefreshTimer;
        private readonly TimeSpan _modelRefreshInterval = TimeSpan.FromHours(24);
        private readonly SemaphoreSlim _modelRefreshLock = new(1, 1);
        private bool _isRefreshingModels;

        /// <summary>
        /// Starts the periodic model refresh timer
        /// </summary>
        private void StartModelRefreshTimer()
        {
            _modelRefreshTimer = new Timer(RefreshModelsCallback, null, TimeSpan.Zero, _modelRefreshInterval);
            _logger.LogInformation("Model refresh timer started with interval of {Interval} hours", _modelRefreshInterval.TotalHours);
        }

        /// <summary>
        /// Callback for the model refresh timer
        /// </summary>
        /// <param name="state">Timer state (not used)</param>
        private void RefreshModelsCallback(object? state)
        {
            RefreshModelsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Refreshes the available models for all providers with valid API keys
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RefreshModelsAsync()
        {
            // Prevent concurrent refreshes
            if (_isRefreshingModels)
            {
                _logger.LogInformation("Model refresh already in progress, skipping");
                return;
            }

            await _modelRefreshLock.WaitAsync();
            try
            {
                _isRefreshingModels = true;
                _logger.LogInformation("Refreshing models for all providers with valid API keys");

                foreach (var provider in _providers)
                {
                    if (provider.HasValidApiKey)
                    {
                        try
                        {
                            _logger.LogInformation("Refreshing models for provider: {ProviderName}", provider.ProviderName);
                            var models = await provider.FetchAvailableModelsAsync();
                            _logger.LogInformation("Refreshed {Count} models for provider: {ProviderName}", models.Count(), provider.ProviderName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error refreshing models for provider: {ProviderName}", provider.ProviderName);
                        }
                    }
                }

                _logger.LogInformation("Model refresh completed");
            }
            finally
            {
                _isRefreshingModels = false;
                _modelRefreshLock.Release();
            }
        }

        /// <summary>
        /// Refreshes the available models for a specific provider
        /// </summary>
        /// <param name="providerName">The name of the provider to refresh</param>
        /// <returns>True if the provider was found and refreshed, false otherwise</returns>
        public async Task<bool> RefreshModelsForProviderAsync(string providerName)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                _logger.LogWarning("Provider not found for model refresh: {ProviderName}", providerName);
                return false;
            }

            if (!provider.HasValidApiKey)
            {
                _logger.LogWarning("Cannot refresh models for provider without valid API key: {ProviderName}", providerName);
                return false;
            }

            try
            {
                _logger.LogInformation("Refreshing models for provider: {ProviderName}", provider.ProviderName);
                var models = await provider.FetchAvailableModelsAsync();
                _logger.LogInformation("Refreshed {Count} models for provider: {ProviderName}", models.Count(), provider.ProviderName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing models for provider: {ProviderName}", provider.ProviderName);
                return false;
            }
        }

        /// <summary>
        /// Stops the model refresh timer
        /// </summary>
        public void StopModelRefreshTimer()
        {
            _modelRefreshTimer?.Dispose();
            _modelRefreshTimer = null;
            _logger.LogInformation("Model refresh timer stopped");
        }
    }
}
