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
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Adept.Services.Integration.Tests.Calendar
{
    [Trait("Category", "Calendar")]
    public class GoogleCalendarIntegrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private string _accessToken = string.Empty;
        private string _refreshToken = string.Empty;
        private DateTime _tokenExpirationTime = DateTime.MinValue;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly int _callbackPort;
        private readonly string _redirectUri;
        private readonly string _testCalendarName;
        private readonly bool _cleanupAfterTests;
        private string _testCalendarId = string.Empty;
        private readonly List<string> _createdEventIds = new();
        private readonly IConfiguration _configuration;

        public GoogleCalendarIntegrationTests()
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
            _testCalendarName = _configuration["TestSettings:Calendar:TestCalendarName"] ?? "ADEPT Test Calendar";
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
                
                // Delete test calendar if created
                if (!string.IsNullOrEmpty(_testCalendarId))
                {
                    try
                    {
                        await DeleteCalendarAsync(_testCalendarId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting calendar {_testCalendarId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        [Fact]
        [Trait("TestCategory", "Authentication")]
        public async Task AuthenticateWithGoogleCalendarApi_ValidCredentials_ReturnsAccessToken()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Act
            await EnsureAccessTokenAsync();

            // Assert
            Assert.NotEmpty(_accessToken);
            Assert.True(_tokenExpirationTime > DateTime.UtcNow);
        }

        [Fact]
        [Trait("TestCategory", "CalendarOperations")]
        public async Task ListCalendars_AuthenticatedUser_ReturnsCalendarList()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();

            // Act
            var calendars = await ListCalendarsAsync();

            // Assert
            Assert.NotNull(calendars);
            Assert.True(calendars.Count > 0, "User should have at least one calendar");
        }

        [Fact]
        [Trait("TestCategory", "CalendarOperations")]
        public async Task CreateCalendar_ValidName_CreatesNewCalendar()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();
            string calendarName = $"{_testCalendarName} {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            // Act
            var calendarId = await CreateCalendarAsync(calendarName);
            _testCalendarId = calendarId; // Store for cleanup

            // Assert
            Assert.NotEmpty(calendarId);
            
            // Verify calendar exists
            var calendars = await ListCalendarsAsync();
            var createdCalendar = calendars.Find(c => c.Id == calendarId);
            Assert.NotNull(createdCalendar);
            Assert.Equal(calendarName, createdCalendar.Summary);
        }

        [Fact]
        [Trait("TestCategory", "EventOperations")]
        public async Task CreateEvent_ValidEvent_CreatesNewEvent()
        {
            // Skip if credentials not provided
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                return;
            }

            // Arrange
            await EnsureAccessTokenAsync();
            var calendars = await ListCalendarsAsync();
            var primaryCalendar = calendars.Find(c => c.Primary) ?? calendars.First();
            
            string eventSummary = $"Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var startTime = DateTime.Now.AddDays(1);
            var endTime = startTime.AddHours(1);

            // Act
            var eventId = await CreateEventAsync(primaryCalendar.Id, eventSummary, startTime, endTime);
            _createdEventIds.Add(eventId); // Store for cleanup

            // Assert
            Assert.NotEmpty(eventId);
            
            // Verify event exists
            var events = await ListEventsAsync(primaryCalendar.Id);
            var createdEvent = events.Find(e => e.Id == eventId);
            Assert.NotNull(createdEvent);
            Assert.Equal(eventSummary, createdEvent.Summary);
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

        private async Task<List<CalendarInfo>> ListCalendarsAsync()
        {
            // Ensure we have a valid token
            await EnsureAccessTokenAsync();
            
            // Prepare the request
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/calendar/v3/users/me/calendarList");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            
            // Send the request
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to list calendars: {responseContent}");
            }
            
            // Parse the response
            var calendarList = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var items = calendarList.GetProperty("items");
            
            var calendars = new List<CalendarInfo>();
            foreach (var item in items.EnumerateArray())
            {
                var calendar = new CalendarInfo
                {
                    Id = item.GetProperty("id").GetString() ?? string.Empty,
                    Summary = item.GetProperty("summary").GetString() ?? string.Empty,
                    Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : string.Empty,
                    Primary = item.TryGetProperty("primary", out var primary) && primary.GetBoolean()
                };
                
                calendars.Add(calendar);
            }
            
            return calendars;
        }

        private async Task<string> CreateCalendarAsync(string summary, string description = "")
        {
            // Ensure we have a valid token
            await EnsureAccessTokenAsync();
            
            // Prepare the request
            var calendarData = new
            {
                summary,
                description,
                timeZone = TimeZoneInfo.Local.Id
            };
            
            var json = JsonSerializer.Serialize(calendarData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/calendar/v3/calendars");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = content;
            
            // Send the request
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to create calendar: {responseContent}");
            }
            
            // Parse the response
            var calendarResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return calendarResponse.GetProperty("id").GetString() ?? string.Empty;
        }

        private async Task DeleteCalendarAsync(string calendarId)
        {
            // Ensure we have a valid token
            await EnsureAccessTokenAsync();
            
            // Prepare the request
            var request = new HttpRequestMessage(HttpMethod.Delete, $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            
            // Send the request
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete calendar: {responseContent}");
            }
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

        private async Task<string> CreateEventAsync(string calendarId = "primary", string summary = "Test Event", 
            DateTime? start = null, DateTime? end = null, string description = "", string location = "")
        {
            // Ensure we have a valid token
            await EnsureAccessTokenAsync();
            
            // Set default times if not provided
            start ??= DateTime.Now.AddDays(1);
            end ??= start.Value.AddHours(1);
            
            // Prepare the request
            var eventData = new
            {
                summary,
                description,
                location,
                start = new
                {
                    dateTime = start.Value.ToString("o"),
                    timeZone = TimeZoneInfo.Local.Id
                },
                end = new
                {
                    dateTime = end.Value.ToString("o"),
                    timeZone = TimeZoneInfo.Local.Id
                }
            };
            
            var json = JsonSerializer.Serialize(eventData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = content;
            
            // Send the request
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to create event: {responseContent}");
            }
            
            // Parse the response
            var eventResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return eventResponse.GetProperty("id").GetString() ?? string.Empty;
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

    // Helper classes for calendar data
    public class CalendarInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Primary { get; set; }
    }

    public class EventInfo
    {
        public string Id { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
