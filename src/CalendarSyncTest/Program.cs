using Adept.Common.Interfaces;
using Adept.Common.Models;
using Adept.Services.Calendar;
using Adept.Services.OAuth;
using Adept.Services.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalendarSyncTest
{
    class Program
    {
        private static IServiceProvider _serviceProvider = null!;
        private static IConfiguration _configuration = null!;
        private static ILogger<Program> _logger = null!;
        private static ICalendarService _calendarService = null!;
        private static ISecureStorageService _secureStorageService = null!;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Google Calendar Two-Way Sync Test");
            Console.WriteLine("=================================");

            // Initialize services
            InitializeServices();

            try
            {
                // Get services
                _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
                _calendarService = _serviceProvider.GetRequiredService<ICalendarService>();
                _secureStorageService = _serviceProvider.GetRequiredService<ISecureStorageService>();

                // Initialize the calendar service
                await _calendarService.InitializeAsync();

                // Check if we need to set up credentials
                await SetupCredentialsIfNeededAsync();

                // Check if we're authenticated
                var isAuthenticated = await _calendarService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    Console.WriteLine("Not authenticated with Google Calendar. Starting authentication...");
                    isAuthenticated = await _calendarService.AuthenticateAsync();
                    if (!isAuthenticated)
                    {
                        Console.WriteLine("Authentication failed. Exiting.");
                        return;
                    }
                }

                Console.WriteLine("Authenticated with Google Calendar.");

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
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
            services.AddSingleton(_configuration);

            // Add HTTP clients
            services.AddHttpClient();

            // Add services
            services.AddSingleton<ICryptographyService, CryptographyService>();
            services.AddSingleton<ISecureStorageService, SecureStorageService>();
            services.AddSingleton<IOAuthService, GoogleOAuthService>();
            services.AddSingleton<GoogleCalendarService>();
            services.AddSingleton<ICalendarService, SimpleCalendarService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        private static async Task SetupCredentialsIfNeededAsync()
        {
            var clientId = _configuration["OAuth:Google:ClientId"];
            var clientSecret = _configuration["OAuth:Google:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("Google OAuth credentials not found in configuration.");
                Console.WriteLine("Please enter your Google OAuth credentials:");

                Console.Write("Client ID: ");
                clientId = Console.ReadLine() ?? string.Empty;

                Console.Write("Client Secret: ");
                clientSecret = Console.ReadLine() ?? string.Empty;

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("Client ID and Client Secret are required.");
                }

                // Store the credentials in secure storage
                await _secureStorageService.StoreSecureValueAsync("google_calendar_client_id", clientId);
                await _secureStorageService.StoreSecureValueAsync("google_calendar_client_secret", clientSecret);

                // Update the configuration
                var configRoot = (IConfigurationRoot)_configuration;
                configRoot["OAuth:Google:ClientId"] = clientId;
                configRoot["OAuth:Google:ClientSecret"] = clientSecret;

                Console.WriteLine("Credentials saved.");
            }
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
                    recurrence.Add(Adept.Services.Calendar.RecurrenceRuleGenerator.CreateDailyRule(dailyInterval));
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
                    var daysOfWeek = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.None;

                    foreach (var choice in dayChoices)
                    {
                        if (int.TryParse(choice.Trim(), out var dayChoice))
                        {
                            switch (dayChoice)
                            {
                                case 1: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Monday; break;
                                case 2: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Tuesday; break;
                                case 3: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Wednesday; break;
                                case 4: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Thursday; break;
                                case 5: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Friday; break;
                                case 6: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Saturday; break;
                                case 7: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Sunday; break;
                                case 8: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Weekdays; break;
                                case 9: daysOfWeek |= Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Weekend; break;
                            }
                        }
                    }

                    if (daysOfWeek == Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.None)
                    {
                        daysOfWeek = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Weekdays;
                    }

                    recurrence.Add(Adept.Services.Calendar.RecurrenceRuleGenerator.CreateWeeklyRule(daysOfWeek, weeklyInterval));
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
                        var dayOfMonth = int.TryParse(Console.ReadLine(), out var dom) ? dom : startDateTime.Day;
                        recurrence.Add(Adept.Services.Calendar.RecurrenceRuleGenerator.CreateMonthlyByDayRule(dayOfMonth, monthlyInterval));
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
                        var day = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.None;

                        if (int.TryParse(dayChoice, out var dc))
                        {
                            switch (dc)
                            {
                                case 1: day = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Monday; break;
                                case 2: day = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Tuesday; break;
                                case 3: day = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Wednesday; break;
                                case 4: day = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Thursday; break;
                                case 5: day = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Friday; break;
                                case 6: day = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Saturday; break;
                                case 7: day = Adept.Services.Calendar.RecurrenceRuleGenerator.DaysOfWeek.Sunday; break;
                            }
                        }

                        recurrence.Add(Adept.Services.Calendar.RecurrenceRuleGenerator.CreateMonthlyByPositionRule(position, day, monthlyInterval));
                    }
                    break;

                case "4": // Yearly
                    Console.Write("Repeat every X years (1-10): ");
                    var yearlyInterval = int.TryParse(Console.ReadLine(), out var yi) ? yi : 1;

                    Console.Write("Month (1-12): ");
                    var month = int.TryParse(Console.ReadLine(), out var m) ? m : startDateTime.Month;

                    Console.Write("Day (1-31): ");
                    var day = int.TryParse(Console.ReadLine(), out var d) ? d : startDateTime.Day;

                    recurrence.Add(Adept.Services.Calendar.RecurrenceRuleGenerator.CreateYearlyRule(month, day, yearlyInterval));
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
                    rule = System.Text.RegularExpressions.Regex.Replace(rule, "(;COUNT=[0-9]+|;UNTIL=[0-9TZ]+)", "");
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
                    rule = System.Text.RegularExpressions.Regex.Replace(rule, "(;COUNT=[0-9]+|;UNTIL=[0-9TZ]+)", "");
                }
                rule += $";UNTIL={endDate.ToUniversalTime():yyyyMMddTHHmmssZ}";
                recurrence[0] = rule;
            }

            // Create the event
            Console.WriteLine("\nCreating recurring event...");
            var eventId = await _calendarService.CreateEventAsync(
                summary,
                description,
                location,
                startDateTime,
                endDateTime,
                "Europe/London",
                null,
                null,
                null,
                null,
                null,
                recurrence);

            Console.WriteLine($"Event created with ID: {eventId}");
            Console.WriteLine($"Recurrence rule: {recurrence[0]}");
        }

        private static async Task ListEventsAsync()
        {
            Console.WriteLine("\nListing Events:");
            Console.WriteLine("---------------");

            Console.Write("Enter start date (YYYY-MM-DD) or press Enter for today: ");
            var startDateStr = Console.ReadLine();
            var startDate = string.IsNullOrEmpty(startDateStr) ? DateTime.Today.ToString("yyyy-MM-dd") : startDateStr;

            Console.Write("Enter end date (YYYY-MM-DD) or press Enter for 30 days from start: ");
            var endDateStr = Console.ReadLine();
            var endDate = string.IsNullOrEmpty(endDateStr) ? DateTime.Today.AddDays(30).ToString("yyyy-MM-dd") : endDateStr;

            var events = await _calendarService.GetEventsForDateRangeAsync(startDate, endDate);
            var eventsList = events.ToList();

            if (eventsList.Count == 0)
            {
                Console.WriteLine("No events found.");
                return;
            }

            Console.WriteLine($"Found {eventsList.Count} events:");

            foreach (var calendarEvent in eventsList)
            {
                Console.WriteLine($"Event: {calendarEvent.Summary} ({calendarEvent.Id})");

                var startDateTime = calendarEvent.Start.DateTime ?? calendarEvent.Start.Date;
                var endDateTime = calendarEvent.End.DateTime ?? calendarEvent.End.Date;

                Console.WriteLine($"  Start: {startDateTime}");
                Console.WriteLine($"  End: {endDateTime}");

                if (!string.IsNullOrEmpty(calendarEvent.Description))
                {
                    Console.WriteLine($"  Description: {calendarEvent.Description}");
                }

                if (!string.IsNullOrEmpty(calendarEvent.Location))
                {
                    Console.WriteLine($"  Location: {calendarEvent.Location}");
                }

                if (!string.IsNullOrEmpty(calendarEvent.ColorId))
                {
                    Console.WriteLine($"  Color ID: {calendarEvent.ColorId}");
                }

                if (calendarEvent.RecurrenceRules != null && calendarEvent.RecurrenceRules.Count > 0)
                {
                    Console.WriteLine($"  Recurrence: {string.Join(", ", calendarEvent.RecurrenceRules)}");
                }

                if (!string.IsNullOrEmpty(calendarEvent.RecurringEventId))
                {
                    Console.WriteLine($"  Instance of recurring event: {calendarEvent.RecurringEventId}");
                }

                Console.WriteLine();
            }
        }

        private static async Task HandleCalendarChangesAsync(List<CalendarEvent> changedEvents, List<string> deletedEventIds)
        {
            Console.WriteLine($"\nReceived {changedEvents.Count} changed events and {deletedEventIds.Count} deleted events from Google Calendar");

            foreach (var calendarEvent in changedEvents)
            {
                Console.WriteLine($"Event: {calendarEvent.Summary} ({calendarEvent.Id})");

                var startDateTime = calendarEvent.Start.DateTime ?? calendarEvent.Start.Date;
                var endDateTime = calendarEvent.End.DateTime ?? calendarEvent.End.Date;

                Console.WriteLine($"  Start: {startDateTime}");
                Console.WriteLine($"  End: {endDateTime}");

                if (!string.IsNullOrEmpty(calendarEvent.Description))
                {
                    Console.WriteLine($"  Description: {calendarEvent.Description}");
                }

                if (!string.IsNullOrEmpty(calendarEvent.Location))
                {
                    Console.WriteLine($"  Location: {calendarEvent.Location}");
                }

                if (!string.IsNullOrEmpty(calendarEvent.ColorId))
                {
                    Console.WriteLine($"  Color ID: {calendarEvent.ColorId}");
                }

                if (calendarEvent.RecurrenceRules != null && calendarEvent.RecurrenceRules.Count > 0)
                {
                    Console.WriteLine($"  Recurrence: {string.Join(", ", calendarEvent.RecurrenceRules)}");
                }

                Console.WriteLine();
            }

            foreach (var eventId in deletedEventIds)
            {
                Console.WriteLine($"Deleted Event ID: {eventId}");
            }

            await Task.CompletedTask;
        }
    }
}
