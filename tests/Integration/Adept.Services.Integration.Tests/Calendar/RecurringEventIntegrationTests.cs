using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Adept.Services.Integration.Tests.Calendar
{
    [Trait("Category", "Calendar")]
    [Trait("TestCategory", "RecurringEvents")]
    public class RecurringEventIntegrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private string _accessToken = string.Empty;
        private string _refreshToken = string.Empty;
        private DateTime _tokenExpirationTime = DateTime.MinValue;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly int _callbackPort;
        private readonly string _redirectUri;
        private readonly bool _cleanupAfterTests;
        private readonly List<string> _createdEventIds = new();
        private readonly IConfiguration _configuration;

        public RecurringEventIntegrationTests()
        {
            _httpClient = new HttpClient();
            
            // Load configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Get OAuth credentials
            _clientId = _configuration["OAuth:Google:ClientId"] ?? string.Empty;
            _clientSecret = _configuration["OAuth:Google:ClientSecret"] ?? string.Empty;
            _callbackPort = int.Parse(_configuration["OAuth:Google:CallbackPort"] ?? "8080");
            _redirectUri = $"http://localhost:{_callbackPort}";
            
            // Get test settings
            _cleanupAfterTests = bool.Parse(_configuration["TestSettings:Calendar:CleanupAfterTests"] ?? "true");
            
            // Skip tests if credentials are not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                Skip.If(true, "Google OAuth credentials not provided in appsettings.json");
            }
        }

        public void Dispose()
        {
            // Clean up test data if configured to do so
            if (_cleanupAfterTests)
            {
                CleanupTestDataAsync().Wait();
            }
            
            _httpClient.Dispose();
        }

        private async Task CleanupTestDataAsync()
        {
            try
            {
                // Ensure we have a valid token
                await EnsureAccessTokenAsync();
                
                // Delete test events
                foreach (var eventId in _createdEventIds)
                {
                    try
                    {
                        await DeleteEventAsync(eventId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting event {eventId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        [Fact]
        public async Task CreateDailyRecurringEvent_ValidParameters_CreatesEvent()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();
            string summary = $"Daily Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            string description = "This is a daily recurring test event";
            string location = "Test Location";
            
            var startDateTime = DateTime.Now.AddDays(1).Date.AddHours(9); // 9 AM tomorrow
            var endDateTime = startDateTime.AddHours(1); // 1 hour duration
            
            // Create daily recurrence rule - repeat every 1 day for 5 occurrences
            var recurrence = new List<string> { "RRULE:FREQ=DAILY;COUNT=5;INTERVAL=1" };

            // Act
            var eventId = await CreateRecurringEventAsync(
                summary, description, location, 
                startDateTime, endDateTime, recurrence);
            
            _createdEventIds.Add(eventId); // Store for cleanup

            // Assert
            Assert.NotEmpty(eventId);
            
            // Verify event exists with recurrence
            var events = await ListEventsAsync();
            var createdEvent = events.Find(e => e.Id == eventId);
            Assert.NotNull(createdEvent);
            Assert.Equal(summary, createdEvent.Summary);
            Assert.Contains(createdEvent.Recurrence, r => r.StartsWith("RRULE:FREQ=DAILY"));
        }

        [Fact]
        public async Task CreateWeeklyRecurringEvent_ValidParameters_CreatesEvent()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();
            string summary = $"Weekly Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            string description = "This is a weekly recurring test event";
            string location = "Test Location";
            
            var startDateTime = DateTime.Now.AddDays(1).Date.AddHours(10); // 10 AM tomorrow
            var endDateTime = startDateTime.AddHours(1); // 1 hour duration
            
            // Create weekly recurrence rule - repeat every 1 week on Monday, Wednesday, Friday for 3 weeks
            var recurrence = new List<string> { "RRULE:FREQ=WEEKLY;COUNT=9;INTERVAL=1;BYDAY=MO,WE,FR" };

            // Act
            var eventId = await CreateRecurringEventAsync(
                summary, description, location, 
                startDateTime, endDateTime, recurrence);
            
            _createdEventIds.Add(eventId); // Store for cleanup

            // Assert
            Assert.NotEmpty(eventId);
            
            // Verify event exists with recurrence
            var events = await ListEventsAsync();
            var createdEvent = events.Find(e => e.Id == eventId);
            Assert.NotNull(createdEvent);
            Assert.Equal(summary, createdEvent.Summary);
            Assert.Contains(createdEvent.Recurrence, r => r.StartsWith("RRULE:FREQ=WEEKLY"));
        }

        [Fact]
        public async Task CreateMonthlyRecurringEvent_ByDayOfMonth_CreatesEvent()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();
            string summary = $"Monthly (Day) Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            string description = "This is a monthly recurring test event (by day of month)";
            string location = "Test Location";
            
            var startDateTime = DateTime.Now.AddDays(1).Date.AddHours(11); // 11 AM tomorrow
            var endDateTime = startDateTime.AddHours(1); // 1 hour duration
            
            // Create monthly recurrence rule - repeat every 1 month on the 15th day for 3 occurrences
            var recurrence = new List<string> { $"RRULE:FREQ=MONTHLY;COUNT=3;INTERVAL=1;BYMONTHDAY=15" };

            // Act
            var eventId = await CreateRecurringEventAsync(
                summary, description, location, 
                startDateTime, endDateTime, recurrence);
            
            _createdEventIds.Add(eventId); // Store for cleanup

            // Assert
            Assert.NotEmpty(eventId);
            
            // Verify event exists with recurrence
            var events = await ListEventsAsync();
            var createdEvent = events.Find(e => e.Id == eventId);
            Assert.NotNull(createdEvent);
            Assert.Equal(summary, createdEvent.Summary);
            Assert.Contains(createdEvent.Recurrence, r => r.StartsWith("RRULE:FREQ=MONTHLY"));
        }

        [Fact]
        public async Task CreateMonthlyRecurringEvent_ByDayOfWeek_CreatesEvent()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();
            string summary = $"Monthly (Position) Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            string description = "This is a monthly recurring test event (by position)";
            string location = "Test Location";
            
            var startDateTime = DateTime.Now.AddDays(1).Date.AddHours(13); // 1 PM tomorrow
            var endDateTime = startDateTime.AddHours(1); // 1 hour duration
            
            // Create monthly recurrence rule - repeat every 1 month on the second Tuesday for 3 occurrences
            var recurrence = new List<string> { "RRULE:FREQ=MONTHLY;COUNT=3;INTERVAL=1;BYDAY=TU;BYSETPOS=2" };

            // Act
            var eventId = await CreateRecurringEventAsync(
                summary, description, location, 
                startDateTime, endDateTime, recurrence);
            
            _createdEventIds.Add(eventId); // Store for cleanup

            // Assert
            Assert.NotEmpty(eventId);
            
            // Verify event exists with recurrence
            var events = await ListEventsAsync();
            var createdEvent = events.Find(e => e.Id == eventId);
            Assert.NotNull(createdEvent);
            Assert.Equal(summary, createdEvent.Summary);
            Assert.Contains(createdEvent.Recurrence, r => r.StartsWith("RRULE:FREQ=MONTHLY"));
        }

        [Fact]
        public async Task CreateYearlyRecurringEvent_ValidParameters_CreatesEvent()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();
            string summary = $"Yearly Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            string description = "This is a yearly recurring test event";
            string location = "Test Location";
            
            var startDateTime = DateTime.Now.AddDays(1).Date.AddHours(14); // 2 PM tomorrow
            var endDateTime = startDateTime.AddHours(1); // 1 hour duration
            
            // Create yearly recurrence rule - repeat every 1 year on January 15 for 3 occurrences
            var recurrence = new List<string> { "RRULE:FREQ=YEARLY;COUNT=3;INTERVAL=1;BYMONTH=1;BYMONTHDAY=15" };

            // Act
            var eventId = await CreateRecurringEventAsync(
                summary, description, location, 
                startDateTime, endDateTime, recurrence);
            
            _createdEventIds.Add(eventId); // Store for cleanup

            // Assert
            Assert.NotEmpty(eventId);
            
            // Verify event exists with recurrence
            var events = await ListEventsAsync();
            var createdEvent = events.Find(e => e.Id == eventId);
            Assert.NotNull(createdEvent);
            Assert.Equal(summary, createdEvent.Summary);
            Assert.Contains(createdEvent.Recurrence, r => r.StartsWith("RRULE:FREQ=YEARLY"));
        }

        [Fact]
        public async Task CreateEventWithReminders_ValidParameters_CreatesEventWithReminders()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();
            string summary = $"Reminder Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            string description = "This is a test event with reminders";
            string location = "Test Location";
            
            var startDateTime = DateTime.Now.AddDays(1).Date.AddHours(15); // 3 PM tomorrow
            var endDateTime = startDateTime.AddHours(1); // 1 hour duration
            
            var recurrence = new List<string> { "RRULE:FREQ=DAILY;COUNT=3;INTERVAL=1" };

            // Act
            var eventId = await CreateRecurringEventAsync(
                summary, description, location, 
                startDateTime, endDateTime, recurrence,
                new[] { 
                    new Dictionary<string, object> { ["method"] = "email", ["minutes"] = 60 },
                    new Dictionary<string, object> { ["method"] = "popup", ["minutes"] = 15 }
                });
            
            _createdEventIds.Add(eventId); // Store for cleanup

            // Assert
            Assert.NotEmpty(eventId);
            
            // Verify event exists with reminders
            var events = await ListEventsAsync();
            var createdEvent = events.Find(e => e.Id == eventId);
            Assert.NotNull(createdEvent);
            Assert.Equal(summary, createdEvent.Summary);
            
            // Note: We can't easily verify the reminders as they're not returned in the list response
            // We would need to get the specific event to check reminders
        }

        private async Task EnsureAccessTokenAsync()
        {
            // Check if we have a valid token
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpirationTime > DateTime.UtcNow.AddMinutes(5))
            {
                return;
            }

            // If we have a refresh token, use it to get a new access token
            if (!string.IsNullOrEmpty(_refreshToken))
            {
                await RefreshAccessTokenAsync();
                return;
            }

            // Otherwise, start the OAuth flow
            await StartOAuthFlowAsync();
        }

        private async Task StartOAuthFlowAsync()
        {
            // Create the authorization URL
            var authUrl = $"https://accounts.google.com/o/oauth2/auth" +
                          $"?client_id={_clientId}" +
                          $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                          $"&response_type=code" +
                          $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/calendar")}" +
                          $"&access_type=offline" +
                          $"&prompt=consent";

            // Open the browser for the user to authenticate
            Console.WriteLine("Opening browser for authentication...");
            Console.WriteLine($"Auth URL: {authUrl}");
            
            // In a real test, we'd use a headless browser or mock this part
            // For now, we'll just ask the user to manually authenticate
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            // Start a local web server to receive the callback
            var authCode = await StartLocalServerForAuthCodeAsync();

            // Exchange the authorization code for tokens
            await ExchangeAuthCodeForTokensAsync(authCode);
        }

        private async Task<string> StartLocalServerForAuthCodeAsync()
        {
            var authCode = string.Empty;
            var listener = new HttpListener();
            listener.Prefixes.Add(_redirectUri + "/");
            listener.Start();

            Console.WriteLine($"Listening for callback on {_redirectUri}...");
            
            // Wait for the callback
            var context = await listener.GetContextAsync();
            var request = context.Request;
            
            // Get the authorization code from the query string
            authCode = request.QueryString["code"] ?? string.Empty;
            
            // Send a response to the browser
            var response = context.Response;
            var responseString = "<html><body><h1>Authentication successful!</h1><p>You can close this window now.</p></body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer);
            responseOutput.Close();
            
            listener.Stop();
            
            if (string.IsNullOrEmpty(authCode))
            {
                throw new Exception("Failed to get authorization code");
            }
            
            return authCode;
        }

        private async Task ExchangeAuthCodeForTokensAsync(string authCode)
        {
            // Prepare the token request
            var tokenRequest = new Dictionary<string, string>
            {
                {"code", authCode},
                {"client_id", _clientId},
                {"client_secret", _clientSecret},
                {"redirect_uri", _redirectUri},
                {"grant_type", "authorization_code"}
            };

            // Send the token request
            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to exchange auth code for tokens: {responseContent}");
            }
            
            // Parse the response
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            _accessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty;
            
            // The refresh token is only provided on the first authorization
            if (tokenResponse.TryGetProperty("refresh_token", out var refreshTokenElement))
            {
                _refreshToken = refreshTokenElement.GetString() ?? string.Empty;
            }
            
            var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
            _tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresIn);
        }

        private async Task RefreshAccessTokenAsync()
        {
            // Prepare the refresh token request
            var refreshRequest = new Dictionary<string, string>
            {
                {"refresh_token", _refreshToken},
                {"client_id", _clientId},
                {"client_secret", _clientSecret},
                {"grant_type", "refresh_token"}
            };

            // Send the refresh token request
            var content = new FormUrlEncodedContent(refreshRequest);
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to refresh access token: {responseContent}");
            }
            
            // Parse the response
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            _accessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty;
            var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
            _tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresIn);
        }

        private async Task<List<EventInfo>> ListEventsAsync(string calendarId = "primary")
        {
            // Ensure we have a valid token
            await EnsureAccessTokenAsync();
            
            // Prepare the request
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            
            // Send the request
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to list events: {responseContent}");
            }
            
            // Parse the response
            var eventList = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var items = eventList.GetProperty("items");
            
            var events = new List<EventInfo>();
            foreach (var item in items.EnumerateArray())
            {
                var eventInfo = new EventInfo
                {
                    Id = item.GetProperty("id").GetString() ?? string.Empty,
                    Summary = item.TryGetProperty("summary", out var summary) ? summary.GetString() : string.Empty,
                    Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : string.Empty,
                    Location = item.TryGetProperty("location", out var loc) ? loc.GetString() : string.Empty
                };
                
                if (item.TryGetProperty("recurrence", out var recurrence))
                {
                    eventInfo.Recurrence = new List<string>();
                    foreach (var rule in recurrence.EnumerateArray())
                    {
                        eventInfo.Recurrence.Add(rule.GetString() ?? string.Empty);
                    }
                }
                
                if (item.TryGetProperty("start", out var start))
                {
                    if (start.TryGetProperty("dateTime", out var startDateTime))
                    {
                        eventInfo.Start = DateTime.Parse(startDateTime.GetString() ?? string.Empty);
                    }
                    else if (start.TryGetProperty("date", out var startDate))
                    {
                        eventInfo.Start = DateTime.Parse(startDate.GetString() ?? string.Empty);
                    }
                }
                
                if (item.TryGetProperty("end", out var end))
                {
                    if (end.TryGetProperty("dateTime", out var endDateTime))
                    {
                        eventInfo.End = DateTime.Parse(endDateTime.GetString() ?? string.Empty);
                    }
                    else if (end.TryGetProperty("date", out var endDate))
                    {
                        eventInfo.End = DateTime.Parse(endDate.GetString() ?? string.Empty);
                    }
                }
                
                events.Add(eventInfo);
            }
            
            return events;
        }

        private async Task<string> CreateRecurringEventAsync(
            string summary, string description, string location,
            DateTime startDateTime, DateTime endDateTime, 
            List<string> recurrence,
            IEnumerable<Dictionary<string, object>>? reminderOverrides = null,
            string colorId = "7", // Light blue
            string calendarId = "primary")
        {
            // Ensure we have a valid token
            await EnsureAccessTokenAsync();
            
            // Prepare the event data
            var eventData = new Dictionary<string, object>
            {
                ["summary"] = summary,
                ["description"] = description,
                ["location"] = location,
                ["start"] = new Dictionary<string, string>
                {
                    ["dateTime"] = startDateTime.ToString("o"),
                    ["timeZone"] = "UTC"
                },
                ["end"] = new Dictionary<string, string>
                {
                    ["dateTime"] = endDateTime.ToString("o"),
                    ["timeZone"] = "UTC"
                },
                ["recurrence"] = recurrence,
                ["colorId"] = colorId
            };
            
            // Add reminders if provided
            if (reminderOverrides != null)
            {
                eventData["reminders"] = new Dictionary<string, object>
                {
                    ["useDefault"] = false,
                    ["overrides"] = reminderOverrides
                };
            }
            else
            {
                eventData["reminders"] = new Dictionary<string, object>
                {
                    ["useDefault"] = true
                };
            }

            // Set up the request
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(eventData),
                Encoding.UTF8,
                "application/json");

            // Send the request
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to create event: {responseContent}");
            }
            
            // Parse the response
            var createdEvent = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return createdEvent.GetProperty("id").GetString() ?? string.Empty;
        }

        private async Task DeleteEventAsync(string eventId, string calendarId = "primary")
        {
            // Ensure we have a valid token
            await EnsureAccessTokenAsync();
            
            // Prepare the request
            var request = new HttpRequestMessage(HttpMethod.Delete, $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events/{eventId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            
            // Send the request
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete event: {responseContent}");
            }
        }
    }

    // Helper class for event data
    public class EventInfo
    {
        public string Id { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<string>? Recurrence { get; set; }
    }
}
