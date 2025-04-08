using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpToolsTest
{
    class CalendarTest
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
            Console.WriteLine("\nSetting up OAuth 2.0 Authentication:");
            Console.WriteLine("----------------------------------");

            // Check if we already have a valid token
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpirationTime)
            {
                Console.WriteLine("✓ Using existing access token");
                return;
            }

            // Prompt for client ID and client secret
            Console.WriteLine("You'll need to provide your Google OAuth credentials.");
            Console.WriteLine("These will only be used for this test and won't be stored in the code.");
            Console.WriteLine("\nPlease enter your Google OAuth Client ID:");
            Console.WriteLine("(It should look like: 123456789012-abcdefghijklmnopqrstuvwxyz.apps.googleusercontent.com)");
            Console.Write("> ");
            var clientId = Console.ReadLine();

            Console.WriteLine("\nPlease enter your Google OAuth Client Secret:");
            Console.Write("> ");
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
                throw new Exception($"Failed to get access token: {errorContent}");
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tokenJson);

            if (tokenData == null || !tokenData.TryGetValue("access_token", out var accessTokenElement))
            {
                throw new Exception("Access token not found in response");
            }

            _accessToken = accessTokenElement.GetString() ?? string.Empty;

            // Get expiration time (default to 1 hour if not provided)
            int expiresIn = 3600;
            if (tokenData.TryGetValue("expires_in", out var expiresInElement))
            {
                expiresIn = expiresInElement.GetInt32();
            }

            _tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresIn);

            Console.WriteLine("✓ Successfully obtained access token");

            // Save the token to a file for future use
            var tokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "google_token.json");
            File.WriteAllText(tokenFilePath, tokenJson);
            Console.WriteLine($"✓ Token saved to: {tokenFilePath}");
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

            var calendarJson = await response.Content.ReadAsStringAsync();
            var calendarData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(calendarJson);

            if (calendarData == null || !calendarData.TryGetValue("items", out var calendarsElement))
            {
                throw new Exception("No calendars found in response");
            }

            var calendars = calendarsElement.EnumerateArray();
            var calendarList = new List<(string Id, string Summary)>();

            Console.WriteLine("Your calendars:");
            foreach (var calendar in calendars)
            {
                var id = calendar.GetProperty("id").GetString() ?? string.Empty;
                var summary = calendar.GetProperty("summary").GetString() ?? "Unnamed Calendar";

                calendarList.Add((id, summary));
                Console.WriteLine($"  • {summary} (ID: {id})");
            }

            // Save the primary calendar ID for later use
            var primaryCalendar = calendarList.Find(c => c.Summary == "Primary" || c.Summary.Contains("primary", StringComparison.OrdinalIgnoreCase));
            if (primaryCalendar == default)
            {
                primaryCalendar = calendarList.Count > 0 ? calendarList[0] : (string.Empty, string.Empty);
            }

            Console.WriteLine($"\n✓ Using calendar: {primaryCalendar.Summary} (ID: {primaryCalendar.Id})");

            // Save the calendar ID to a file for future use
            var calendarIdFilePath = Path.Combine(Directory.GetCurrentDirectory(), "primary_calendar_id.txt");
            File.WriteAllText(calendarIdFilePath, primaryCalendar.Id);
        }

        private static async Task ListEventsAsync()
        {
            Console.WriteLine("\nListing Events:");
            Console.WriteLine("--------------");

            // Get the primary calendar ID
            var calendarIdFilePath = Path.Combine(Directory.GetCurrentDirectory(), "primary_calendar_id.txt");
            if (!File.Exists(calendarIdFilePath))
            {
                throw new Exception("Primary calendar ID not found. Please run ListCalendarsAsync first.");
            }

            var calendarId = File.ReadAllText(calendarIdFilePath);
            if (string.IsNullOrEmpty(calendarId))
            {
                throw new Exception("Primary calendar ID is empty");
            }

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

            var eventsJson = await response.Content.ReadAsStringAsync();
            var eventsData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(eventsJson);

            if (eventsData == null)
            {
                throw new Exception("No events data found in response");
            }

            // Save the raw events data to a file for inspection
            var eventsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "calendar_events.json");
            File.WriteAllText(eventsFilePath, eventsJson);
            Console.WriteLine($"✓ Raw events data saved to: {eventsFilePath}");

            if (!eventsData.TryGetValue("items", out var eventsElement))
            {
                Console.WriteLine("No events found for the next 7 days");
                return;
            }

            var events = eventsElement.EnumerateArray();
            var eventsList = new List<(string Id, string Summary, string Start, string End)>();

            Console.WriteLine("\nUpcoming events (next 7 days):");
            foreach (var eventItem in events)
            {
                var id = eventItem.GetProperty("id").GetString() ?? string.Empty;
                var summary = eventItem.TryGetProperty("summary", out var summaryElement)
                    ? summaryElement.GetString() ?? "Unnamed Event"
                    : "Unnamed Event";

                string start = "Unknown";
                string end = "Unknown";

                if (eventItem.TryGetProperty("start", out var startElement))
                {
                    if (startElement.TryGetProperty("dateTime", out var startDateTime))
                    {
                        start = startDateTime.GetString() ?? "Unknown";
                    }
                    else if (startElement.TryGetProperty("date", out var startDate))
                    {
                        start = startDate.GetString() ?? "Unknown";
                    }
                }

                if (eventItem.TryGetProperty("end", out var endElement))
                {
                    if (endElement.TryGetProperty("dateTime", out var endDateTime))
                    {
                        end = endDateTime.GetString() ?? "Unknown";
                    }
                    else if (endElement.TryGetProperty("date", out var endDate))
                    {
                        end = endDate.GetString() ?? "Unknown";
                    }
                }

                eventsList.Add((id, summary, start, end));
                Console.WriteLine($"  • {summary}");
                Console.WriteLine($"    Start: {start}");
                Console.WriteLine($"    End: {end}");
                Console.WriteLine($"    ID: {id}");
                Console.WriteLine();
            }

            Console.WriteLine($"✓ Found {eventsList.Count} events in the next 7 days");
        }

        private static async Task CreateEventAsync()
        {
            Console.WriteLine("\nCreating a Test Event:");
            Console.WriteLine("--------------------");

            // Get the primary calendar ID
            var calendarIdFilePath = Path.Combine(Directory.GetCurrentDirectory(), "primary_calendar_id.txt");
            if (!File.Exists(calendarIdFilePath))
            {
                throw new Exception("Primary calendar ID not found. Please run ListCalendarsAsync first.");
            }

            var calendarId = File.ReadAllText(calendarIdFilePath);
            if (string.IsNullOrEmpty(calendarId))
            {
                throw new Exception("Primary calendar ID is empty");
            }

            // Create a test event
            var startTime = DateTime.UtcNow.AddHours(1);
            var endTime = startTime.AddHours(1);

            var eventData = new
            {
                summary = "Test Event from Calendar API Test",
                description = "This is a test event created by the Calendar API Test application.",
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

            var eventJson = JsonSerializer.Serialize(eventData);
            var content = new StringContent(eventJson, Encoding.UTF8, "application/json");

            var requestUrl = $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(calendarId)}/events";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create event: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseJson);

            if (responseData == null)
            {
                throw new Exception("No response data found");
            }

            var eventId = responseData.TryGetValue("id", out var idElement) ? idElement.GetString() : "Unknown";
            var eventHtmlLink = responseData.TryGetValue("htmlLink", out var linkElement) ? linkElement.GetString() : "Unknown";

            Console.WriteLine("✓ Event created successfully!");
            Console.WriteLine($"  Event ID: {eventId}");
            Console.WriteLine($"  Event Link: {eventHtmlLink}");
            Console.WriteLine($"  Start Time: {startTime.ToString("o")}");
            Console.WriteLine($"  End Time: {endTime.ToString("o")}");
        }
    }
}
