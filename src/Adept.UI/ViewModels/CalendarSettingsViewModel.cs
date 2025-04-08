using Adept.Common.Interfaces;
using Adept.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private bool _useDefaultReminders = true;
        private int _reminderMinutes = 30;
        private string _reminderMethod = "Popup";
        private ObservableCollection<ColorItem> _colorPalette = new ObservableCollection<ColorItem>();
        private ColorItem? _selectedColor;
        private string _defaultVisibility = "Default";
        private string _attendeeEmail = string.Empty;
        private ObservableCollection<CalendarAttendee> _defaultAttendees = new ObservableCollection<CalendarAttendee>();
        private bool _enableAttachments = false;

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
        /// Command to save reminder settings
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveReminderSettingsCommand { get; }

        /// <summary>
        /// Command to save color settings
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveColorSettingsCommand { get; }

        /// <summary>
        /// Command to save sharing settings
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveSharingSettingsCommand { get; }

        /// <summary>
        /// Command to add an attendee
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddAttendeeCommand { get; }

        /// <summary>
        /// Command to remove an attendee
        /// </summary>
        public ReactiveCommand<CalendarAttendee, Unit> RemoveAttendeeCommand { get; }

        /// <summary>
        /// Gets or sets whether to use default reminders
        /// </summary>
        public bool UseDefaultReminders
        {
            get => _useDefaultReminders;
            set => this.RaiseAndSetIfChanged(ref _useDefaultReminders, value);
        }

        /// <summary>
        /// Gets or sets the reminder minutes
        /// </summary>
        public int ReminderMinutes
        {
            get => _reminderMinutes;
            set => this.RaiseAndSetIfChanged(ref _reminderMinutes, value);
        }

        /// <summary>
        /// Gets or sets the reminder method
        /// </summary>
        public string ReminderMethod
        {
            get => _reminderMethod;
            set => this.RaiseAndSetIfChanged(ref _reminderMethod, value);
        }

        /// <summary>
        /// Gets or sets the color palette
        /// </summary>
        public ObservableCollection<ColorItem> ColorPalette
        {
            get => _colorPalette;
            set => this.RaiseAndSetIfChanged(ref _colorPalette, value);
        }

        /// <summary>
        /// Gets or sets the selected color
        /// </summary>
        public ColorItem? SelectedColor
        {
            get => _selectedColor;
            set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
        }

        /// <summary>
        /// Gets or sets the default visibility
        /// </summary>
        public string DefaultVisibility
        {
            get => _defaultVisibility;
            set => this.RaiseAndSetIfChanged(ref _defaultVisibility, value);
        }

        /// <summary>
        /// Gets or sets the attendee email
        /// </summary>
        public string AttendeeEmail
        {
            get => _attendeeEmail;
            set => this.RaiseAndSetIfChanged(ref _attendeeEmail, value);
        }

        /// <summary>
        /// Gets or sets the default attendees
        /// </summary>
        public ObservableCollection<CalendarAttendee> DefaultAttendees
        {
            get => _defaultAttendees;
            set => this.RaiseAndSetIfChanged(ref _defaultAttendees, value);
        }

        /// <summary>
        /// Gets or sets whether to enable attachments
        /// </summary>
        public bool EnableAttachments
        {
            get => _enableAttachments;
            set => this.RaiseAndSetIfChanged(ref _enableAttachments, value);
        }

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
            SaveReminderSettingsCommand = ReactiveCommand.CreateFromTask(SaveReminderSettingsAsync, this.WhenAnyValue(x => x.IsAuthenticated));
            SaveColorSettingsCommand = ReactiveCommand.CreateFromTask(SaveColorSettingsAsync, this.WhenAnyValue(x => x.IsAuthenticated));
            SaveSharingSettingsCommand = ReactiveCommand.CreateFromTask(SaveSharingSettingsAsync, this.WhenAnyValue(x => x.IsAuthenticated));

            var canAddAttendee = this.WhenAnyValue(
                x => x.AttendeeEmail,
                x => x.IsAuthenticated,
                (email, isAuthenticated) => !string.IsNullOrWhiteSpace(email) && isAuthenticated);
            AddAttendeeCommand = ReactiveCommand.Create(AddAttendee, canAddAttendee);
            RemoveAttendeeCommand = ReactiveCommand.Create<CalendarAttendee>(RemoveAttendee);

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

                // Load color palette if authenticated
                if (IsAuthenticated)
                {
                    await LoadColorPaletteAsync();
                    await LoadSavedSettingsAsync();
                }
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

        /// <summary>
        /// Loads the color palette
        /// </summary>
        private async Task LoadColorPaletteAsync()
        {
            try
            {
                var colorPalette = await _calendarService.GetColorPaletteAsync();
                ColorPalette.Clear();

                // Add a default color
                ColorPalette.Add(new ColorItem { Id = string.Empty, Name = "Default", Background = "#FFFFFF" });

                // Add the colors from the API
                foreach (var color in colorPalette)
                {
                    ColorPalette.Add(new ColorItem
                    {
                        Id = color.Key,
                        Name = $"Color {color.Key}",
                        Background = color.Value.Background,
                        Foreground = color.Value.Foreground
                    });
                }

                // Select the default color
                SelectedColor = ColorPalette.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading color palette");
                StatusMessage = $"Error loading color palette: {ex.Message}";
            }
        }

        /// <summary>
        /// Loads the saved settings
        /// </summary>
        private async Task LoadSavedSettingsAsync()
        {
            try
            {
                // Load reminder settings
                var useDefaultRemindersStr = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_use_default_reminders");
                if (!string.IsNullOrEmpty(useDefaultRemindersStr) && bool.TryParse(useDefaultRemindersStr, out var useDefaultReminders))
                {
                    UseDefaultReminders = useDefaultReminders;
                }

                var reminderMinutesStr = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_reminder_minutes");
                if (!string.IsNullOrEmpty(reminderMinutesStr) && int.TryParse(reminderMinutesStr, out var reminderMinutes))
                {
                    ReminderMinutes = reminderMinutes;
                }

                var reminderMethod = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_reminder_method");
                if (!string.IsNullOrEmpty(reminderMethod))
                {
                    ReminderMethod = reminderMethod;
                }

                // Load color settings
                var colorId = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_color_id");
                if (!string.IsNullOrEmpty(colorId))
                {
                    SelectedColor = ColorPalette.FirstOrDefault(c => c.Id == colorId) ?? ColorPalette.FirstOrDefault();
                }

                // Load visibility settings
                var visibility = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_visibility");
                if (!string.IsNullOrEmpty(visibility))
                {
                    DefaultVisibility = visibility;
                }

                // Load attendees
                var attendeesJson = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_attendees");
                if (!string.IsNullOrEmpty(attendeesJson))
                {
                    var attendees = System.Text.Json.JsonSerializer.Deserialize<List<CalendarAttendee>>(attendeesJson);
                    if (attendees != null)
                    {
                        DefaultAttendees.Clear();
                        foreach (var attendee in attendees)
                        {
                            DefaultAttendees.Add(attendee);
                        }
                    }
                }

                // Load attachment settings
                var enableAttachmentsStr = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_enable_attachments");
                if (!string.IsNullOrEmpty(enableAttachmentsStr) && bool.TryParse(enableAttachmentsStr, out var enableAttachments))
                {
                    EnableAttachments = enableAttachments;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading saved settings");
                StatusMessage = $"Error loading saved settings: {ex.Message}";
            }
        }

        /// <summary>
        /// Saves the reminder settings
        /// </summary>
        private async Task SaveReminderSettingsAsync()
        {
            try
            {
                // Save the settings
                await _secureStorageService.StoreSecureValueAsync("google_calendar_use_default_reminders", UseDefaultReminders.ToString());
                await _secureStorageService.StoreSecureValueAsync("google_calendar_reminder_minutes", ReminderMinutes.ToString());
                await _secureStorageService.StoreSecureValueAsync("google_calendar_reminder_method", ReminderMethod);

                StatusMessage = "Reminder settings saved";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving reminder settings");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Saves the color settings
        /// </summary>
        private async Task SaveColorSettingsAsync()
        {
            try
            {
                // Save the settings
                await _secureStorageService.StoreSecureValueAsync("google_calendar_color_id", SelectedColor?.Id ?? string.Empty);

                StatusMessage = "Color settings saved";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving color settings");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Saves the sharing settings
        /// </summary>
        private async Task SaveSharingSettingsAsync()
        {
            try
            {
                // Save the settings
                await _secureStorageService.StoreSecureValueAsync("google_calendar_visibility", DefaultVisibility);

                // Save attendees
                var attendeesJson = System.Text.Json.JsonSerializer.Serialize(DefaultAttendees);
                await _secureStorageService.StoreSecureValueAsync("google_calendar_attendees", attendeesJson);

                // Save attachment settings
                await _secureStorageService.StoreSecureValueAsync("google_calendar_enable_attachments", EnableAttachments.ToString());

                StatusMessage = "Sharing settings saved";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving sharing settings");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds an attendee
        /// </summary>
        private void AddAttendee()
        {
            if (string.IsNullOrWhiteSpace(AttendeeEmail))
            {
                return;
            }

            // Check if the attendee already exists
            if (DefaultAttendees.Any(a => a.Email == AttendeeEmail))
            {
                StatusMessage = $"Attendee {AttendeeEmail} already exists";
                return;
            }

            // Add the attendee
            DefaultAttendees.Add(new CalendarAttendee
            {
                Email = AttendeeEmail,
                ResponseStatus = "needsAction"
            });

            // Clear the email field
            AttendeeEmail = string.Empty;
        }

        /// <summary>
        /// Removes an attendee
        /// </summary>
        private void RemoveAttendee(CalendarAttendee attendee)
        {
            DefaultAttendees.Remove(attendee);
        }
    }

    /// <summary>
    /// Represents a color item for the color palette
    /// </summary>
    public class ColorItem
    {
        /// <summary>
        /// Gets or sets the color ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the color name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        public string Background { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        public string Foreground { get; set; } = string.Empty;
    }
}
