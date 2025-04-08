using Adept.Common.Interfaces;
using Adept.Common.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Adept.Services.Calendar
{
    /// <summary>
    /// Google Calendar service implementation
    /// </summary>
    public class GoogleCalendarService : ICalendarService
    {
        private readonly IOAuthService _oauthService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleCalendarService> _logger;
        private readonly string _baseUrl = "https://www.googleapis.com/calendar/v3";
        private bool _isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleCalendarService"/> class
        /// </summary>
        /// <param name="oauthService">The OAuth service</param>
        /// <param name="httpClient">The HTTP client</param>
        /// <param name="logger">The logger</param>
        public GoogleCalendarService(
            IOAuthService oauthService,
            HttpClient httpClient,
            ILogger<GoogleCalendarService> logger)
        {
            _oauthService = oauthService;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the calendar service
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                // Check if we're authenticated
                var isAuthenticated = await _oauthService.IsAuthenticatedAsync();
                if (isAuthenticated)
                {
                    _logger.LogInformation("Google Calendar service initialized with existing authentication");
                }
                else
                {
                    _logger.LogInformation("Google Calendar service initialized, but not authenticated");
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Google Calendar service");
                throw;
            }
        }

        /// <summary>
        /// Checks if the calendar service is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        public async Task<bool> IsAuthenticatedAsync()
        {
            return await _oauthService.IsAuthenticatedAsync();
        }

        /// <summary>
        /// Starts the OAuth authentication process
        /// </summary>
        /// <returns>True if authentication was successful, false otherwise</returns>
        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                await _oauthService.AuthenticateAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with Google Calendar");
                return false;
            }
        }

        /// <summary>
        /// Creates a calendar event
        /// </summary>
        /// <param name="summary">The event summary</param>
        /// <param name="description">The event description</param>
        /// <param name="location">The event location</param>
        /// <param name="startDateTime">The start date and time</param>
        /// <param name="endDateTime">The end date and time</param>
        /// <param name="timeZone">The timezone</param>
        /// <returns>The ID of the created event</returns>
        public async Task<string> CreateEventAsync(string summary, string description, string location, DateTime startDateTime, DateTime endDateTime, string timeZone = "Europe/London")
        {
            try
            {
                // Ensure we have a valid token
                var token = await _oauthService.GetValidTokenAsync();

                // Create the event data
                var eventData = new
                {
                    summary = summary,
                    description = description,
                    location = location,
                    start = new
                    {
                        dateTime = startDateTime.ToString("o"),
                        timeZone = timeZone
                    },
                    end = new
                    {
                        dateTime = endDateTime.ToString("o"),
                        timeZone = timeZone
                    }
                };

                // Create the request
                var json = JsonSerializer.Serialize(eventData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/calendars/primary/events");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                request.Content = content;

                // Send the request
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var eventId = responseData.GetProperty("id").GetString();

                _logger.LogInformation("Created calendar event: {Summary}", summary);
                return eventId ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar event: {Summary}", summary);
                throw;
            }
        }

        /// <summary>
        /// Updates a calendar event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="summary">The event summary</param>
        /// <param name="description">The event description</param>
        /// <param name="location">The event location</param>
        /// <param name="startDateTime">The start date and time</param>
        /// <param name="endDateTime">The end date and time</param>
        /// <param name="timeZone">The timezone</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public async Task<bool> UpdateEventAsync(string eventId, string summary, string description, string location, DateTime startDateTime, DateTime endDateTime, string timeZone = "Europe/London")
        {
            try
            {
                // Ensure we have a valid token
                var token = await _oauthService.GetValidTokenAsync();

                // Create the event data
                var eventData = new
                {
                    summary = summary,
                    description = description,
                    location = location,
                    start = new
                    {
                        dateTime = startDateTime.ToString("o"),
                        timeZone = timeZone
                    },
                    end = new
                    {
                        dateTime = endDateTime.ToString("o"),
                        timeZone = timeZone
                    }
                };

                // Create the request
                var json = JsonSerializer.Serialize(eventData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/calendars/primary/events/{eventId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                request.Content = content;

                // Send the request
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Updated calendar event: {Summary}", summary);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calendar event: {Summary}", summary);
                return false;
            }
        }

        /// <summary>
        /// Deletes a calendar event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public async Task<bool> DeleteEventAsync(string eventId)
        {
            try
            {
                // Ensure we have a valid token
                var token = await _oauthService.GetValidTokenAsync();

                // Create the request
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/calendars/primary/events/{eventId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                // Send the request
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Deleted calendar event: {EventId}", eventId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar event: {EventId}", eventId);
                return false;
            }
        }

        /// <summary>
        /// Gets events for a specific date
        /// </summary>
        /// <param name="date">The date in YYYY-MM-DD format</param>
        /// <returns>A collection of calendar events</returns>
        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(string date)
        {
            try
            {
                // Ensure we have a valid token
                var token = await _oauthService.GetValidTokenAsync();

                // Calculate the time range
                var timeMin = $"{date}T00:00:00Z";
                var timeMax = $"{date}T23:59:59Z";

                // Create the request URL
                var requestUrl = $"{_baseUrl}/calendars/primary/events" +
                    $"?timeMin={Uri.EscapeDataString(timeMin)}" +
                    $"&timeMax={Uri.EscapeDataString(timeMax)}" +
                    $"&singleEvents=true" +
                    $"&orderBy=startTime";

                // Create the request
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                // Send the request
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var events = responseData.GetProperty("items");

                // Convert to CalendarEvent objects
                var calendarEvents = new List<CalendarEvent>();
                foreach (var eventItem in events.EnumerateArray())
                {
                    calendarEvents.Add(ParseCalendarEvent(eventItem));
                }

                _logger.LogInformation("Retrieved {Count} calendar events for date: {Date}", calendarEvents.Count, date);
                return calendarEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar events for date: {Date}", date);
                return Enumerable.Empty<CalendarEvent>();
            }
        }

        /// <summary>
        /// Gets events for a date range
        /// </summary>
        /// <param name="startDate">The start date in YYYY-MM-DD format</param>
        /// <param name="endDate">The end date in YYYY-MM-DD format</param>
        /// <returns>A collection of calendar events</returns>
        public async Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(string startDate, string endDate)
        {
            try
            {
                // Ensure we have a valid token
                var token = await _oauthService.GetValidTokenAsync();

                // Calculate the time range
                var timeMin = $"{startDate}T00:00:00Z";
                var timeMax = $"{endDate}T23:59:59Z";

                // Create the request URL
                var requestUrl = $"{_baseUrl}/calendars/primary/events" +
                    $"?timeMin={Uri.EscapeDataString(timeMin)}" +
                    $"&timeMax={Uri.EscapeDataString(timeMax)}" +
                    $"&singleEvents=true" +
                    $"&orderBy=startTime";

                // Create the request
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                // Send the request
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var events = responseData.GetProperty("items");

                // Convert to CalendarEvent objects
                var calendarEvents = new List<CalendarEvent>();
                foreach (var eventItem in events.EnumerateArray())
                {
                    calendarEvents.Add(ParseCalendarEvent(eventItem));
                }

                _logger.LogInformation("Retrieved {Count} calendar events for date range: {StartDate} to {EndDate}",
                    calendarEvents.Count, startDate, endDate);
                return calendarEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar events for date range: {StartDate} to {EndDate}",
                    startDate, endDate);
                return Enumerable.Empty<CalendarEvent>();
            }
        }

        /// <summary>
        /// Gets the calendar ID for the primary calendar
        /// </summary>
        /// <returns>The calendar ID</returns>
        public async Task<string> GetPrimaryCalendarIdAsync()
        {
            try
            {
                // Ensure we have a valid token
                var token = await _oauthService.GetValidTokenAsync();

                // Create the request
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/me/calendarList");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                // Send the request
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var items = responseData.GetProperty("items");

                // Find the primary calendar
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("primary", out var primary) && primary.GetBoolean())
                    {
                        return item.GetProperty("id").GetString() ?? "primary";
                    }
                }

                // Default to "primary" if not found
                return "primary";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary calendar ID");
                return "primary";
            }
        }

        /// <summary>
        /// Parses a calendar event from a JSON element
        /// </summary>
        /// <param name="eventItem">The JSON element</param>
        /// <returns>The calendar event</returns>
        private CalendarEvent ParseCalendarEvent(JsonElement eventItem)
        {
            var calendarEvent = new CalendarEvent
            {
                Id = eventItem.GetProperty("id").GetString() ?? string.Empty,
                Summary = eventItem.GetProperty("summary").GetString() ?? string.Empty
            };

            // Optional properties
            if (eventItem.TryGetProperty("description", out var description))
            {
                calendarEvent.Description = description.GetString();
            }

            if (eventItem.TryGetProperty("location", out var location))
            {
                calendarEvent.Location = location.GetString();
            }

            if (eventItem.TryGetProperty("htmlLink", out var htmlLink))
            {
                calendarEvent.HtmlLink = htmlLink.GetString();
            }

            if (eventItem.TryGetProperty("status", out var status))
            {
                calendarEvent.Status = status.GetString();
            }

            // Start and end times
            if (eventItem.TryGetProperty("start", out var start))
            {
                calendarEvent.Start = ParseCalendarDateTime(start);
            }

            if (eventItem.TryGetProperty("end", out var end))
            {
                calendarEvent.End = ParseCalendarDateTime(end);
            }

            // Creator and organizer
            if (eventItem.TryGetProperty("creator", out var creator))
            {
                calendarEvent.Creator = ParseCalendarPerson(creator);
            }

            if (eventItem.TryGetProperty("organizer", out var organizer))
            {
                calendarEvent.Organizer = ParseCalendarPerson(organizer);
            }

            // Created and updated times
            if (eventItem.TryGetProperty("created", out var created) && created.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(created.GetString(), out var createdDate))
                {
                    calendarEvent.Created = createdDate;
                }
            }

            if (eventItem.TryGetProperty("updated", out var updated) && updated.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(updated.GetString(), out var updatedDate))
                {
                    calendarEvent.Updated = updatedDate;
                }
            }

            return calendarEvent;
        }

        /// <summary>
        /// Parses a calendar date and time from a JSON element
        /// </summary>
        /// <param name="element">The JSON element</param>
        /// <returns>The calendar date and time</returns>
        private CalendarDateTime ParseCalendarDateTime(JsonElement element)
        {
            var dateTime = new CalendarDateTime();

            if (element.TryGetProperty("dateTime", out var dateTimeValue))
            {
                dateTime.DateTime = dateTimeValue.GetString();
            }

            if (element.TryGetProperty("timeZone", out var timeZoneValue))
            {
                dateTime.TimeZone = timeZoneValue.GetString();
            }

            if (element.TryGetProperty("date", out var dateValue))
            {
                dateTime.Date = dateValue.GetString();
            }

            return dateTime;
        }

        /// <summary>
        /// Parses a calendar person from a JSON element
        /// </summary>
        /// <param name="element">The JSON element</param>
        /// <returns>The calendar person</returns>
        private CalendarPerson ParseCalendarPerson(JsonElement element)
        {
            var person = new CalendarPerson();

            if (element.TryGetProperty("id", out var idValue))
            {
                person.Id = idValue.GetString();
            }

            if (element.TryGetProperty("email", out var emailValue))
            {
                person.Email = emailValue.GetString();
            }

            if (element.TryGetProperty("displayName", out var displayNameValue))
            {
                person.DisplayName = displayNameValue.GetString();
            }

            if (element.TryGetProperty("self", out var selfValue) && selfValue.ValueKind == JsonValueKind.True)
            {
                person.Self = true;
            }

            return person;
        }
    }
}
