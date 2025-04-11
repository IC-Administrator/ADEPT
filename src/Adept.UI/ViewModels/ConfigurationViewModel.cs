using Adept.Common.Interfaces;
using Adept.Common.Models;
using Adept.Core.Interfaces;
using Adept.UI.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IDatabaseBackupService _databaseBackupService;
        private readonly IDatabaseIntegrityService _databaseIntegrityService;
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
        private string _selectedWakeWordProvider = string.Empty;
        private string _selectedSttProvider = string.Empty;
        private string _selectedTtsProvider = string.Empty;
        private string _selectedTtsVoice = string.Empty;
        private float _ttsSpeed = 1.0f;
        private float _ttsVolume = 0.0f;
        private float _ttsClarity = 0.0f;
        private float _ttsEmotion = 0.0f;
        private bool _useDiskCache = true;
        private int _maxCacheItems = 100;

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

        private ObservableCollection<DatabaseBackupInfo> _availableBackups = new ObservableCollection<DatabaseBackupInfo>();
        /// <summary>
        /// Gets the available database backups
        /// </summary>
        public ObservableCollection<DatabaseBackupInfo> AvailableBackups
        {
            get => _availableBackups;
            private set => SetProperty(ref _availableBackups, value);
        }

        private string _selectedBackupPath = string.Empty;
        /// <summary>
        /// Gets or sets the selected backup path
        /// </summary>
        public string SelectedBackupPath
        {
            get => _selectedBackupPath;
            set => SetProperty(ref _selectedBackupPath, value);
        }

        private string _backupName = string.Empty;
        /// <summary>
        /// Gets or sets the backup name
        /// </summary>
        public string BackupName
        {
            get => _backupName;
            set => SetProperty(ref _backupName, value);
        }

        private string _databaseStatus = "Unknown";
        /// <summary>
        /// Gets or sets the database status
        /// </summary>
        public string DatabaseStatus
        {
            get => _databaseStatus;
            set => SetProperty(ref _databaseStatus, value);
        }

        private bool _isBackupInProgress = false;
        /// <summary>
        /// Gets or sets a value indicating whether a backup is in progress
        /// </summary>
        public bool IsBackupInProgress
        {
            get => _isBackupInProgress;
            set => SetProperty(ref _isBackupInProgress, value);
        }

        private bool _isRestoreInProgress = false;
        /// <summary>
        /// Gets or sets a value indicating whether a restore is in progress
        /// </summary>
        public bool IsRestoreInProgress
        {
            get => _isRestoreInProgress;
            set => SetProperty(ref _isRestoreInProgress, value);
        }

        private bool _isMaintenanceInProgress = false;
        /// <summary>
        /// Gets or sets a value indicating whether maintenance is in progress
        /// </summary>
        public bool IsMaintenanceInProgress
        {
            get => _isMaintenanceInProgress;
            set => SetProperty(ref _isMaintenanceInProgress, value);
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
        /// Gets or sets the selected wake word provider
        /// </summary>
        public string SelectedWakeWordProvider
        {
            get => _selectedWakeWordProvider;
            set => SetProperty(ref _selectedWakeWordProvider, value);
        }

        /// <summary>
        /// Gets or sets the selected speech-to-text provider
        /// </summary>
        public string SelectedSttProvider
        {
            get => _selectedSttProvider;
            set => SetProperty(ref _selectedSttProvider, value);
        }

        /// <summary>
        /// Gets or sets the selected text-to-speech provider
        /// </summary>
        public string SelectedTtsProvider
        {
            get => _selectedTtsProvider;
            set
            {
                if (SetProperty(ref _selectedTtsProvider, value))
                {
                    // Update available voices for this provider
                    UpdateAvailableVoices();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected text-to-speech voice
        /// </summary>
        public string SelectedTtsVoice
        {
            get => _selectedTtsVoice;
            set => SetProperty(ref _selectedTtsVoice, value);
        }

        /// <summary>
        /// Gets or sets the text-to-speech speed
        /// </summary>
        public float TtsSpeed
        {
            get => _ttsSpeed;
            set => SetProperty(ref _ttsSpeed, value);
        }

        /// <summary>
        /// Gets or sets the text-to-speech volume
        /// </summary>
        public float TtsVolume
        {
            get => _ttsVolume;
            set => SetProperty(ref _ttsVolume, value);
        }

        /// <summary>
        /// Gets or sets the text-to-speech clarity
        /// </summary>
        public float TtsClarity
        {
            get => _ttsClarity;
            set => SetProperty(ref _ttsClarity, value);
        }

        /// <summary>
        /// Gets or sets the text-to-speech emotion
        /// </summary>
        public float TtsEmotion
        {
            get => _ttsEmotion;
            set => SetProperty(ref _ttsEmotion, value);
        }

        /// <summary>
        /// Gets or sets whether to use disk cache for text-to-speech
        /// </summary>
        public bool UseDiskCache
        {
            get => _useDiskCache;
            set => SetProperty(ref _useDiskCache, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of cache items
        /// </summary>
        public int MaxCacheItems
        {
            get => _maxCacheItems;
            set => SetProperty(ref _maxCacheItems, value);
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
        /// Gets the available wake word providers
        /// </summary>
        public ObservableCollection<string> WakeWordProviders { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the available speech-to-text providers
        /// </summary>
        public ObservableCollection<string> SttProviders { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the available text-to-speech providers
        /// </summary>
        public ObservableCollection<string> TtsProviders { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the available text-to-speech voices
        /// </summary>
        public ObservableCollection<string> TtsVoices { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the save general settings command
        /// </summary>
        public ICommand SaveGeneralSettingsCommand { get; }

        /// <summary>
        /// Gets the save API keys command
        /// </summary>
        public ICommand SaveApiKeysCommand { get; }

        /// <summary>
        /// Gets the save voice settings command
        /// </summary>
        public ICommand SaveVoiceSettingsCommand { get; }

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
        /// Gets the refresh models command
        /// </summary>
        public ICommand RefreshModelsCommand { get; }

        /// <summary>
        /// Gets the create database backup command
        /// </summary>
        public ICommand CreateBackupCommand { get; }

        /// <summary>
        /// Gets the restore database backup command
        /// </summary>
        public ICommand RestoreBackupCommand { get; }

        /// <summary>
        /// Gets the verify database integrity command
        /// </summary>
        public ICommand VerifyDatabaseCommand { get; }

        /// <summary>
        /// Gets the perform database maintenance command
        /// </summary>
        public ICommand PerformMaintenanceCommand { get; }

        /// <summary>
        /// Gets the refresh database backups command
        /// </summary>
        public ICommand RefreshBackupsCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationViewModel"/> class
        /// </summary>
        /// <param name="configurationService">The configuration service</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="llmService">The LLM service</param>
        /// <param name="mcpServerManager">The MCP server manager</param>
        /// <param name="databaseBackupService">The database backup service</param>
        /// <param name="databaseIntegrityService">The database integrity service</param>
        /// <param name="logger">The logger</param>
        public ConfigurationViewModel(
            IConfigurationService configurationService,
            ISecureStorageService secureStorageService,
            ILlmService llmService,
            IMcpServerManager mcpServerManager,
            IDatabaseBackupService databaseBackupService,
            IDatabaseIntegrityService databaseIntegrityService,
            ILogger<ConfigurationViewModel> logger)
        {
            _configurationService = configurationService;
            _secureStorageService = secureStorageService;
            _llmService = llmService;
            _mcpServerManager = mcpServerManager;
            _databaseBackupService = databaseBackupService;
            _databaseIntegrityService = databaseIntegrityService;
            _logger = logger;

            SaveGeneralSettingsCommand = new RelayCommand(SaveGeneralSettingsAsync);
            SaveApiKeysCommand = new RelayCommand(SaveApiKeysAsync);
            SaveVoiceSettingsCommand = new RelayCommand(SaveVoiceSettingsAsync);
            StartMcpServerCommand = new RelayCommand(StartMcpServerAsync, () => !McpServerRunning);
            StopMcpServerCommand = new RelayCommand(StopMcpServerAsync, () => McpServerRunning);
            RestartMcpServerCommand = new RelayCommand(RestartMcpServerAsync);
            BrowseDataDirectoryCommand = new RelayCommand(BrowseDataDirectoryAsync);
            RefreshModelsCommand = new RelayCommand(RefreshModelsAsync);

            // Database commands
            CreateBackupCommand = new RelayCommand(CreateBackupAsync);
            RestoreBackupCommand = new RelayCommand<string>(RestoreBackupAsync);
            VerifyDatabaseCommand = new RelayCommand(VerifyDatabaseAsync);
            PerformMaintenanceCommand = new RelayCommand(PerformMaintenanceAsync);
            RefreshBackupsCommand = new RelayCommand(() => { RefreshBackupsAsync().ConfigureAwait(false); });

            // Subscribe to MCP server status changes
            _mcpServerManager.ServerStatusChanged += OnMcpServerStatusChanged;

            // Load configuration and database backups
            LoadConfigurationAsync().ConfigureAwait(false);
            RefreshBackupsAsync().ConfigureAwait(false);
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

                // Load voice settings
                WakeWordProviders.Clear();
                WakeWordProviders.Add("vosk");
                WakeWordProviders.Add("simple");
                SelectedWakeWordProvider = await _configurationService.GetConfigurationValueAsync("wake_word_detector", "vosk") ?? "vosk";

                SttProviders.Clear();
                SttProviders.Add("whisper");
                SttProviders.Add("google");
                SttProviders.Add("simple");
                SelectedSttProvider = await _configurationService.GetConfigurationValueAsync("speech_to_text_provider", "whisper") ?? "whisper";

                TtsProviders.Clear();
                TtsProviders.Add("fishaudio");
                TtsProviders.Add("openai");
                TtsProviders.Add("google");
                TtsProviders.Add("simple");
                SelectedTtsProvider = await _configurationService.GetConfigurationValueAsync("text_to_speech_provider", "fishaudio") ?? "fishaudio";

                // Load Fish Audio settings
                SelectedTtsVoice = await _configurationService.GetConfigurationValueAsync("fish_audio_voice_id", "default") ?? "default";
                TtsSpeed = float.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_speed", "1.0") ?? "1.0");
                TtsVolume = float.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_volume", "0.0") ?? "0.0");
                TtsClarity = float.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_clarity", "0.0") ?? "0.0");
                TtsEmotion = float.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_emotion", "0.0") ?? "0.0");
                UseDiskCache = bool.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_use_disk_cache", "true") ?? "true");
                MaxCacheItems = int.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_max_cache_items", "100") ?? "100");

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
        /// Saves the voice settings
        /// </summary>
        private async void SaveVoiceSettingsAsync()
        {
            try
            {
                IsBusy = true;

                // Save voice settings
                await _configurationService.SetConfigurationValueAsync("wake_word_detector", SelectedWakeWordProvider);
                await _configurationService.SetConfigurationValueAsync("speech_to_text_provider", SelectedSttProvider);
                await _configurationService.SetConfigurationValueAsync("text_to_speech_provider", SelectedTtsProvider);
                await _configurationService.SetConfigurationValueAsync("fish_audio_voice_id", SelectedTtsVoice);
                await _configurationService.SetConfigurationValueAsync("fish_audio_speed", TtsSpeed.ToString());
                await _configurationService.SetConfigurationValueAsync("fish_audio_volume", TtsVolume.ToString());
                await _configurationService.SetConfigurationValueAsync("fish_audio_clarity", TtsClarity.ToString());
                await _configurationService.SetConfigurationValueAsync("fish_audio_emotion", TtsEmotion.ToString());
                await _configurationService.SetConfigurationValueAsync("fish_audio_use_disk_cache", UseDiskCache.ToString());
                await _configurationService.SetConfigurationValueAsync("fish_audio_max_cache_items", MaxCacheItems.ToString());

                _logger.LogInformation("Voice settings saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving voice settings");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Updates the available models for the selected provider
        /// </summary>
        private async void UpdateAvailableModels()
        {
            try
            {
                IsBusy = true;

                // Clear the current models
                AvailableModels.Clear();

                // Get the provider
                var provider = _llmService.AvailableProviders.FirstOrDefault(p => p.ProviderName == SelectedLlmProvider);
                if (provider == null)
                {
                    return;
                }

                // Use the new model refresh functionality to ensure we have the latest models
                await _llmService.RefreshModelsForProviderAsync(provider.ProviderName);

                // Add the models to the UI list
                foreach (var model in provider.AvailableModels)
                {
                    AvailableModels.Add(model.Id);
                }

                // If no models were fetched, add fallback models based on the provider
                if (AvailableModels.Count == 0)
                {
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
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Updates the available voices for the selected provider
        /// </summary>
        private void UpdateAvailableVoices()
        {
            try
            {
                // Clear the current voices
                TtsVoices.Clear();

                // Add the available voices based on the provider
                switch (SelectedTtsProvider.ToLowerInvariant())
                {
                    case "fishaudio":
                        // Add default voices
                        TtsVoices.Add("default");
                        TtsVoices.Add("male-1");
                        TtsVoices.Add("male-2");
                        TtsVoices.Add("female-1");
                        TtsVoices.Add("female-2");
                        TtsVoices.Add("child-1");
                        TtsVoices.Add("elder-1");
                        TtsVoices.Add("narrator-1");
                        TtsVoices.Add("assistant-1");
                        break;
                    case "openai":
                        TtsVoices.Add("alloy");
                        TtsVoices.Add("echo");
                        TtsVoices.Add("fable");
                        TtsVoices.Add("onyx");
                        TtsVoices.Add("nova");
                        TtsVoices.Add("shimmer");
                        break;
                    case "google":
                        TtsVoices.Add("en-US-Neural2-A");
                        TtsVoices.Add("en-US-Neural2-C");
                        TtsVoices.Add("en-US-Neural2-D");
                        TtsVoices.Add("en-US-Neural2-F");
                        TtsVoices.Add("en-US-Neural2-H");
                        break;
                    default:
                        TtsVoices.Add("default");
                        break;
                }

                // Set the current voice
                if (TtsVoices.Count > 0)
                {
                    SelectedTtsVoice = TtsVoices[0];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating available voices");
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
        /// Refreshes the models for all providers
        /// </summary>
        private async void RefreshModelsAsync()
        {
            try
            {
                IsBusy = true;

                // Refresh models for all providers
                await _llmService.RefreshModelsAsync();
                _logger.LogInformation("Refreshed models for all providers");

                // Update the UI with the refreshed models
                UpdateAvailableModels();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing models");
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

        #region Database Management

        /// <summary>
        /// Creates a database backup
        /// </summary>
        private async void CreateBackupAsync()
        {
            try
            {
                IsBackupInProgress = true;

                // Create a backup with the specified name or a timestamp if no name is provided
                string backupPath = await _databaseBackupService.CreateBackupAsync(string.IsNullOrWhiteSpace(BackupName) ? null : BackupName);

                // Refresh the list of backups
                await RefreshBackupsAsync();

                // Clear the backup name field
                BackupName = string.Empty;

                _logger.LogInformation("Database backup created at {BackupPath}", backupPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database backup");
            }
            finally
            {
                IsBackupInProgress = false;
            }
        }

        /// <summary>
        /// Restores a database backup
        /// </summary>
        /// <param name="backupPath">The backup path</param>
        private async void RestoreBackupAsync(string backupPath)
        {
            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = SelectedBackupPath;
            }

            if (string.IsNullOrEmpty(backupPath))
            {
                _logger.LogWarning("No backup selected for restore");
                return;
            }

            try
            {
                IsRestoreInProgress = true;

                // Verify the backup integrity first
                bool isValid = await _databaseBackupService.VerifyBackupIntegrityAsync(backupPath);
                if (!isValid)
                {
                    _logger.LogWarning("Backup integrity check failed for {BackupPath}", backupPath);
                    return;
                }

                // Restore the backup
                bool success = await _databaseBackupService.RestoreFromBackupAsync(backupPath);
                if (success)
                {
                    _logger.LogInformation("Database restored from backup {BackupPath}", backupPath);
                }
                else
                {
                    _logger.LogWarning("Failed to restore database from backup {BackupPath}", backupPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring database from backup {BackupPath}", backupPath);
            }
            finally
            {
                IsRestoreInProgress = false;
            }
        }

        /// <summary>
        /// Verifies the database integrity
        /// </summary>
        private async void VerifyDatabaseAsync()
        {
            try
            {
                IsMaintenanceInProgress = true;

                // Check database integrity
                var result = await _databaseIntegrityService.CheckIntegrityAsync();
                if (result.IsValid)
                {
                    DatabaseStatus = "Valid";
                    _logger.LogInformation("Database integrity check passed");
                }
                else
                {
                    DatabaseStatus = "Invalid: " + string.Join(", ", result.Issues);
                    _logger.LogWarning("Database integrity check failed: {Issues}", string.Join(", ", result.Issues));
                }
            }
            catch (Exception ex)
            {
                DatabaseStatus = "Error: " + ex.Message;
                _logger.LogError(ex, "Error checking database integrity");
            }
            finally
            {
                IsMaintenanceInProgress = false;
            }
        }

        /// <summary>
        /// Performs database maintenance
        /// </summary>
        private async void PerformMaintenanceAsync()
        {
            try
            {
                IsMaintenanceInProgress = true;

                // Perform database maintenance
                bool success = await _databaseIntegrityService.PerformMaintenanceAsync();
                if (success)
                {
                    DatabaseStatus = "Maintenance completed successfully";
                    _logger.LogInformation("Database maintenance completed successfully");
                }
                else
                {
                    DatabaseStatus = "Maintenance failed";
                    _logger.LogWarning("Database maintenance failed");
                }
            }
            catch (Exception ex)
            {
                DatabaseStatus = "Error: " + ex.Message;
                _logger.LogError(ex, "Error performing database maintenance");
            }
            finally
            {
                IsMaintenanceInProgress = false;
            }
        }

        /// <summary>
        /// Refreshes the list of available backups
        /// </summary>
        private async Task RefreshBackupsAsync()
        {
            try
            {
                // Get available backups
                var backups = await _databaseBackupService.GetAvailableBackupsAsync();

                // Update the collection
                AvailableBackups.Clear();
                foreach (var backup in backups)
                {
                    AvailableBackups.Add(backup);
                }

                _logger.LogInformation("Found {Count} database backups", AvailableBackups.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing database backups");
            }
        }

        #endregion
    }
}
