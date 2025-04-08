using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RecurringEventTest
{
    class Program
    {
        private static IServiceProvider _serviceProvider = null!;
        private static IConfiguration _configuration = null!;
        private static ILogger<Program> _logger = null!;
        private static HttpClient _httpClient = null!;
        private static string _accessToken = string.Empty;
        private static string _refreshToken = string.Empty;
        private static DateTime _tokenExpirationTime = DateTime.MinValue;
        private static string _clientId = string.Empty;
        private static string _clientSecret = string.Empty;
        private static readonly string _redirectUri = "http://localhost:8080";
        private static readonly string _credentialsFile = "google_credentials.json";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Google Calendar Recurring Event Test");
            Console.WriteLine("====================================");

            // Initialize services
            InitializeServices();

            try
            {
                // Get services
                _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

                // Get OAuth credentials
                await GetOAuthCredentialsAsync();

                // Show menu
                await ShowMenuAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void InitializeServices()
        {
            // Configure services
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // Add configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();
            services.AddSingleton(_configuration);

            // Add HTTP client
            _httpClient = new HttpClient();

            _serviceProvider = services.BuildServiceProvider();
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
            _clientId = _configuration["OAuth:Google:ClientId"] ?? string.Empty;
            _clientSecret = _configuration["OAuth:Google:ClientSecret"] ?? string.Empty;

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

        private static async Task ShowMenuAsync()
        {
            while (true)
            {
                Console.WriteLine("\nGoogle Calendar Test Menu:");
                Console.WriteLine("1. Create Recurring Event");
                Console.WriteLine("2. List Events");
                Console.WriteLine("3. Exit");

                Console.Write("\nEnter your choice: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await CreateRecurringEventAsync();
                        break;
                    case "2":
                        await ListEventsAsync();
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static async Task CreateRecurringEventAsync()
        {
            Console.WriteLine("\nCreate Recurring Event:");
            Console.WriteLine("----------------------");

            // Ensure we have a valid token
            await RefreshAccessTokenAsync();

            // Get event details
            Console.Write("Summary: ");
            var summary = Console.ReadLine() ?? "Recurring Test Event";

            Console.Write("Description: ");
            var description = Console.ReadLine() ?? "This is a recurring test event.";

            Console.Write("Location: ");
            var location = Console.ReadLine() ?? "Test Location";

            // Get recurrence type
            Console.WriteLine("\nRecurrence Type:");
            Console.WriteLine("1. Daily");
            Console.WriteLine("2. Weekly");
            Console.WriteLine("3. Monthly");
            Console.WriteLine("4. Yearly");
            Console.Write("Enter your choice: ");
            var recurrenceType = Console.ReadLine() ?? "1";

            // Get start date and time
            Console.Write("\nStart date (YYYY-MM-DD): ");
            var startDateStr = Console.ReadLine() ?? DateTime.Now.ToString("yyyy-MM-dd");
            if (!DateTime.TryParse(startDateStr, out var startDate))
            {
                startDate = DateTime.Now.Date;
            }

            Console.Write("Start time (HH:MM): ");
            var startTimeStr = Console.ReadLine() ?? "09:00";
            if (!TimeSpan.TryParse(startTimeStr, out var startTime))
            {
                startTime = new TimeSpan(9, 0, 0);
            }

            var startDateTime = startDate.Add(startTime);

            // Get duration
            Console.Write("Duration in minutes: ");
            var durationStr = Console.ReadLine() ?? "60";
            if (!int.TryParse(durationStr, out var duration))
            {
                duration = 60;
            }

            var endDateTime = startDateTime.AddMinutes(duration);

            // Create recurrence rule
            List<string> recurrence = new List<string>();

            switch (recurrenceType)
            {
                case "1": // Daily
                    Console.Write("Repeat every X days (1-30): ");
                    var dailyInterval = int.TryParse(Console.ReadLine(), out var di) ? di : 1;
                    recurrence.Add(CreateDailyRule(dailyInterval));
                    break;

                case "2": // Weekly
                    Console.Write("Repeat every X weeks (1-12): ");
                    var weeklyInterval = int.TryParse(Console.ReadLine(), out var wi) ? wi : 1;

                    Console.WriteLine("Select days of the week (comma-separated):");
                    Console.WriteLine("1. Monday");
                    Console.WriteLine("2. Tuesday");
                    Console.WriteLine("3. Wednesday");
                    Console.WriteLine("4. Thursday");
                    Console.WriteLine("5. Friday");
                    Console.WriteLine("6. Saturday");
                    Console.WriteLine("7. Sunday");
                    Console.WriteLine("8. Weekdays");
                    Console.WriteLine("9. Weekend");
                    Console.Write("Enter your choices: ");

                    var daysInput = Console.ReadLine() ?? "1,2,3,4,5";
                    var dayChoices = daysInput.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var daysOfWeek = new List<string>();

                    foreach (var choice in dayChoices)
                    {
                        if (int.TryParse(choice.Trim(), out var dayChoice))
                        {
                            switch (dayChoice)
                            {
                                case 1: daysOfWeek.Add("MO"); break;
                                case 2: daysOfWeek.Add("TU"); break;
                                case 3: daysOfWeek.Add("WE"); break;
                                case 4: daysOfWeek.Add("TH"); break;
                                case 5: daysOfWeek.Add("FR"); break;
                                case 6: daysOfWeek.Add("SA"); break;
                                case 7: daysOfWeek.Add("SU"); break;
                                case 8: daysOfWeek.AddRange(new[] { "MO", "TU", "WE", "TH", "FR" }); break;
                                case 9: daysOfWeek.AddRange(new[] { "SA", "SU" }); break;
                            }
                        }
                    }

                    if (daysOfWeek.Count == 0)
                    {
                        daysOfWeek.AddRange(new[] { "MO", "TU", "WE", "TH", "FR" });
                    }

                    // Remove duplicates
                    daysOfWeek = daysOfWeek.Distinct().ToList();

                    recurrence.Add(CreateWeeklyRule(weeklyInterval, daysOfWeek));
                    break;

                case "3": // Monthly
                    Console.Write("Repeat every X months (1-12): ");
                    var monthlyInterval = int.TryParse(Console.ReadLine(), out var mi) ? mi : 1;

                    Console.WriteLine("Recurrence type:");
                    Console.WriteLine("1. On day X of the month");
                    Console.WriteLine("2. On the Xth day of the week");
                    Console.Write("Enter your choice: ");
                    var monthlyType = Console.ReadLine() ?? "1";

                    if (monthlyType == "1")
                    {
                        Console.Write("Day of month (1-31): ");
                        var monthDay = int.TryParse(Console.ReadLine(), out var dom) ? dom : startDateTime.Day;
                        recurrence.Add(CreateMonthlyByDayRule(monthlyInterval, monthDay));
                    }
                    else
                    {
                        Console.WriteLine("Position:");
                        Console.WriteLine("1. First");
                        Console.WriteLine("2. Second");
                        Console.WriteLine("3. Third");
                        Console.WriteLine("4. Fourth");
                        Console.WriteLine("5. Last");
                        Console.Write("Enter your choice: ");
                        var positionChoice = Console.ReadLine() ?? "1";
                        var position = int.TryParse(positionChoice, out var pos) ? pos : 1;
                        if (position == 5) position = -1;

                        Console.WriteLine("Day of week:");
                        Console.WriteLine("1. Monday");
                        Console.WriteLine("2. Tuesday");
                        Console.WriteLine("3. Wednesday");
                        Console.WriteLine("4. Thursday");
                        Console.WriteLine("5. Friday");
                        Console.WriteLine("6. Saturday");
                        Console.WriteLine("7. Sunday");
                        Console.Write("Enter your choice: ");
                        var dayChoice = Console.ReadLine() ?? "1";
                        string dayCode = "MO";

                        if (int.TryParse(dayChoice, out var dc))
                        {
                            switch (dc)
                            {
                                case 1: dayCode = "MO"; break;
                                case 2: dayCode = "TU"; break;
                                case 3: dayCode = "WE"; break;
                                case 4: dayCode = "TH"; break;
                                case 5: dayCode = "FR"; break;
                                case 6: dayCode = "SA"; break;
                                case 7: dayCode = "SU"; break;
                            }
                        }

                        recurrence.Add(CreateMonthlyByPositionRule(monthlyInterval, position, dayCode));
                    }
                    break;

                case "4": // Yearly
                    Console.Write("Repeat every X years (1-10): ");
                    var yearlyInterval = int.TryParse(Console.ReadLine(), out var yi) ? yi : 1;

                    Console.Write("Month (1-12): ");
                    var month = int.TryParse(Console.ReadLine(), out var m) ? m : startDateTime.Month;

                    Console.Write("Day (1-31): ");
                    var dayOfMonth = int.TryParse(Console.ReadLine(), out var d) ? d : startDateTime.Day;

                    recurrence.Add(CreateYearlyRule(yearlyInterval, month, dayOfMonth));
                    break;
            }

            // Get end type
            Console.WriteLine("\nEnd type:");
            Console.WriteLine("1. Never");
            Console.WriteLine("2. After X occurrences");
            Console.WriteLine("3. On date");
            Console.Write("Enter your choice: ");
            var endType = Console.ReadLine() ?? "1";

            if (endType == "2")
            {
                Console.Write("Number of occurrences: ");
                var count = int.TryParse(Console.ReadLine(), out var c) ? c : 10;

                // Update the recurrence rule with count
                var rule = recurrence[0];
                if (rule.Contains("COUNT=") || rule.Contains("UNTIL="))
                {
                    // Remove existing COUNT or UNTIL
                    rule = Regex.Replace(rule, "(;COUNT=[0-9]+|;UNTIL=[0-9TZ]+)", "");
                }
                rule += $";COUNT={count}";
                recurrence[0] = rule;
            }
            else if (endType == "3")
            {
                Console.Write("End date (YYYY-MM-DD): ");
                var endDateStr = Console.ReadLine() ?? DateTime.Now.AddMonths(3).ToString("yyyy-MM-dd");
                if (!DateTime.TryParse(endDateStr, out var endDate))
                {
                    endDate = DateTime.Now.AddMonths(3).Date;
                }

                // Update the recurrence rule with until
                var rule = recurrence[0];
                if (rule.Contains("COUNT=") || rule.Contains("UNTIL="))
                {
                    // Remove existing COUNT or UNTIL
                    rule = Regex.Replace(rule, "(;COUNT=[0-9]+|;UNTIL=[0-9TZ]+)", "");
                }
                rule += $";UNTIL={endDate.ToUniversalTime():yyyyMMddTHHmmssZ}";
                recurrence[0] = rule;
            }

            // Create the event
            Console.WriteLine("\nCreating recurring event...");

            // Create the event data
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
                ["reminders"] = new Dictionary<string, object>
                {
                    ["useDefault"] = false,
                    ["overrides"] = new[]
                    {
                        new Dictionary<string, object> { ["method"] = "email", ["minutes"] = 30 },
                        new Dictionary<string, object> { ["method"] = "popup", ["minutes"] = 10 }
                    }
                },
                ["colorId"] = "7" // Light blue
            };

            // Set up the request
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/calendar/v3/calendars/primary/events");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(eventData),
                Encoding.UTF8,
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
            Console.WriteLine($"Recurrence rule: {recurrence[0]}");
        }

        private static async Task ListEventsAsync()
        {
            Console.WriteLine("\nListing Events:");
            Console.WriteLine("---------------");

            // Ensure we have a valid token
            await RefreshAccessTokenAsync();

            Console.Write("Enter start date (YYYY-MM-DD) or press Enter for today: ");
            var startDateStr = Console.ReadLine();
            var startDate = string.IsNullOrEmpty(startDateStr) ? DateTime.Today.ToString("yyyy-MM-dd") : startDateStr;

            Console.Write("Enter end date (YYYY-MM-DD) or press Enter for 30 days from start: ");
            var endDateStr = Console.ReadLine();
            var endDate = string.IsNullOrEmpty(endDateStr) ? DateTime.Today.AddDays(30).ToString("yyyy-MM-dd") : endDateStr;

            // Get events for the date range
            var timeMin = Uri.EscapeDataString(DateTime.Parse(startDate).ToString("o"));
            var timeMax = Uri.EscapeDataString(DateTime.Parse(endDate).ToString("o"));

            var requestUrl = $"https://www.googleapis.com/calendar/v3/calendars/primary/events" +
                $"?timeMin={timeMin}" +
                $"&timeMax={timeMax}" +
                $"&maxResults=100" +
                $"&singleEvents=true" +
                $"&orderBy=startTime";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            if (!data.TryGetProperty("items", out var items))
            {
                Console.WriteLine("No events found.");
                return;
            }

            var eventCount = items.GetArrayLength();
            if (eventCount == 0)
            {
                Console.WriteLine("No events found.");
                return;
            }

            Console.WriteLine($"Found {eventCount} events:");

            foreach (var item in items.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString();
                var summary = item.GetProperty("summary").GetString();

                Console.WriteLine($"Event: {summary} ({id})");

                // Get start and end times
                string? startDateTime = null;
                string? endDateTime = null;

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

                if (item.TryGetProperty("end", out var end))
                {
                    if (end.TryGetProperty("dateTime", out var endDateTimeProp))
                    {
                        endDateTime = endDateTimeProp.GetString();
                    }
                    else if (end.TryGetProperty("date", out var endDateProp))
                    {
                        endDateTime = endDateProp.GetString();
                    }
                }

                Console.WriteLine($"  Start: {startDateTime}");
                Console.WriteLine($"  End: {endDateTime}");

                // Get description
                if (item.TryGetProperty("description", out var descriptionProp))
                {
                    Console.WriteLine($"  Description: {descriptionProp.GetString()}");
                }

                // Get location
                if (item.TryGetProperty("location", out var locationProp))
                {
                    Console.WriteLine($"  Location: {locationProp.GetString()}");
                }

                // Get color
                if (item.TryGetProperty("colorId", out var colorIdProp))
                {
                    Console.WriteLine($"  Color ID: {colorIdProp.GetString()}");
                }

                // Get recurrence
                if (item.TryGetProperty("recurrence", out var recurrenceProp))
                {
                    var recurrenceRules = new List<string>();
                    foreach (var rule in recurrenceProp.EnumerateArray())
                    {
                        recurrenceRules.Add(rule.GetString() ?? string.Empty);
                    }
                    Console.WriteLine($"  Recurrence: {string.Join(", ", recurrenceRules)}");
                }

                // Get recurring event ID
                if (item.TryGetProperty("recurringEventId", out var recurringEventIdProp))
                {
                    Console.WriteLine($"  Instance of recurring event: {recurringEventIdProp.GetString()}");
                }

                Console.WriteLine();
            }
        }

        #region Recurrence Rule Helpers

        private static string CreateDailyRule(int interval = 1)
        {
            var rule = $"RRULE:FREQ=DAILY";

            if (interval > 1)
            {
                rule += $";INTERVAL={interval}";
            }

            return rule;
        }

        private static string CreateWeeklyRule(int interval, List<string> daysOfWeek)
        {
            var rule = $"RRULE:FREQ=WEEKLY";

            if (interval > 1)
            {
                rule += $";INTERVAL={interval}";
            }

            if (daysOfWeek.Count > 0)
            {
                rule += $";BYDAY={string.Join(",", daysOfWeek)}";
            }

            return rule;
        }

        private static string CreateMonthlyByDayRule(int interval, int dayOfMonth)
        {
            var rule = $"RRULE:FREQ=MONTHLY";

            if (interval > 1)
            {
                rule += $";INTERVAL={interval}";
            }

            rule += $";BYMONTHDAY={dayOfMonth}";

            return rule;
        }

        private static string CreateMonthlyByPositionRule(int interval, int position, string dayOfWeek)
        {
            var rule = $"RRULE:FREQ=MONTHLY";

            if (interval > 1)
            {
                rule += $";INTERVAL={interval}";
            }

            rule += $";BYDAY={position}{dayOfWeek}";

            return rule;
        }

        private static string CreateYearlyRule(int interval, int month, int day)
        {
            var rule = $"RRULE:FREQ=YEARLY";

            if (interval > 1)
            {
                rule += $";INTERVAL={interval}";
            }

            rule += $";BYMONTH={month};BYMONTHDAY={day}";

            return rule;
        }

        #endregion
    }
}
