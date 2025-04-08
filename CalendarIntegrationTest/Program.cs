using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CalendarIntegrationTest
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _accessToken = string.Empty;
        private static DateTime _tokenExpirationTime = DateTime.MinValue;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Google Calendar API Test");
            Console.WriteLine("=======================");

            try
            {
                // Get OAuth credentials
                await GetOAuthCredentialsAsync();

                // Test listing calendars
                await ListCalendarsAsync();

                // Test listing events
                await ListEventsAsync();

                // Test creating an event
                await CreateEventAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task GetOAuthCredentialsAsync()
        {
            Console.WriteLine("\nEnter your Google OAuth credentials:");
            Console.Write("Client ID: ");
            var clientId = Console.ReadLine();

            Console.Write("Client Secret: ");
            var clientSecret = Console.ReadLine();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception("Client ID and Client Secret are required");
            }

            Console.WriteLine("\nCredentials received. Proceeding with OAuth flow...");

            // Generate the authorization URL
            var authUrl = $"https://accounts.google.com/o/oauth2/auth" +
                $"?client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString("http://localhost:8080")}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/calendar")}" +
                $"&access_type=offline" +
                $"&prompt=consent";

            Console.WriteLine("\nPlease follow these steps:");
            Console.WriteLine("1. Copy and paste the following URL into your browser:");
            Console.WriteLine(authUrl);
            Console.WriteLine("\n2. Sign in with your Google account and grant the requested permissions");
            Console.WriteLine("3. After authorization, you will be redirected to a URL that may show an error page (this is normal)");
            Console.WriteLine("4. Copy the 'code' parameter from the URL in your browser's address bar");
            Console.WriteLine("   (The URL will look like: http://localhost:8080/?code=4/0AeaYSHDJ_kw...)");

            Console.WriteLine("\nPlease enter the authorization code:");
            Console.Write("> ");
            var authCode = Console.ReadLine();

            if (string.IsNullOrEmpty(authCode))
            {
                throw new Exception("Authorization code is required");
            }

            Console.WriteLine("\nAuthorization code received. Exchanging for access token...");

            // Exchange the authorization code for an access token
            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = authCode,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = "http://localhost:8080",
                ["grant_type"] = "authorization_code"
            });

            var tokenResponse = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequestContent);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to exchange authorization code for token: {errorContent}");
            }

            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResponseContent);

            _accessToken = tokenData.GetProperty("access_token").GetString() ?? string.Empty;
            var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
            _tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresIn);

            Console.WriteLine("✓ Access token received");
        }

        private static async Task ListCalendarsAsync()
        {
            Console.WriteLine("\nListing Calendars:");
            Console.WriteLine("-----------------");

            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/calendar/v3/users/me/calendarList");
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to list calendars: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var calendarList = JsonSerializer.Deserialize<JsonElement>(content);
            var items = calendarList.GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString();
                var summary = item.GetProperty("summary").GetString();
                var primary = item.TryGetProperty("primary", out var primaryProp) && primaryProp.GetBoolean();

                Console.WriteLine($"Calendar: {summary} ({id}){(primary ? " (Primary)" : "")}");
            }

            Console.WriteLine("✓ Calendars listed successfully");
        }

        private static async Task ListEventsAsync()
        {
            Console.WriteLine("\nListing Events:");
            Console.WriteLine("---------------");

            // Use the primary calendar
            var calendarId = "primary";

            // Get events for the next 7 days
            var now = DateTime.UtcNow;
            var timeMin = Uri.EscapeDataString(now.ToString("o"));
            var timeMax = Uri.EscapeDataString(now.AddDays(7).ToString("o"));

            var requestUrl = $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(calendarId)}/events" +
                $"?timeMin={timeMin}" +
                $"&timeMax={timeMax}" +
                $"&maxResults=10" +
                $"&singleEvents=true" +
                $"&orderBy=startTime";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to list events: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var eventList = JsonSerializer.Deserialize<JsonElement>(content);

            if (eventList.TryGetProperty("items", out var items))
            {
                if (items.GetArrayLength() == 0)
                {
                    Console.WriteLine("No events found for the next 7 days");
                }
                else
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var id = item.GetProperty("id").GetString();
                        var summary = item.GetProperty("summary").GetString();

                        string? startDateTime = null;
                        if (item.TryGetProperty("start", out var start))
                        {
                            if (start.TryGetProperty("dateTime", out var startDateTimeProp))
                            {
                                startDateTime = startDateTimeProp.GetString();
                            }
                            else if (start.TryGetProperty("date", out var startDateProp))
                            {
                                startDateTime = startDateProp.GetString();
                            }
                        }

                        Console.WriteLine($"Event: {summary} ({id}) - {startDateTime}");
                    }
                }
            }
            else
            {
                Console.WriteLine("No events property found in response");
            }

            Console.WriteLine("✓ Events listed successfully");
        }

        private static async Task CreateEventAsync()
        {
            Console.WriteLine("\nCreating Event:");
            Console.WriteLine("---------------");

            // Use the primary calendar
            var calendarId = "primary";

            // Create an event for tomorrow
            var tomorrow = DateTime.UtcNow.AddDays(1).Date;
            var startTime = tomorrow.AddHours(10);
            var endTime = tomorrow.AddHours(11);

            var eventData = new
            {
                summary = "Test Event from API",
                description = "This is a test event created by the Google Calendar API test program",
                start = new
                {
                    dateTime = startTime.ToString("o"),
                    timeZone = "UTC"
                },
                end = new
                {
                    dateTime = endTime.ToString("o"),
                    timeZone = "UTC"
                }
            };

            var json = JsonSerializer.Serialize(eventData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(calendarId)}/events");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create event: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdEvent = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var eventId = createdEvent.GetProperty("id").GetString();
            var eventHtmlLink = createdEvent.GetProperty("htmlLink").GetString();

            Console.WriteLine($"✓ Event created successfully with ID: {eventId}");
            Console.WriteLine($"Event link: {eventHtmlLink}");
        }
    }
}