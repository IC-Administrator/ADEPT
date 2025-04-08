using Adept.Common.Interfaces;
using Adept.Common.Models;
using Adept.Services.Calendar;
using Adept.Services.OAuth;
using Adept.Services.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GoogleCalendarTest
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
            Console.WriteLine("Google Calendar Integration Test");
            Console.WriteLine("================================");
            Console.WriteLine("1. Basic Calendar Test");
            Console.WriteLine("2. Enhanced Calendar Test");
            Console.Write("\nSelect a test to run: ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await RunBasicTestAsync();
                    break;
                case "2":
                    await EnhancedCalendarTest.Main(args);
                    break;
                default:
                    Console.WriteLine("Invalid choice. Running basic test by default.");
                    await RunBasicTestAsync();
                    break;
            }
        }

        static async Task RunBasicTestAsync()
        {
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

        public static void ConfigureServices(IServiceCollection services)
        {
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
            services.AddSingleton<ICalendarService, GoogleCalendarService>();
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

            // Configure services
            ConfigureServices(services);

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
                Console.WriteLine("\nMenu:");
                Console.WriteLine("1. List events for today");
                Console.WriteLine("2. List events for a specific date");
                Console.WriteLine("3. List events for a date range");
                Console.WriteLine("4. Create a test event");
                Console.WriteLine("5. Update a test event");
                Console.WriteLine("6. Delete an event");
                Console.WriteLine("7. Exit");

                Console.Write("\nEnter your choice: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await ListEventsForTodayAsync();
                        break;
                    case "2":
                        await ListEventsForDateAsync();
                        break;
                    case "3":
                        await ListEventsForDateRangeAsync();
                        break;
                    case "4":
                        await CreateTestEventAsync();
                        break;
                    case "5":
                        await UpdateTestEventAsync();
                        break;
                    case "6":
                        await DeleteEventAsync();
                        break;
                    case "7":
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static async Task ListEventsForTodayAsync()
        {
            try
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                Console.WriteLine($"\nListing events for today ({today}):");
                Console.WriteLine("----------------------------------");

                var events = await _calendarService.GetEventsForDateAsync(today);
                DisplayEvents(events);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task ListEventsForDateAsync()
        {
            try
            {
                Console.Write("\nEnter date (YYYY-MM-DD): ");
                var date = Console.ReadLine() ?? DateTime.Today.ToString("yyyy-MM-dd");

                Console.WriteLine($"\nListing events for {date}:");
                Console.WriteLine("---------------------------");

                var events = await _calendarService.GetEventsForDateAsync(date);
                DisplayEvents(events);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task ListEventsForDateRangeAsync()
        {
            try
            {
                Console.Write("\nEnter start date (YYYY-MM-DD): ");
                var startDate = Console.ReadLine() ?? DateTime.Today.ToString("yyyy-MM-dd");

                Console.Write("Enter end date (YYYY-MM-DD): ");
                var endDate = Console.ReadLine() ?? DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");

                Console.WriteLine($"\nListing events from {startDate} to {endDate}:");
                Console.WriteLine("------------------------------------------");

                var events = await _calendarService.GetEventsForDateRangeAsync(startDate, endDate);
                DisplayEvents(events);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task CreateTestEventAsync()
        {
            try
            {
                // Get event details
                Console.WriteLine("\nEnter event details:");

                Console.Write("Summary: ");
                var summary = Console.ReadLine() ?? "Test Event";

                Console.Write("Description: ");
                var description = Console.ReadLine() ?? "This is a test event created by the Google Calendar Integration Test.";

                Console.Write("Location: ");
                var location = Console.ReadLine() ?? "Test Location";

                Console.Write("Start date and time (YYYY-MM-DD HH:MM): ");
                var startDateTimeStr = Console.ReadLine() ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                if (!DateTime.TryParse(startDateTimeStr, out var startDateTime))
                {
                    startDateTime = DateTime.Now;
                }

                Console.Write("Duration in minutes: ");
                var durationStr = Console.ReadLine() ?? "60";
                if (!int.TryParse(durationStr, out var duration))
                {
                    duration = 60;
                }

                var endDateTime = startDateTime.AddMinutes(duration);

                Console.WriteLine("\nCreating a test event...");
                var eventId = await _calendarService.CreateEventAsync(summary, description, location, startDateTime, endDateTime);
                Console.WriteLine($"Event created with ID: {eventId}");

                // Store the event ID for later use
                await _secureStorageService.StoreSecureValueAsync("test_event_id", eventId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task UpdateTestEventAsync()
        {
            try
            {
                // Get the stored event ID
                var eventId = await _secureStorageService.RetrieveSecureValueAsync("test_event_id");
                if (string.IsNullOrEmpty(eventId))
                {
                    Console.WriteLine("No test event ID found. Please create a test event first.");
                    return;
                }

                // Get event details
                Console.WriteLine("\nEnter updated event details:");

                Console.Write("Summary: ");
                var summary = Console.ReadLine() ?? "Updated Test Event";

                Console.Write("Description: ");
                var description = Console.ReadLine() ?? "This is an updated test event.";

                Console.Write("Location: ");
                var location = Console.ReadLine() ?? "Updated Test Location";

                Console.Write("Start date and time (YYYY-MM-DD HH:MM): ");
                var startDateTimeStr = Console.ReadLine() ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                if (!DateTime.TryParse(startDateTimeStr, out var startDateTime))
                {
                    startDateTime = DateTime.Now;
                }

                Console.Write("Duration in minutes: ");
                var durationStr = Console.ReadLine() ?? "90";
                if (!int.TryParse(durationStr, out var duration))
                {
                    duration = 90;
                }

                var endDateTime = startDateTime.AddMinutes(duration);

                Console.WriteLine($"\nUpdating event with ID: {eventId}...");
                var success = await _calendarService.UpdateEventAsync(eventId, summary, description, location, startDateTime, endDateTime);
                if (success)
                {
                    Console.WriteLine("Event updated successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to update event.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task DeleteEventAsync()
        {
            try
            {
                // Get the stored event ID or ask for one
                var eventId = await _secureStorageService.RetrieveSecureValueAsync("test_event_id");
                if (string.IsNullOrEmpty(eventId))
                {
                    Console.Write("\nEnter event ID to delete: ");
                    eventId = Console.ReadLine() ?? string.Empty;
                    if (string.IsNullOrEmpty(eventId))
                    {
                        Console.WriteLine("Event ID is required.");
                        return;
                    }
                }

                Console.WriteLine($"\nDeleting event with ID: {eventId}...");
                var success = await _calendarService.DeleteEventAsync(eventId);
                if (success)
                {
                    Console.WriteLine("Event deleted successfully.");
                    await _secureStorageService.RemoveSecureValueAsync("test_event_id");
                }
                else
                {
                    Console.WriteLine("Failed to delete event.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void DisplayEvents(IEnumerable<CalendarEvent> events)
        {
            var eventsList = events.ToList();
            if (eventsList.Count == 0)
            {
                Console.WriteLine("No events found.");
                return;
            }

            foreach (var calendarEvent in eventsList)
            {
                Console.WriteLine($"Event ID: {calendarEvent.Id}");
                Console.WriteLine($"Summary: {calendarEvent.Summary}");

                if (!string.IsNullOrEmpty(calendarEvent.Description))
                {
                    Console.WriteLine($"Description: {calendarEvent.Description}");
                }

                if (!string.IsNullOrEmpty(calendarEvent.Location))
                {
                    Console.WriteLine($"Location: {calendarEvent.Location}");
                }

                var startDateTime = calendarEvent.Start.DateTime ?? calendarEvent.Start.Date;
                var endDateTime = calendarEvent.End.DateTime ?? calendarEvent.End.Date;

                Console.WriteLine($"Start: {startDateTime}");
                Console.WriteLine($"End: {endDateTime}");

                if (!string.IsNullOrEmpty(calendarEvent.HtmlLink))
                {
                    Console.WriteLine($"Link: {calendarEvent.HtmlLink}");
                }

                Console.WriteLine(new string('-', 40));
            }
        }
    }
}
