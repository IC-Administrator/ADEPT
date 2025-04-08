using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.UI.Commands;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// View model for the Configuration tab
    /// </summary>
    public class ConfigurationViewModel : ViewModelBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILlmService _llmService;
        private readonly IMcpServerManager _mcpServerManager;
        private readonly ILogger<ConfigurationViewModel> _logger;
        private bool _isBusy;
        private string _selectedTab = "General";
        private string _appName = string.Empty;
        private string _appVersion = string.Empty;
        private string _dataDirectory = string.Empty;
        private string _selectedLlmProvider = string.Empty;
        private string _selectedLlmModel = string.Empty;
        private string _openAiApiKey = string.Empty;
        private string _anthropicApiKey = string.Empty;
        private string _googleApiKey = string.Empty;
        private string _metaApiKey = string.Empty;
        private string _openRouterApiKey = string.Empty;
        private string _deepSeekApiKey = string.Empty;
        private string _braveApiKey = string.Empty;
        private bool _mcpServerRunning;
        private string _mcpServerUrl = string.Empty;

        /// <summary>
        /// Gets or sets whether the view model is busy
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the selected tab
        /// </summary>
        public string SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        /// <summary>
        /// Gets or sets the application name
        /// </summary>
        public string AppName
        {
            get => _appName;
            set => SetProperty(ref _appName, value);
        }

        /// <summary>
        /// Gets or sets the application version
        /// </summary>
        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        /// <summary>
        /// Gets or sets the data directory
        /// </summary>
        public string DataDirectory
        {
            get => _dataDirectory;
            set => SetProperty(ref _dataDirectory, value);
        }

        /// <summary>
        /// Gets or sets the selected LLM provider
        /// </summary>
        public string SelectedLlmProvider
        {
            get => _selectedLlmProvider;
            set
            {
                if (SetProperty(ref _selectedLlmProvider, value))
                {
                    // Update available models for this provider
                    UpdateAvailableModels();
                    SetLlmProviderAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected LLM model
        /// </summary>
        public string SelectedLlmModel
        {
            get => _selectedLlmModel;
            set
            {
                if (SetProperty(ref _selectedLlmModel, value))
                {
                    SetLlmModelAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets or sets the OpenAI API key
        /// </summary>
        public string OpenAiApiKey
        {
            get => _openAiApiKey;
            set => SetProperty(ref _openAiApiKey, value);
        }

        /// <summary>
        /// Gets or sets the Anthropic API key
        /// </summary>
        public string AnthropicApiKey
        {
            get => _anthropicApiKey;
            set => SetProperty(ref _anthropicApiKey, value);
        }

        /// <summary>
        /// Gets or sets the Google API key
        /// </summary>
        public string GoogleApiKey
        {
            get => _googleApiKey;
            set => SetProperty(ref _googleApiKey, value);
        }

        /// <summary>
        /// Gets or sets the Meta API key
        /// </summary>
        public string MetaApiKey
        {
            get => _metaApiKey;
            set => SetProperty(ref _metaApiKey, value);
        }

        /// <summary>
        /// Gets or sets the OpenRouter API key
        /// </summary>
        public string OpenRouterApiKey
        {
            get => _openRouterApiKey;
            set => SetProperty(ref _openRouterApiKey, value);
        }

        /// <summary>
        /// Gets or sets the DeepSeek API key
        /// </summary>
        public string DeepSeekApiKey
        {
            get => _deepSeekApiKey;
            set => SetProperty(ref _deepSeekApiKey, value);
        }

        /// <summary>
        /// Gets or sets the Brave API key
        /// </summary>
        public string BraveApiKey
        {
            get => _braveApiKey;
            set => SetProperty(ref _braveApiKey, value);
        }

        /// <summary>
        /// Gets or sets whether the MCP server is running
        /// </summary>
        public bool McpServerRunning
        {
            get => _mcpServerRunning;
            set => SetProperty(ref _mcpServerRunning, value);
        }

        /// <summary>
        /// Gets or sets the MCP server URL
        /// </summary>
        public string McpServerUrl
        {
            get => _mcpServerUrl;
            set => SetProperty(ref _mcpServerUrl, value);
        }

        /// <summary>
        /// Gets the LLM providers
        /// </summary>
        public ObservableCollection<string> LlmProviders { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the available models for the selected provider
        /// </summary>
        public ObservableCollection<string> AvailableModels { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the save general settings command
        /// </summary>
        public ICommand SaveGeneralSettingsCommand { get; }

        /// <summary>
        /// Gets the save API keys command
        /// </summary>
        public ICommand SaveApiKeysCommand { get; }

        /// <summary>
        /// Gets the start MCP server command
        /// </summary>
        public ICommand StartMcpServerCommand { get; }

        /// <summary>
        /// Gets the stop MCP server command
        /// </summary>
        public ICommand StopMcpServerCommand { get; }

        /// <summary>
        /// Gets the restart MCP server command
        /// </summary>
        public ICommand RestartMcpServerCommand { get; }

        /// <summary>
        /// Gets the browse data directory command
        /// </summary>
        public ICommand BrowseDataDirectoryCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationViewModel"/> class
        /// </summary>
        /// <param name="configurationService">The configuration service</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="llmService">The LLM service</param>
        /// <param name="mcpServerManager">The MCP server manager</param>
        /// <param name="logger">The logger</param>
        public ConfigurationViewModel(
            IConfigurationService configurationService,
            ISecureStorageService secureStorageService,
            ILlmService llmService,
            IMcpServerManager mcpServerManager,
            ILogger<ConfigurationViewModel> logger)
        {
            _configurationService = configurationService;
            _secureStorageService = secureStorageService;
            _llmService = llmService;
            _mcpServerManager = mcpServerManager;
            _logger = logger;

            SaveGeneralSettingsCommand = new RelayCommand(SaveGeneralSettingsAsync);
            SaveApiKeysCommand = new RelayCommand(SaveApiKeysAsync);
            StartMcpServerCommand = new RelayCommand(StartMcpServerAsync, () => !McpServerRunning);
            StopMcpServerCommand = new RelayCommand(StopMcpServerAsync, () => McpServerRunning);
            RestartMcpServerCommand = new RelayCommand(RestartMcpServerAsync);
            BrowseDataDirectoryCommand = new RelayCommand(BrowseDataDirectoryAsync);

            // Subscribe to MCP server status changes
            _mcpServerManager.ServerStatusChanged += OnMcpServerStatusChanged;

            // Load configuration
            LoadConfigurationAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the configuration
        /// </summary>
        private async Task LoadConfigurationAsync()
        {
            try
            {
                IsBusy = true;

                // Load general settings
                AppName = "ADEPT AI Teaching Assistant";
                AppVersion = "1.0.0";
                DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Adept");

                // Load LLM providers
                LlmProviders.Clear();
                foreach (var provider in _llmService.AvailableProviders)
                {
                    LlmProviders.Add(provider.ProviderName);
                }

                // Set the selected LLM provider
                SelectedLlmProvider = _llmService.ActiveProvider.ProviderName;

                // Load the saved model preference for this provider
                string savedModel = string.Empty;
                if (!string.IsNullOrEmpty(savedModel))
                {
                    // This will be set after UpdateAvailableModels is called
                    SelectedLlmModel = savedModel;
                }

                // Load API keys
                OpenAiApiKey = await _secureStorageService.RetrieveSecureValueAsync("openai_api_key") ?? string.Empty;
                AnthropicApiKey = await _secureStorageService.RetrieveSecureValueAsync("anthropic_api_key") ?? string.Empty;
                GoogleApiKey = await _secureStorageService.RetrieveSecureValueAsync("google_api_key") ?? string.Empty;
                MetaApiKey = await _secureStorageService.RetrieveSecureValueAsync("meta_api_key") ?? string.Empty;
                OpenRouterApiKey = await _secureStorageService.RetrieveSecureValueAsync("openrouter_api_key") ?? string.Empty;
                DeepSeekApiKey = await _secureStorageService.RetrieveSecureValueAsync("deepseek_api_key") ?? string.Empty;
                BraveApiKey = await _secureStorageService.RetrieveSecureValueAsync("brave_api_key") ?? string.Empty;

                // Get MCP server status
                McpServerRunning = _mcpServerManager.IsServerRunning;
                McpServerUrl = _mcpServerManager.ServerUrl;

                _logger.LogInformation("Configuration loaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Saves the general settings
        /// </summary>
        private async void SaveGeneralSettingsAsync()
        {
            try
            {
                IsBusy = true;

                // Save general settings
                // TODO: Implement configuration saving
                await Task.CompletedTask;

                _logger.LogInformation("General settings saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving general settings");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Saves the API keys
        /// </summary>
        private async void SaveApiKeysAsync()
        {
            try
            {
                IsBusy = true;

                // Save API keys
                await _secureStorageService.StoreSecureValueAsync("openai_api_key", OpenAiApiKey);
                await _secureStorageService.StoreSecureValueAsync("anthropic_api_key", AnthropicApiKey);
                await _secureStorageService.StoreSecureValueAsync("google_api_key", GoogleApiKey);
                await _secureStorageService.StoreSecureValueAsync("meta_api_key", MetaApiKey);
                await _secureStorageService.StoreSecureValueAsync("openrouter_api_key", OpenRouterApiKey);
                await _secureStorageService.StoreSecureValueAsync("deepseek_api_key", DeepSeekApiKey);
                await _secureStorageService.StoreSecureValueAsync("brave_api_key", BraveApiKey);

                _logger.LogInformation("API keys saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API keys");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Updates the available models for the selected provider
        /// </summary>
        private void UpdateAvailableModels()
        {
            try
            {
                // Clear the current models
                AvailableModels.Clear();

                // Get the provider
                var provider = _llmService.AvailableProviders.FirstOrDefault(p => p.ProviderName == SelectedLlmProvider);
                if (provider == null)
                {
                    return;
                }

                // Add the available models based on the provider
                switch (provider.ProviderName.ToLowerInvariant())
                {
                    case "openai":
                        AvailableModels.Add("gpt-3.5-turbo");
                        AvailableModels.Add("gpt-3.5-turbo-16k");
                        AvailableModels.Add("gpt-4");
                        AvailableModels.Add("gpt-4-32k");
                        AvailableModels.Add("gpt-4-turbo");
                        AvailableModels.Add("gpt-4o");
                        break;
                    case "anthropic":
                        AvailableModels.Add("claude-instant-1");
                        AvailableModels.Add("claude-2");
                        AvailableModels.Add("claude-3-opus");
                        AvailableModels.Add("claude-3-sonnet");
                        AvailableModels.Add("claude-3-haiku");
                        break;
                    case "google":
                        AvailableModels.Add("gemini-pro");
                        AvailableModels.Add("gemini-ultra");
                        break;
                    case "meta":
                        AvailableModels.Add("llama-3-8b");
                        AvailableModels.Add("llama-3-70b");
                        break;
                    case "deepseek":
                        AvailableModels.Add("deepseek-chat");
                        AvailableModels.Add("deepseek-coder");
                        break;
                    case "openrouter":
                        AvailableModels.Add("openai/gpt-4");
                        AvailableModels.Add("anthropic/claude-3-opus");
                        AvailableModels.Add("meta-llama/llama-3-70b");
                        AvailableModels.Add("google/gemini-pro");
                        break;
                }

                // Set the current model
                SelectedLlmModel = provider.ModelName;

                // If the selected model is not in the list, add it
                if (!string.IsNullOrEmpty(SelectedLlmModel) && !AvailableModels.Contains(SelectedLlmModel))
                {
                    AvailableModels.Add(SelectedLlmModel);
                }

                // If no model is selected, select the first one
                if (string.IsNullOrEmpty(SelectedLlmModel) && AvailableModels.Count > 0)
                {
                    SelectedLlmModel = AvailableModels[0];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating available models");
            }
        }

        /// <summary>
        /// Sets the LLM provider
        /// </summary>
        private async Task SetLlmProviderAsync()
        {
            try
            {
                IsBusy = true;

                // Set the LLM provider
                var success = await _llmService.SetActiveProviderAsync(SelectedLlmProvider);
                if (success)
                {
                    _logger.LogInformation("LLM provider set to {ProviderName}", SelectedLlmProvider);

                    // Update the selected model
                    if (!string.IsNullOrEmpty(SelectedLlmModel))
                    {
                        await SetLlmModelAsync();
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to set LLM provider to {ProviderName}", SelectedLlmProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting LLM provider");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Sets the LLM model
        /// </summary>
        private async Task SetLlmModelAsync()
        {
            try
            {
                IsBusy = true;

                // Get the provider
                var provider = _llmService.AvailableProviders.FirstOrDefault(p => p.ProviderName == SelectedLlmProvider);
                if (provider == null)
                {
                    _logger.LogWarning("Provider not found: {ProviderName}", SelectedLlmProvider);
                    return;
                }

                // Set the model
                var success = await provider.SetModelAsync(SelectedLlmModel);
                if (success)
                {
                    _logger.LogInformation("LLM model set to {ModelName}", SelectedLlmModel);

                    // Save the model preference
                    // TODO: Implement configuration saving
                    await Task.CompletedTask;
                }
                else
                {
                    _logger.LogWarning("Failed to set LLM model to {ModelName}", SelectedLlmModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting LLM model");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Starts the MCP server
        /// </summary>
        private async void StartMcpServerAsync()
        {
            try
            {
                IsBusy = true;

                // Start the MCP server
                await _mcpServerManager.StartServerAsync();

                _logger.LogInformation("MCP server started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting MCP server");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Stops the MCP server
        /// </summary>
        private async void StopMcpServerAsync()
        {
            try
            {
                IsBusy = true;

                // Stop the MCP server
                await _mcpServerManager.StopServerAsync();

                _logger.LogInformation("MCP server stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MCP server");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Restarts the MCP server
        /// </summary>
        private async void RestartMcpServerAsync()
        {
            try
            {
                IsBusy = true;

                // Restart the MCP server
                await _mcpServerManager.RestartServerAsync();

                _logger.LogInformation("MCP server restarted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting MCP server");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Browses for the data directory
        /// </summary>
        private void BrowseDataDirectoryAsync()
        {
            try
            {
                // In a real implementation, this would show a folder browser dialog
                DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Adept");
                _logger.LogInformation("Data directory set to {DataDirectory}", DataDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing for data directory");
            }
        }

        /// <summary>
        /// Handles MCP server status changes
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void OnMcpServerStatusChanged(object? sender, McpServerStatusChangedEventArgs e)
        {
            McpServerRunning = e.IsRunning;
            McpServerUrl = e.ServerUrl;

            // Refresh command can execute status
            (StartMcpServerCommand as Commands.RelayCommand)?.RaiseCanExecuteChanged();
            (StopMcpServerCommand as Commands.RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
