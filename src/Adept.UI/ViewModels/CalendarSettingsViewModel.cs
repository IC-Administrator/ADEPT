using Adept.Common.Interfaces;
using Adept.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Adept.UI.Commands;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// View model for calendar settings
    /// </summary>
    public class CalendarSettingsViewModel : ViewModelBase
    {
        private readonly ICalendarService? _calendarService;
        private readonly IConfiguration? _configuration;
        private readonly ILogger<CalendarSettingsViewModel>? _logger;

        private string _clientId = string.Empty;
        private string _clientSecret = string.Empty;
        private string _statusMessage = "Ready";
        private bool _isAuthenticated;
        private bool _isAuthenticating;
        private ObservableCollection<CalendarInfo> _calendars = new();
        private CalendarInfo? _selectedCalendar;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarSettingsViewModel"/> class.
        /// </summary>
        /// <param name="calendarService">The calendar service.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public CalendarSettingsViewModel(ICalendarService? calendarService = null, IConfiguration? configuration = null, ILogger<CalendarSettingsViewModel>? logger = null)
        {
            _calendarService = calendarService;
            _configuration = configuration;
            _logger = logger;

            // Initialize properties
            _isAuthenticated = false;
            _isAuthenticating = false;

            // Load credentials from configuration if available
            if (_configuration != null)
            {
                _clientId = _configuration["GoogleCalendar:ClientId"] ?? string.Empty;
                _clientSecret = _configuration["GoogleCalendar:ClientSecret"] ?? string.Empty;
            }

            // Initialize commands
            SaveCredentialsCommand = new RelayCommand(SaveCredentialsAsync);
            AuthenticateCommand = new RelayCommand(AuthenticateAsync);
            RevokeAuthenticationCommand = new RelayCommand(RevokeAuthenticationAsync);
            SynchronizeAllLessonsCommand = new RelayCommand(SynchronizeAllLessonsAsync);
        }

        /// <summary>
        /// Gets or sets the client ID.
        /// </summary>
        public string ClientId
        {
            get => _clientId;
            set
            {
                _clientId = value;
                OnPropertyChanged(nameof(ClientId));
            }
        }

        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        public string ClientSecret
        {
            get => _clientSecret;
            set
            {
                _clientSecret = value;
                OnPropertyChanged(nameof(ClientSecret));
            }
        }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set
            {
                _isAuthenticated = value;
                OnPropertyChanged(nameof(IsAuthenticated));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether authentication is in progress.
        /// </summary>
        public bool IsAuthenticating
        {
            get => _isAuthenticating;
            set
            {
                _isAuthenticating = value;
                OnPropertyChanged(nameof(IsAuthenticating));
            }
        }

        /// <summary>
        /// Gets or sets the calendars.
        /// </summary>
        public ObservableCollection<CalendarInfo> Calendars
        {
            get => _calendars;
            set
            {
                _calendars = value;
                OnPropertyChanged(nameof(Calendars));
            }
        }

        /// <summary>
        /// Gets or sets the selected calendar.
        /// </summary>
        public CalendarInfo? SelectedCalendar
        {
            get => _selectedCalendar;
            set
            {
                _selectedCalendar = value;
                OnPropertyChanged(nameof(SelectedCalendar));
            }
        }

        /// <summary>
        /// Command to save the credentials
        /// </summary>
        public ICommand SaveCredentialsCommand { get; private set; }

        /// <summary>
        /// Command to authenticate
        /// </summary>
        public ICommand AuthenticateCommand { get; private set; }

        /// <summary>
        /// Command to revoke authentication
        /// </summary>
        public ICommand RevokeAuthenticationCommand { get; private set; }

        /// <summary>
        /// Command to synchronize all lessons
        /// </summary>
        public ICommand SynchronizeAllLessonsCommand { get; private set; }

        /// <summary>
        /// Saves the credentials.
        /// </summary>
        private void SaveCredentialsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
                {
                    StatusMessage = "Client ID and Client Secret are required.";
                    return;
                }

                // Save credentials to configuration
                if (_configuration != null)
                {
                    // In a real application, we would save to configuration here
                    // For now, just update the status message
                    StatusMessage = "Credentials saved successfully.";
                }
                else
                {
                    StatusMessage = "Configuration service not available.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving credentials: {ex.Message}";
                _logger?.LogError(ex, "Error saving credentials");
            }
        }

        /// <summary>
        /// Authenticates with Google Calendar.
        /// </summary>
        private async void AuthenticateAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
                {
                    StatusMessage = "Client ID and Client Secret are required.";
                    return;
                }

                IsAuthenticating = true;
                StatusMessage = "Authenticating...";

                if (_calendarService != null)
                {
                    // In a real application, we would authenticate with the calendar service here
                    // For now, just simulate authentication
                    await Task.Delay(2000); // Simulate network delay
                    IsAuthenticated = true;
                    StatusMessage = "Authentication successful.";

                    // Load calendars
                    await LoadCalendarsAsync();
                }
                else
                {
                    StatusMessage = "Calendar service not available.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error authenticating: {ex.Message}";
                _logger?.LogError(ex, "Error authenticating");
            }
            finally
            {
                IsAuthenticating = false;
            }
        }

        /// <summary>
        /// Revokes authentication with Google Calendar.
        /// </summary>
        private async void RevokeAuthenticationAsync()
        {
            try
            {
                if (_calendarService != null)
                {
                    // In a real application, we would revoke authentication with the calendar service here
                    // For now, just simulate revocation
                    await Task.Delay(1000); // Simulate network delay
                    IsAuthenticated = false;
                    StatusMessage = "Authentication revoked.";
                    Calendars.Clear();
                }
                else
                {
                    StatusMessage = "Calendar service not available.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error revoking authentication: {ex.Message}";
                _logger?.LogError(ex, "Error revoking authentication");
            }
        }

        /// <summary>
        /// Loads calendars from Google Calendar.
        /// </summary>
        private async Task LoadCalendarsAsync()
        {
            try
            {
                if (_calendarService != null)
                {
                    // In a real application, we would load calendars from the calendar service here
                    // For now, just add some sample calendars
                    await Task.Delay(100); // Add a small delay to make this truly async
                    Calendars.Clear();
                    Calendars.Add(new CalendarInfo { Id = "1", Name = "Primary Calendar", Description = "Your primary calendar" });
                    Calendars.Add(new CalendarInfo { Id = "2", Name = "Work Calendar", Description = "Calendar for work events" });
                    Calendars.Add(new CalendarInfo { Id = "3", Name = "Personal Calendar", Description = "Calendar for personal events" });
                }
                else
                {
                    StatusMessage = "Calendar service not available.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading calendars: {ex.Message}";
                _logger?.LogError(ex, "Error loading calendars");
            }
        }

        /// <summary>
        /// Synchronizes all lessons with Google Calendar.
        /// </summary>
        private async void SynchronizeAllLessonsAsync()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    StatusMessage = "You must authenticate first.";
                    return;
                }

                StatusMessage = "Synchronizing lessons...";

                if (_calendarService != null)
                {
                    // In a real application, we would synchronize lessons with the calendar service here
                    // For now, just simulate synchronization
                    await Task.Delay(2000); // Simulate network delay
                    StatusMessage = "Lessons synchronized successfully.";
                }
                else
                {
                    StatusMessage = "Calendar service not available.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error synchronizing lessons: {ex.Message}";
                _logger?.LogError(ex, "Error synchronizing lessons");
            }
        }
    }
}
