using Adept.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// View model for calendar settings
    /// </summary>
    public class CalendarSettingsViewModel : ViewModelBase
    {
        private readonly ICalendarService _calendarService;
        private readonly ICalendarSyncService _calendarSyncService;
        private readonly ISecureStorageService _secureStorageService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CalendarSettingsViewModel> _logger;
        private bool _isAuthenticated;
        private bool _isAuthenticating;
        private string _clientId = string.Empty;
        private string _clientSecret = string.Empty;
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Gets or sets whether the service is authenticated
        /// </summary>
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
        }

        /// <summary>
        /// Gets or sets whether authentication is in progress
        /// </summary>
        public bool IsAuthenticating
        {
            get => _isAuthenticating;
            set => this.RaiseAndSetIfChanged(ref _isAuthenticating, value);
        }

        /// <summary>
        /// Gets or sets the client ID
        /// </summary>
        public string ClientId
        {
            get => _clientId;
            set => this.RaiseAndSetIfChanged(ref _clientId, value);
        }

        /// <summary>
        /// Gets or sets the client secret
        /// </summary>
        public string ClientSecret
        {
            get => _clientSecret;
            set => this.RaiseAndSetIfChanged(ref _clientSecret, value);
        }

        /// <summary>
        /// Gets or sets the status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        /// <summary>
        /// Command to save the credentials
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveCredentialsCommand { get; }

        /// <summary>
        /// Command to authenticate
        /// </summary>
        public ReactiveCommand<Unit, Unit> AuthenticateCommand { get; }

        /// <summary>
        /// Command to revoke authentication
        /// </summary>
        public ReactiveCommand<Unit, Unit> RevokeAuthenticationCommand { get; }

        /// <summary>
        /// Command to synchronize all lessons
        /// </summary>
        public ReactiveCommand<Unit, Unit> SynchronizeAllLessonsCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarSettingsViewModel"/> class
        /// </summary>
        /// <param name="calendarService">The calendar service</param>
        /// <param name="calendarSyncService">The calendar sync service</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        public CalendarSettingsViewModel(
            ICalendarService calendarService,
            ICalendarSyncService calendarSyncService,
            ISecureStorageService secureStorageService,
            IConfiguration configuration,
            ILogger<CalendarSettingsViewModel> logger)
        {
            _calendarService = calendarService;
            _calendarSyncService = calendarSyncService;
            _secureStorageService = secureStorageService;
            _configuration = configuration;
            _logger = logger;

            // Initialize commands
            var canSaveCredentials = this.WhenAnyValue(
                x => x.ClientId,
                x => x.ClientSecret,
                (clientId, clientSecret) => !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret));

            SaveCredentialsCommand = ReactiveCommand.CreateFromTask(SaveCredentialsAsync, canSaveCredentials);
            AuthenticateCommand = ReactiveCommand.CreateFromTask(AuthenticateAsync);
            RevokeAuthenticationCommand = ReactiveCommand.CreateFromTask(RevokeAuthenticationAsync);
            SynchronizeAllLessonsCommand = ReactiveCommand.CreateFromTask(SynchronizeAllLessonsAsync, this.WhenAnyValue(x => x.IsAuthenticated));

            // Load initial values
            _ = LoadInitialValuesAsync();
        }

        /// <summary>
        /// Loads the initial values
        /// </summary>
        private async Task LoadInitialValuesAsync()
        {
            try
            {
                // Get the client ID and secret from configuration
                ClientId = _configuration["OAuth:Google:ClientId"] ?? string.Empty;
                ClientSecret = _configuration["OAuth:Google:ClientSecret"] ?? string.Empty;

                // Check if we're authenticated
                IsAuthenticated = await _calendarService.IsAuthenticatedAsync();
                StatusMessage = IsAuthenticated ? "Authenticated with Google Calendar" : "Not authenticated";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading initial values");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Saves the credentials
        /// </summary>
        private async Task SaveCredentialsAsync()
        {
            try
            {
                // Store the credentials in secure storage
                await _secureStorageService.StoreSecureValueAsync("google_calendar_client_id", ClientId);
                await _secureStorageService.StoreSecureValueAsync("google_calendar_client_secret", ClientSecret);

                // Update the configuration
                var configRoot = (IConfigurationRoot)_configuration;
                configRoot["OAuth:Google:ClientId"] = ClientId;
                configRoot["OAuth:Google:ClientSecret"] = ClientSecret;

                StatusMessage = "Credentials saved";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving credentials");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Authenticates with Google Calendar
        /// </summary>
        private async Task AuthenticateAsync()
        {
            try
            {
                IsAuthenticating = true;
                StatusMessage = "Authenticating...";

                var success = await _calendarService.AuthenticateAsync();
                IsAuthenticated = success;
                StatusMessage = success ? "Authentication successful" : "Authentication failed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating");
                StatusMessage = $"Error: {ex.Message}";
                IsAuthenticated = false;
            }
            finally
            {
                IsAuthenticating = false;
            }
        }

        /// <summary>
        /// Revokes authentication
        /// </summary>
        private async Task RevokeAuthenticationAsync()
        {
            try
            {
                StatusMessage = "Revoking authentication...";

                // Get the access token
                var accessToken = await _secureStorageService.RetrieveSecureValueAsync("google_access_token");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Revoke the token
                    var oauthService = _calendarService as IOAuthService;
                    if (oauthService != null)
                    {
                        await oauthService.RevokeTokenAsync(accessToken);
                    }
                }

                // Remove the tokens from secure storage
                await _secureStorageService.RemoveSecureValueAsync("google_access_token");
                await _secureStorageService.RemoveSecureValueAsync("google_refresh_token");
                await _secureStorageService.RemoveSecureValueAsync("google_token_expiry");

                IsAuthenticated = false;
                StatusMessage = "Authentication revoked";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking authentication");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Synchronizes all lessons with calendar events
        /// </summary>
        private async Task SynchronizeAllLessonsAsync()
        {
            try
            {
                StatusMessage = "Synchronizing lessons with calendar...";
                IsAuthenticating = true;

                // Check if we're authenticated
                var isAuthenticated = await _calendarService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    StatusMessage = "Not authenticated with Google Calendar. Please authenticate first.";
                    return;
                }

                // Synchronize all lessons
                var syncCount = await _calendarSyncService.SynchronizeAllLessonPlansAsync();

                // Show a message box with the result
                MessageBox.Show($"{syncCount} lessons synchronized with calendar.", "Synchronization Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                StatusMessage = $"{syncCount} lessons synchronized with calendar";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing lessons with calendar");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error synchronizing lessons: {ex.Message}", "Synchronization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsAuthenticating = false;
            }
        }
    }
}
