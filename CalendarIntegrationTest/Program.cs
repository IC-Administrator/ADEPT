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

namespace CalendarIntegrationTest
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _accessToken = string.Empty;
        private static string _refreshToken = string.Empty;
        private static DateTime _tokenExpirationTime = DateTime.MinValue;
        private static string _clientId = string.Empty;
        private static string _clientSecret = string.Empty;
        private static readonly string _redirectUri = "http://localhost:8080";
        private static readonly string _credentialsFile = "google_credentials.json";

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
            // Check if we have saved credentials
            if (File.Exists(_credentialsFile))
            {
                try
                {
                    Console.WriteLine("Loading saved credentials...");
                    var json = await File.ReadAllTextAsync(_credentialsFile);
                    var credentials = JsonSerializer.Deserialize<JsonElement>(json);

                    _clientId = credentials.GetProperty("client_id").GetString() ?? string.Empty;
                    _clientSecret = credentials.GetProperty("client_secret").GetString() ?? string.Empty;
                    _refreshToken = credentials.GetProperty("refresh_token").GetString() ?? string.Empty;

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
                    // Continue to prompt for credentials
                }
            }

            // Load from appsettings.json if available
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            _clientId = configuration["OAuth:Google:ClientId"] ?? string.Empty;
            _clientSecret = configuration["OAuth:Google:ClientSecret"] ?? string.Empty;

            // Prompt for credentials if not found
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                Console.WriteLine("\nEnter your Google OAuth credentials:");
                Console.Write("Client ID: ");
                _clientId = Console.ReadLine() ?? string.Empty;

                Console.Write("Client Secret: ");
                _clientSecret = Console.ReadLine() ?? string.Empty;

                if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
                {
                    throw new Exception("Client ID and Client Secret are required");
                }
            }

            Console.WriteLine("\nCredentials received. Starting OAuth flow...");

            // Start the authorization flow
            await StartAuthorizationFlowAsync();
        }

        private static async Task StartAuthorizationFlowAsync()
        {
            Console.WriteLine("Starting OAuth authorization flow...");

            // Create a local HTTP server to receive the callback
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add($"{_redirectUri}/");

            try
            {
                httpListener.Start();
                Console.WriteLine("Local HTTP server started on port 8080");

                // Generate the authorization URL
                var authUrl = $"https://accounts.google.com/o/oauth2/auth" +
                    $"?client_id={Uri.EscapeDataString(_clientId)}" +
                    $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                    $"&response_type=code" +
                    $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/calendar")}" +
                    $"&access_type=offline" +
                    $"&prompt=consent";

                // Open the browser for the user to authenticate
                Console.WriteLine("Opening browser for authentication...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                Console.WriteLine("Waiting for authorization callback...");

                // Wait for the callback
                var context = await httpListener.GetContextAsync();
                var code = context.Request.QueryString["code"];

                // Send a response to the browser
                var response = context.Response;
                var responseString = "<html><body><h1>Authorization successful!</h1><p>You can close this window now.</p></body></html>";
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var responseOutput = response.OutputStream;
                await responseOutput.WriteAsync(buffer, 0, buffer.Length);
                responseOutput.Close();

                if (string.IsNullOrEmpty(code))
                {
                    throw new Exception("Authorization code not received");
                }

                Console.WriteLine("Authorization code received, exchanging for tokens...");

                // Exchange the authorization code for tokens
                var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = _clientId,
                    ["client_secret"] = _clientSecret,
                    ["redirect_uri"] = _redirectUri,
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
                _refreshToken = tokenData.GetProperty("refresh_token").GetString() ?? string.Empty;
                var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
                _tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresIn);

                // Save the credentials
                var credentials = new
                {
                    client_id = _clientId,
                    client_secret = _clientSecret,
                    refresh_token = _refreshToken
                };

                await File.WriteAllTextAsync(_credentialsFile, JsonSerializer.Serialize(credentials));

                Console.WriteLine("✓ Authentication successful and credentials saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during authorization: {ex.Message}");
                throw;
            }
            finally
            {
                // Stop the HTTP listener
                if (httpListener.IsListening)
                {
                    httpListener.Stop();
                    Console.WriteLine("Local HTTP server stopped");
                }
            }
        }

        private static async Task RefreshAccessTokenAsync()
        {
            Console.WriteLine("Refreshing access token...");

            // Check if the token is still valid
            if (_tokenExpirationTime > DateTime.UtcNow.AddMinutes(5))
            {
                Console.WriteLine("Token is still valid");
                return;
            }

            // Refresh the token
            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["refresh_token"] = _refreshToken,
                ["grant_type"] = "refresh_token"
            });

            var tokenResponse = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequestContent);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to refresh token: {errorContent}");
            }

            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResponseContent);

            _accessToken = tokenData.GetProperty("access_token").GetString() ?? string.Empty;
            var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
            _tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresIn);

            Console.WriteLine("✓ Token refreshed successfully");
        }

        private static async Task ListCalendarsAsync()
        {
            Console.WriteLine("\nListing Calendars:");
            Console.WriteLine("-----------------");

            // Ensure we have a valid token
            await RefreshAccessTokenAsync();

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

            // Ensure we have a valid token
            await RefreshAccessTokenAsync();

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

            // Ensure we have a valid token
            await RefreshAccessTokenAsync();

            // Use the primary calendar
            var calendarId = "primary";

            // Create an event for tomorrow
            var tomorrow = DateTime.UtcNow.AddDays(1).Date;
            var startTime = tomorrow.AddHours(10);
            var endTime = tomorrow.AddHours(11);

            // Ask for event details
            Console.WriteLine("\nEnter event details (or press Enter for defaults):");

            Console.Write("Summary [Test Event from API]: ");
            var summary = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(summary))
                summary = "Test Event from API";

            Console.Write("Description [This is a test event]: ");
            var description = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(description))
                description = "This is a test event created by the Google Calendar API test program";

            Console.Write("Location [Test Location]: ");
            var location = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(location))
                location = "Test Location";

            // Create the event with enhanced features
            var eventData = new
            {
                summary = summary,
                description = description,
                location = location,
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
                colorId = "7", // Light blue
                reminders = new
                {
                    useDefault = false,
                    overrides = new[]
                    {
                        new { method = "email", minutes = 30 },
                        new { method = "popup", minutes = 10 }
                    }
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