using Adept.Common.Interfaces;
using Adept.Common.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Adept.Services.Calendar
{
    /// <summary>
    /// Service for two-way synchronization with Google Calendar
    /// </summary>
    public class GoogleCalendarSyncService
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<GoogleCalendarSyncService> _logger;
        private readonly HttpClient _httpClient;
        private Timer _syncTimer;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(15);
        private readonly List<CalendarSyncHandler> _syncHandlers = new();
        private string _syncToken;
        private bool _isSyncing;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleCalendarSyncService"/> class
        /// </summary>
        /// <param name="calendarService">The calendar service</param>
        /// <param name="httpClient">The HTTP client</param>
        /// <param name="logger">The logger</param>
        public GoogleCalendarSyncService(
            ICalendarService calendarService,
            HttpClient httpClient,
            ILogger<GoogleCalendarSyncService> logger)
        {
            _calendarService = calendarService;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Starts the synchronization service
        /// </summary>
        public async Task StartAsync()
        {
            _logger.LogInformation("Starting Google Calendar sync service");

            // Initialize the calendar service
            await _calendarService.InitializeAsync();

            // Check if we're authenticated
            var isAuthenticated = await _calendarService.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                _logger.LogWarning("Not authenticated with Google Calendar. Sync service will not start.");
                return;
            }

            // Get the initial sync token
            await GetInitialSyncTokenAsync();

            // Start the sync timer
            _syncTimer = new Timer(SyncCallback, null, TimeSpan.Zero, _syncInterval);

            _logger.LogInformation("Google Calendar sync service started");
        }

        /// <summary>
        /// Stops the synchronization service
        /// </summary>
        public Task StopAsync()
        {
            _logger.LogInformation("Stopping Google Calendar sync service");

            // Stop the sync timer
            _syncTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _syncTimer?.Dispose();
            _syncTimer = null;

            _logger.LogInformation("Google Calendar sync service stopped");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Registers a sync handler
        /// </summary>
        /// <param name="handler">The handler to register</param>
        public void RegisterSyncHandler(CalendarSyncHandler handler)
        {
            _syncHandlers.Add(handler);
            _logger.LogInformation("Registered calendar sync handler");
        }

        /// <summary>
        /// Unregisters a sync handler
        /// </summary>
        /// <param name="handler">The handler to unregister</param>
        public void UnregisterSyncHandler(CalendarSyncHandler handler)
        {
            _syncHandlers.Remove(handler);
            _logger.LogInformation("Unregistered calendar sync handler");
        }

        /// <summary>
        /// Triggers a manual synchronization
        /// </summary>
        public async Task SyncNowAsync()
        {
            await SyncChangesAsync();
        }

        /// <summary>
        /// Gets the initial sync token
        /// </summary>
        private async Task GetInitialSyncTokenAsync()
        {
            try
            {
                _logger.LogInformation("Getting initial sync token");

                // Get the primary calendar ID
                var calendarId = await _calendarService.GetPrimaryCalendarIdAsync();

                // Get a sync token for the calendar
                var accessToken = await GetAccessTokenAsync();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events?maxResults=1&showDeleted=true&syncToken=");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);

                if (data.TryGetProperty("nextSyncToken", out var syncTokenElement))
                {
                    _syncToken = syncTokenElement.GetString();
                    _logger.LogInformation("Initial sync token obtained");
                }
                else
                {
                    _logger.LogWarning("Failed to get initial sync token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting initial sync token");
            }
        }

        /// <summary>
        /// Callback for the sync timer
        /// </summary>
        private async void SyncCallback(object state)
        {
            await SyncChangesAsync();
        }

        /// <summary>
        /// Synchronizes changes from Google Calendar
        /// </summary>
        private async Task SyncChangesAsync()
        {
            // Prevent concurrent syncs
            if (_isSyncing)
            {
                _logger.LogInformation("Sync already in progress, skipping");
                return;
            }

            _isSyncing = true;

            try
            {
                _logger.LogInformation("Syncing changes from Google Calendar");

                // Check if we have a sync token
                if (string.IsNullOrEmpty(_syncToken))
                {
                    _logger.LogWarning("No sync token available, getting initial sync token");
                    await GetInitialSyncTokenAsync();
                    return;
                }

                // Get the primary calendar ID
                var calendarId = await _calendarService.GetPrimaryCalendarIdAsync();

                // Get changes since the last sync
                var accessToken = await GetAccessTokenAsync();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events?showDeleted=true&syncToken={_syncToken}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                // Handle sync token expiration
                if (response.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    _logger.LogWarning("Sync token expired, getting new sync token");
                    _syncToken = null;
                    await GetInitialSyncTokenAsync();
                    return;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);

                // Update the sync token
                if (data.TryGetProperty("nextSyncToken", out var syncTokenElement))
                {
                    _syncToken = syncTokenElement.GetString();
                }

                // Process the changes
                if (data.TryGetProperty("items", out var itemsElement))
                {
                    var changedEvents = new List<CalendarEvent>();
                    var deletedEventIds = new List<string>();

                    foreach (var item in itemsElement.EnumerateArray())
                    {
                        // Check if the event is deleted
                        if (item.TryGetProperty("status", out var statusElement) && statusElement.GetString() == "cancelled")
                        {
                            var eventId = item.GetProperty("id").GetString();
                            deletedEventIds.Add(eventId);
                            _logger.LogInformation("Event deleted: {EventId}", eventId);
                        }
                        else
                        {
                            // Convert the event to our model
                            var calendarEvent = ConvertToCalendarEvent(item);
                            changedEvents.Add(calendarEvent);
                            _logger.LogInformation("Event changed: {EventId}", calendarEvent.Id);
                        }
                    }

                    // Notify handlers of changes
                    foreach (var handler in _syncHandlers)
                    {
                        try
                        {
                            await handler(changedEvents, deletedEventIds);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in sync handler");
                        }
                    }

                    _logger.LogInformation("Processed {ChangedCount} changed events and {DeletedCount} deleted events", changedEvents.Count, deletedEventIds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing changes from Google Calendar");
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// Gets the access token
        /// </summary>
        private async Task<string> GetAccessTokenAsync()
        {
            // This is a simplified implementation. In a real application, you would use the OAuth service.
            var token = await _calendarService.GetAccessTokenAsync();
            return token;
        }

        /// <summary>
        /// Converts a Google Calendar event to our model
        /// </summary>
        private CalendarEvent ConvertToCalendarEvent(JsonElement eventElement)
        {
            var calendarEvent = new CalendarEvent
            {
                Id = eventElement.GetProperty("id").GetString() ?? string.Empty,
                Summary = eventElement.GetProperty("summary").GetString() ?? string.Empty
            };

            if (eventElement.TryGetProperty("description", out var descriptionElement))
            {
                calendarEvent.Description = descriptionElement.GetString() ?? string.Empty;
            }

            if (eventElement.TryGetProperty("location", out var locationElement))
            {
                calendarEvent.Location = locationElement.GetString() ?? string.Empty;
            }

            if (eventElement.TryGetProperty("start", out var startElement))
            {
                calendarEvent.Start = new EventDateTime();
                if (startElement.TryGetProperty("dateTime", out var startDateTimeElement))
                {
                    calendarEvent.Start.DateTime = startDateTimeElement.GetString();
                }
                else if (startElement.TryGetProperty("date", out var startDateElement))
                {
                    calendarEvent.Start.Date = startDateElement.GetString();
                }
            }

            if (eventElement.TryGetProperty("end", out var endElement))
            {
                calendarEvent.End = new EventDateTime();
                if (endElement.TryGetProperty("dateTime", out var endDateTimeElement))
                {
                    calendarEvent.End.DateTime = endDateTimeElement.GetString();
                }
                else if (endElement.TryGetProperty("date", out var endDateElement))
                {
                    calendarEvent.End.Date = endDateElement.GetString();
                }
            }

            if (eventElement.TryGetProperty("htmlLink", out var htmlLinkElement))
            {
                calendarEvent.HtmlLink = htmlLinkElement.GetString();
            }

            if (eventElement.TryGetProperty("colorId", out var colorIdElement))
            {
                calendarEvent.ColorId = colorIdElement.GetString();
            }

            if (eventElement.TryGetProperty("recurrence", out var recurrenceElement))
            {
                calendarEvent.RecurrenceRules = new List<string>();
                foreach (var rule in recurrenceElement.EnumerateArray())
                {
                    calendarEvent.RecurrenceRules.Add(rule.GetString() ?? string.Empty);
                }
            }

            return calendarEvent;
        }
    }
}
