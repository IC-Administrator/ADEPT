using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace CalendarApiTest
{
    class Program
    {
        private static HttpClient _httpClient = new HttpClient();
        private static string _clientId = "";
        private static string _clientSecret = "";
        private static string _accessToken = "";
        private static string _refreshToken = "";
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private static readonly string _redirectUri = "http://localhost:8080";
        private static readonly string _authorizationEndpoint = "https://accounts.google.com/o/oauth2/auth";
        private static readonly string _tokenEndpoint = "https://oauth2.googleapis.com/token";
        private static readonly string _calendarApiBaseUrl = "https://www.googleapis.com/calendar/v3";
        private static readonly string _scope = "https://www.googleapis.com/auth/calendar";
        private static readonly string _credentialsFile = "credentials.json";

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

        static async Task GetOAuthCredentialsAsync()
        {
            Console.WriteLine("Setting up OAuth credentials...");

            // Check if we have saved credentials
            if (File.Exists(_credentialsFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_credentialsFile);
                    var credentials = JsonSerializer.Deserialize<JsonElement>(json);

                    _clientId = credentials.GetProperty("client_id").GetString() ?? "";
                    _clientSecret = credentials.GetProperty("client_secret").GetString() ?? "";
                    _refreshToken = credentials.GetProperty("refresh_token").GetString() ?? "";

                    if (!string.IsNullOrEmpty(_refreshToken))
                    {
                        // Refresh the access token
                        await RefreshAccessTokenAsync();
                        Console.WriteLine("✓ Credentials loaded and token refreshed");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading credentials: {ex.Message}");
                }
            }

            // Ask for client ID and secret
            Console.Write("Enter your Google API Client ID: ");
            _clientId = Console.ReadLine() ?? "";

            Console.Write("Enter your Google API Client Secret: ");
            _clientSecret = Console.ReadLine() ?? "";

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                throw new InvalidOperationException("Client ID and Client Secret are required");
            }

            // Start the authorization flow
            await StartAuthorizationFlowAsync();
        }

        static async Task StartAuthorizationFlowAsync()
        {
            Console.WriteLine("Starting OAuth authorization flow...");

            // Create a local HTTP server to receive the callback
            var httpListener = new System.Net.HttpListener();
            httpListener.Prefixes.Add($"{_redirectUri}/");
            httpListener.Start();

            // Generate the authorization URL
            var authorizationUrl = $"{_authorizationEndpoint}?client_id={_clientId}&redirect_uri={_redirectUri}&response_type=code&scope={_scope}&access_type=offline&prompt=consent";

            // Open the browser for the user to authenticate
            Process.Start(new ProcessStartInfo
            {
                FileName = authorizationUrl,
                UseShellExecute = true
            });

            Console.WriteLine("Waiting for authorization...");

            // Wait for the callback
            var context = await httpListener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            // Send a response to the browser
            var response = context.Response;
            var responseString = "<html><body><h1>Authorization successful!</h1><p>You can close this window now.</p></body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length);
            responseOutput.Close();
            httpListener.Stop();

            if (string.IsNullOrEmpty(code))
            {
                throw new InvalidOperationException("Authorization code not received");
            }

            Console.WriteLine("Authorization code received, exchanging for tokens...");

            // Exchange the authorization code for tokens
            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "redirect_uri", _redirectUri },
                { "grant_type", "authorization_code" }
            });

            var tokenResponse = await _httpClient.PostAsync(_tokenEndpoint, tokenRequestContent);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

            _accessToken = tokenData.GetProperty("access_token").GetString() ?? "";
            _refreshToken = tokenData.GetProperty("refresh_token").GetString() ?? "";
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32());

            // Save the credentials
            var credentials = new
            {
                client_id = _clientId,
                client_secret = _clientSecret,
                refresh_token = _refreshToken
            };

            await File.WriteAllTextAsync(_credentialsFile, JsonSerializer.Serialize(credentials));

            Console.WriteLine("✓ Authentication successful");
        }

        static async Task RefreshAccessTokenAsync()
        {
            Console.WriteLine("Refreshing access token...");

            // Check if the token is still valid
            if (_tokenExpiry > DateTime.UtcNow.AddMinutes(5))
            {
                Console.WriteLine("Token is still valid");
                return;
            }

            // Refresh the token
            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "refresh_token", _refreshToken },
                { "grant_type", "refresh_token" }
            });

            var tokenResponse = await _httpClient.PostAsync(_tokenEndpoint, tokenRequestContent);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

            _accessToken = tokenData.GetProperty("access_token").GetString() ?? "";
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32());

            Console.WriteLine("✓ Token refreshed successfully");
        }

        static async Task ListCalendarsAsync()
        {
            Console.WriteLine("\nListing calendars...");

            // Ensure we have a valid token
            await RefreshAccessTokenAsync();

            // Set up the request
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_calendarApiBaseUrl}/users/me/calendarList");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Send the request
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Parse the response
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

        static async Task ListEventsAsync()
        {
            Console.WriteLine("\nListing events for the next 7 days...");

            // Ensure we have a valid token
            await RefreshAccessTokenAsync();

            // Set up the request
            var now = DateTime.UtcNow;
            var timeMin = Uri.EscapeDataString(now.ToString("o"));
            var timeMax = Uri.EscapeDataString(now.AddDays(7).ToString("o"));

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_calendarApiBaseUrl}/calendars/primary/events?timeMin={timeMin}&timeMax={timeMax}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Send the request
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Parse the response
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

        static async Task CreateEventAsync()
        {
            Console.WriteLine("\nCreating a test event...");

            // Ensure we have a valid token
            await RefreshAccessTokenAsync();

            // Create the event data
            var startTime = DateTime.UtcNow.AddHours(1);
            var endTime = startTime.AddHours(1);

            var eventData = new
            {
                summary = "Test Event",
                description = "This is a test event created by the Calendar API Test",
                location = "Test Location",
                start = new
                {
                    dateTime = startTime.ToString("o"),
                    timeZone = "UTC"
                },
                end = new
                {
                    dateTime = endTime.ToString("o"),
                    timeZone = "UTC"
                },
                reminders = new
                {
                    useDefault = false,
                    overrides = new[]
                    {
                        new { method = "email", minutes = 30 },
                        new { method = "popup", minutes = 10 }
                    }
                },
                colorId = "7" // A light blue color
            };

            // Set up the request
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_calendarApiBaseUrl}/calendars/primary/events");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(eventData),
                System.Text.Encoding.UTF8,
                "application/json");

            // Send the request
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Parse the response
            var content = await response.Content.ReadAsStringAsync();
            var createdEvent = JsonSerializer.Deserialize<JsonElement>(content);
            var eventId = createdEvent.GetProperty("id").GetString();
            var htmlLink = createdEvent.GetProperty("htmlLink").GetString();

            Console.WriteLine($"✓ Event created successfully with ID: {eventId}");
            Console.WriteLine($"View the event at: {htmlLink}");
        }
    }
}
