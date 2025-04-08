using Adept.Core.Interfaces;
using Adept.UI.Commands;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// View model for the System Status tab
    /// </summary>
    public class SystemStatusViewModel : ViewModelBase
    {
        private readonly ILogger<SystemStatusViewModel> _logger;
        private readonly IMcpServerManager _mcpServerManager;
        private readonly IVoiceService _voiceService;
        private readonly ILlmService _llmService;
        private bool _isBusy;
        private bool _mcpServerRunning;
        private string _mcpServerUrl = string.Empty;
        private VoiceServiceState _voiceServiceState;
        private string _activeLlmProvider = string.Empty;
        private string _activeLlmModel = string.Empty;
        private double _cpuUsage;
        private double _memoryUsage;
        private double _diskUsage;
        private string _logContent = string.Empty;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;
        private readonly System.Threading.Timer _refreshTimer;

        /// <summary>
        /// Gets or sets whether the view model is busy
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
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
        /// Gets or sets the voice service state
        /// </summary>
        public VoiceServiceState VoiceServiceState
        {
            get => _voiceServiceState;
            set => SetProperty(ref _voiceServiceState, value);
        }

        /// <summary>
        /// Gets or sets the active LLM provider
        /// </summary>
        public string ActiveLlmProvider
        {
            get => _activeLlmProvider;
            set => SetProperty(ref _activeLlmProvider, value);
        }

        /// <summary>
        /// Gets or sets the active LLM model
        /// </summary>
        public string ActiveLlmModel
        {
            get => _activeLlmModel;
            set => SetProperty(ref _activeLlmModel, value);
        }

        /// <summary>
        /// Gets or sets the CPU usage
        /// </summary>
        public double CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        /// <summary>
        /// Gets or sets the memory usage
        /// </summary>
        public double MemoryUsage
        {
            get => _memoryUsage;
            set => SetProperty(ref _memoryUsage, value);
        }

        /// <summary>
        /// Gets or sets the disk usage
        /// </summary>
        public double DiskUsage
        {
            get => _diskUsage;
            set => SetProperty(ref _diskUsage, value);
        }

        /// <summary>
        /// Gets or sets the log content
        /// </summary>
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        /// <summary>
        /// Gets the tool providers
        /// </summary>
        public ObservableCollection<string> ToolProviders { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the refresh command
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Gets the clear logs command
        /// </summary>
        public ICommand ClearLogsCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemStatusViewModel"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="mcpServerManager">The MCP server manager</param>
        /// <param name="voiceService">The voice service</param>
        /// <param name="llmService">The LLM service</param>
        public SystemStatusViewModel(
            ILogger<SystemStatusViewModel> logger,
            IMcpServerManager mcpServerManager,
            IVoiceService voiceService,
            ILlmService llmService)
        {
            _logger = logger;
            _mcpServerManager = mcpServerManager;
            _voiceService = voiceService;
            _llmService = llmService;

            RefreshCommand = new RelayCommand(RefreshAsync);
            ClearLogsCommand = new RelayCommand(ClearLogsAsync);

            // Subscribe to events
            _mcpServerManager.ServerStatusChanged += OnMcpServerStatusChanged;
            _voiceService.StateChanged += OnVoiceServiceStateChanged;

            // Initialize performance counters
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing performance counters");
            }

            // Start refresh timer
            _refreshTimer = new System.Threading.Timer(RefreshTimerCallback, null, 0, 5000);

            // Load initial data
            RefreshAsync();
        }

        /// <summary>
        /// Refreshes the data
        /// </summary>
        private async void RefreshAsync()
        {
            try
            {
                IsBusy = true;

                // Get MCP server status
                McpServerRunning = _mcpServerManager.IsServerRunning;
                McpServerUrl = _mcpServerManager.ServerUrl;

                // Get voice service state
                VoiceServiceState = _voiceService.State;

                // Get LLM provider info
                ActiveLlmProvider = _llmService.ActiveProvider.ProviderName;
                ActiveLlmModel = _llmService.ActiveProvider.CurrentModel.Name;

                // Get tool providers
                ToolProviders.Clear();
                foreach (var provider in _mcpServerManager.ToolProviders)
                {
                    ToolProviders.Add(provider.ProviderName);
                }

                // Get system performance
                UpdateSystemPerformance();

                // Get logs
                LoadLogs();

                _logger.LogInformation("System status refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing system status");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Updates system performance metrics
        /// </summary>
        private void UpdateSystemPerformance()
        {
            try
            {
                // Get CPU usage
                CpuUsage = Math.Round(_cpuCounter.NextValue(), 1);

                // Get memory usage
                var totalPhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024 * 1024);
                var availableMemory = _ramCounter.NextValue();
                MemoryUsage = Math.Round(100 - (availableMemory / totalPhysicalMemory * 100), 1);

                // Get disk usage
                var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:");
                var totalSize = drive.TotalSize;
                var freeSpace = drive.AvailableFreeSpace;
                DiskUsage = Math.Round(100 - ((double)freeSpace / totalSize * 100), 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system performance");
            }
        }

        /// <summary>
        /// Loads the logs
        /// </summary>
        private void LoadLogs()
        {
            try
            {
                // In a real implementation, this would load logs from a file
                if (string.IsNullOrEmpty(LogContent))
                {
                    LogContent = "System started\nLoading components...\nAll components loaded successfully";
                }

                LogContent += $"\n[{DateTime.Now:HH:mm:ss}] System status refreshed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading logs");
            }
        }

        /// <summary>
        /// Clears the logs
        /// </summary>
        private void ClearLogsAsync()
        {
            try
            {
                LogContent = string.Empty;
                _logger.LogInformation("Logs cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing logs");
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
        }

        /// <summary>
        /// Handles voice service state changes
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void OnVoiceServiceStateChanged(object? sender, VoiceServiceStateChangedEventArgs e)
        {
            VoiceServiceState = e.NewState;
        }

        /// <summary>
        /// Refresh timer callback
        /// </summary>
        /// <param name="state">The state</param>
        private void RefreshTimerCallback(object? state)
        {
            try
            {
                // Update system performance on the UI thread
                App.Current.Dispatcher.Invoke(() =>
                {
                    UpdateSystemPerformance();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in refresh timer callback");
            }
        }
    }
}
