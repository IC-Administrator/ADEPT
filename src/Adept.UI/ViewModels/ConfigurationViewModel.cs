using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
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
        private string _openAiApiKey = string.Empty;
        private string _anthropicApiKey = string.Empty;
        private string _googleApiKey = string.Empty;
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
                    SetLlmProviderAsync().ConfigureAwait(false);
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
                AppName = _configurationService.GetValue<string>("AppName") ?? "ADEPT AI Teaching Assistant";
                AppVersion = _configurationService.GetValue<string>("AppVersion") ?? "1.0.0";
                DataDirectory = _configurationService.GetValue<string>("DataDirectory") ?? 
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Adept");

                // Load LLM providers
                LlmProviders.Clear();
                foreach (var provider in _llmService.AvailableProviders)
                {
                    LlmProviders.Add(provider.ProviderName);
                }

                // Set the selected LLM provider
                SelectedLlmProvider = _llmService.ActiveProvider.ProviderName;

                // Load API keys
                OpenAiApiKey = await _secureStorageService.RetrieveSecureValueAsync("openai_api_key") ?? string.Empty;
                AnthropicApiKey = await _secureStorageService.RetrieveSecureValueAsync("anthropic_api_key") ?? string.Empty;
                GoogleApiKey = await _secureStorageService.RetrieveSecureValueAsync("google_api_key") ?? string.Empty;
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
                _configurationService.SetValue("AppName", AppName);
                _configurationService.SetValue("AppVersion", AppVersion);
                _configurationService.SetValue("DataDirectory", DataDirectory);

                await _configurationService.SaveAsync();

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
            (StartMcpServerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopMcpServerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
