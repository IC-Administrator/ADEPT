using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adept.Core.Interfaces;
using Adept.Core.Models;
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
        private readonly TimeSpan _startupRefreshDelay = TimeSpan.FromMinutes(2);
        private readonly SemaphoreSlim _modelRefreshLock = new(1, 1);
        private bool _isRefreshingModels;

        /// <summary>
        /// Starts the periodic model refresh timer
        /// </summary>
        private void StartModelRefreshTimer()
        {
            // Start with a short delay to allow the application to initialize fully
            // Then refresh every 24 hours
            _modelRefreshTimer = new Timer(RefreshModelsCallback, null, _startupRefreshDelay, _modelRefreshInterval);
            _logger.LogInformation("Model refresh timer started with interval of {Interval} hours and initial delay of {Delay} minutes",
                _modelRefreshInterval.TotalHours, _startupRefreshDelay.TotalMinutes);
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

                            // Store the current model ID before refresh
                            var currentModelId = provider.CurrentModel.Id;

                            // Fetch the latest models
                            var models = await provider.FetchAvailableModelsAsync();

                            // Try to select a newer version of the same model if available
                            await TrySelectNewerModelVariantAsync(provider, currentModelId);

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
        /// Tries to select a newer variant of the same model if available
        /// </summary>
        /// <param name="provider">The LLM provider</param>
        /// <param name="currentModelId">The current model ID</param>
        /// <returns>True if a newer model was selected, false otherwise</returns>
        private async Task<bool> TrySelectNewerModelVariantAsync(ILlmProvider provider, string currentModelId)
        {
            try
            {
                // Get the base model name (without version numbers)
                var baseModelName = GetBaseModelName(currentModelId);
                if (string.IsNullOrEmpty(baseModelName))
                {
                    return false;
                }

                // Find all models that match the base name
                var matchingModels = provider.AvailableModels
                    .Where(m => GetBaseModelName(m.Id) == baseModelName)
                    .ToList();

                if (matchingModels.Count <= 1)
                {
                    return false; // No alternatives found
                }

                // Try to find a newer version with the same capabilities
                var currentModel = provider.CurrentModel;
                var newerModel = FindNewerModel(matchingModels, currentModel);

                if (newerModel != null && newerModel.Id != currentModelId)
                {
                    _logger.LogInformation("Upgrading {ProviderName} from {OldModel} to newer model {NewModel}",
                        provider.ProviderName, currentModelId, newerModel.Id);

                    await provider.SetModelAsync(newerModel.Id);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error trying to select newer model variant for provider {ProviderName}", provider.ProviderName);
                return false;
            }
        }

        /// <summary>
        /// Gets the base model name without version numbers
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>The base model name</returns>
        private string GetBaseModelName(string modelId)
        {
            // Handle different naming patterns
            // Examples:
            // - gemini-1.5-pro -> gemini-pro
            // - llama-3.1-70b-instruct -> llama-70b-instruct
            // - gpt-4-turbo -> gpt-4
            // - claude-3-opus -> claude-opus

            // Remove provider prefix if present (e.g., "openai/gpt-4" -> "gpt-4")
            var name = modelId;
            if (name.Contains('/'))
            {
                name = name.Split('/').Last();
            }

            // Replace version numbers with regex
            return System.Text.RegularExpressions.Regex.Replace(name, @"[-.]\d+(\.\d+)?[-]?", "-");
        }

        /// <summary>
        /// Finds a newer model from the list of matching models
        /// </summary>
        /// <param name="matchingModels">List of models with the same base name</param>
        /// <param name="currentModel">The current model</param>
        /// <returns>A newer model if found, null otherwise</returns>
        private LlmModel? FindNewerModel(List<LlmModel> matchingModels, LlmModel currentModel)
        {
            // Prefer models with "latest" in the name
            var latestModel = matchingModels.FirstOrDefault(m =>
                m.Id.Contains("latest", StringComparison.OrdinalIgnoreCase));

            if (latestModel != null)
            {
                return latestModel;
            }

            // Look for models with higher version numbers
            // This is a simple heuristic - we're looking for models with higher numbers in their IDs
            // while maintaining the same capabilities

            // Extract version numbers from model IDs
            var modelVersions = new Dictionary<LlmModel, double>();
            foreach (var model in matchingModels)
            {
                var versionMatch = System.Text.RegularExpressions.Regex.Match(model.Id, @"(\d+(\.\d+)?)");
                if (versionMatch.Success && double.TryParse(versionMatch.Groups[1].Value, out var version))
                {
                    modelVersions[model] = version;
                }
                else
                {
                    modelVersions[model] = 0;
                }
            }

            // Find models with the same or better capabilities
            var compatibleModels = matchingModels.Where(m =>
                m.SupportsToolCalls >= currentModel.SupportsToolCalls &&
                m.SupportsVision >= currentModel.SupportsVision &&
                m.MaxContextLength >= currentModel.MaxContextLength).ToList();

            // Return the model with the highest version number
            return compatibleModels
                .OrderByDescending(m => modelVersions.GetValueOrDefault(m, 0))
                .FirstOrDefault();
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
