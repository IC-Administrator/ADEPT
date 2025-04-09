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
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Adept.Calendar.ManualTests
{
    /// <summary>
    /// Manual tests for calendar functionality
    /// </summary>
    class Program
    {
        private static IServiceProvider _serviceProvider = null!;
        private static IConfiguration _configuration = null!;
        private static ILogger<Program> _logger = null!;
        private static ICalendarService _calendarService = null!;
        private static ISecureStorageService _secureStorageService = null!;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Adept Calendar Manual Tests");
            Console.WriteLine("==========================");

            // Initialize services
            InitializeServices();

            // Get services
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            _calendarService = _serviceProvider.GetRequiredService<ICalendarService>();
            _secureStorageService = _serviceProvider.GetRequiredService<ISecureStorageService>();

            // Display menu
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nCalendar Test Menu:");
                Console.WriteLine("1. Test Authentication");
                Console.WriteLine("2. Test Calendar Operations");
                Console.WriteLine("3. Test Event Operations");
                Console.WriteLine("4. Test Calendar Sync");
                Console.WriteLine("5. Test Recurring Events");
                Console.WriteLine("0. Exit");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await TestAuthenticationAsync();
                            break;
                        case "2":
                            await TestCalendarOperationsAsync();
                            break;
                        case "3":
                            await TestEventOperationsAsync();
                            break;
                        case "4":
                            await TestCalendarSyncAsync();
                            break;
                        case "5":
                            await TestRecurringEventsAsync();
                            break;
                        case "0":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during test execution");
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            Console.WriteLine("\nTests completed. Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Initialize the services
        /// </summary>
        private static void InitializeServices()
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Create service collection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add configuration
            services.AddSingleton(_configuration);

            // Add HTTP client
            services.AddHttpClient();

            // Add secure storage service
            services.AddSingleton<ISecureStorageService, InMemorySecureStorageService>();

            // Add OAuth service
            services.AddSingleton<IOAuthService, GoogleOAuthService>();

            // Add calendar services
            services.AddSingleton<GoogleCalendarService>();
            services.AddSingleton<ICalendarService, SimpleCalendarService>();

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Test authentication with the calendar service
        /// </summary>
        private static async Task TestAuthenticationAsync()
        {
            Console.WriteLine("Testing Authentication");
            Console.WriteLine("=====================\n");

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
                    Console.WriteLine("Authentication failed.");
                    return;
                }
            }

            Console.WriteLine("Successfully authenticated with Google Calendar.");
        }

        /// <summary>
        /// Test calendar operations
        /// </summary>
        private static async Task TestCalendarOperationsAsync()
        {
            Console.WriteLine("Testing Calendar Operations");
            Console.WriteLine("==========================\n");

            // Ensure we're authenticated
            if (!await EnsureAuthenticatedAsync())
                return;

            // List calendars
            Console.WriteLine("Listing calendars...");
            var calendars = await _calendarService.GetCalendarsAsync();
            Console.WriteLine($"Found {calendars.Count} calendars:");
            foreach (var calendar in calendars)
            {
                Console.WriteLine($"- {calendar.Name} (ID: {calendar.Id})");
            }

            // Create a test calendar
            Console.WriteLine("\nCreating a test calendar...");
            var testCalendarName = $"Test Calendar {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var testCalendar = await _calendarService.CreateCalendarAsync(new CalendarInfo
            {
                Name = testCalendarName,
                Description = "This is a test calendar created by the Adept Calendar Manual Tests."
            });
            Console.WriteLine($"Created calendar: {testCalendar.Name} (ID: {testCalendar.Id})");

            // Get the test calendar
            Console.WriteLine("\nGetting the test calendar...");
            var retrievedCalendar = await _calendarService.GetCalendarAsync(testCalendar.Id);
            Console.WriteLine($"Retrieved calendar: {retrievedCalendar.Name} (ID: {retrievedCalendar.Id})");

            // Update the test calendar
            Console.WriteLine("\nUpdating the test calendar...");
            retrievedCalendar.Description = "This is an updated test calendar.";
            var updatedCalendar = await _calendarService.UpdateCalendarAsync(retrievedCalendar);
            Console.WriteLine($"Updated calendar: {updatedCalendar.Name} (ID: {updatedCalendar.Id})");
            Console.WriteLine($"Description: {updatedCalendar.Description}");

            // Delete the test calendar
            Console.WriteLine("\nDeleting the test calendar...");
            var deleteResult = await _calendarService.DeleteCalendarAsync(testCalendar.Id);
            Console.WriteLine($"Delete result: {deleteResult}");
        }

        /// <summary>
        /// Test event operations
        /// </summary>
        private static async Task TestEventOperationsAsync()
        {
            Console.WriteLine("Testing Event Operations");
            Console.WriteLine("=======================\n");

            // Ensure we're authenticated
            if (!await EnsureAuthenticatedAsync())
                return;

            // Get the primary calendar
            Console.WriteLine("Getting the primary calendar...");
            var calendars = await _calendarService.GetCalendarsAsync();
            var primaryCalendar = calendars.Find(c => c.IsPrimary) ?? calendars[0];
            Console.WriteLine($"Using calendar: {primaryCalendar.Name} (ID: {primaryCalendar.Id})");

            // List events
            Console.WriteLine("\nListing events...");
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now.AddDays(30);
            var events = await _calendarService.GetEventsAsync(primaryCalendar.Id, startDate, endDate);
            Console.WriteLine($"Found {events.Count} events between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}:");
            foreach (var evt in events.Take(5))
            {
                Console.WriteLine($"- {evt.Title} ({evt.StartTime:yyyy-MM-dd HH:mm} - {evt.EndTime:yyyy-MM-dd HH:mm})");
            }
            if (events.Count > 5)
            {
                Console.WriteLine($"  ... and {events.Count - 5} more events");
            }

            // Create a test event
            Console.WriteLine("\nCreating a test event...");
            var testEventTitle = $"Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var testEvent = await _calendarService.CreateEventAsync(primaryCalendar.Id, new EventInfo
            {
                Title = testEventTitle,
                Description = "This is a test event created by the Adept Calendar Manual Tests.",
                Location = "Test Location",
                StartTime = DateTime.Now.AddDays(1).Date.AddHours(10),
                EndTime = DateTime.Now.AddDays(1).Date.AddHours(11),
                Attendees = new List<AttendeeInfo>()
            });
            Console.WriteLine($"Created event: {testEvent.Title} (ID: {testEvent.Id})");
            Console.WriteLine($"Start: {testEvent.StartTime:yyyy-MM-dd HH:mm}, End: {testEvent.EndTime:yyyy-MM-dd HH:mm}");

            // Get the test event
            Console.WriteLine("\nGetting the test event...");
            var retrievedEvent = await _calendarService.GetEventAsync(primaryCalendar.Id, testEvent.Id);
            Console.WriteLine($"Retrieved event: {retrievedEvent.Title} (ID: {retrievedEvent.Id})");

            // Update the test event
            Console.WriteLine("\nUpdating the test event...");
            retrievedEvent.Description = "This is an updated test event.";
            retrievedEvent.EndTime = retrievedEvent.EndTime.AddHours(1);
            var updatedEvent = await _calendarService.UpdateEventAsync(primaryCalendar.Id, retrievedEvent);
            Console.WriteLine($"Updated event: {updatedEvent.Title} (ID: {updatedEvent.Id})");
            Console.WriteLine($"Description: {updatedEvent.Description}");
            Console.WriteLine($"Start: {updatedEvent.StartTime:yyyy-MM-dd HH:mm}, End: {updatedEvent.EndTime:yyyy-MM-dd HH:mm}");

            // Delete the test event
            Console.WriteLine("\nDeleting the test event...");
            var deleteResult = await _calendarService.DeleteEventAsync(primaryCalendar.Id, testEvent.Id);
            Console.WriteLine($"Delete result: {deleteResult}");
        }

        /// <summary>
        /// Test calendar synchronization
        /// </summary>
        private static async Task TestCalendarSyncAsync()
        {
            Console.WriteLine("Testing Calendar Sync");
            Console.WriteLine("====================\n");

            // Ensure we're authenticated
            if (!await EnsureAuthenticatedAsync())
                return;

            // Get the primary calendar
            Console.WriteLine("Getting the primary calendar...");
            var calendars = await _calendarService.GetCalendarsAsync();
            var primaryCalendar = calendars.Find(c => c.IsPrimary) ?? calendars[0];
            Console.WriteLine($"Using calendar: {primaryCalendar.Name} (ID: {primaryCalendar.Id})");

            // Get the sync token
            Console.WriteLine("\nGetting initial sync token...");
            var syncToken = await _calendarService.GetSyncTokenAsync(primaryCalendar.Id);
            Console.WriteLine($"Initial sync token: {syncToken}");

            // Create a test event
            Console.WriteLine("\nCreating a test event to trigger a sync...");
            var testEventTitle = $"Sync Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var testEvent = await _calendarService.CreateEventAsync(primaryCalendar.Id, new EventInfo
            {
                Title = testEventTitle,
                Description = "This is a test event for calendar sync.",
                StartTime = DateTime.Now.AddDays(2).Date.AddHours(14),
                EndTime = DateTime.Now.AddDays(2).Date.AddHours(15)
            });
            Console.WriteLine($"Created event: {testEvent.Title} (ID: {testEvent.Id})");

            // Sync changes
            Console.WriteLine("\nSyncing changes...");
            var changes = await _calendarService.SyncEventsAsync(primaryCalendar.Id, syncToken);
            Console.WriteLine($"Sync results:");
            Console.WriteLine($"- New sync token: {changes.NextSyncToken}");
            Console.WriteLine($"- Added events: {changes.AddedEvents.Count}");
            Console.WriteLine($"- Updated events: {changes.UpdatedEvents.Count}");
            Console.WriteLine($"- Deleted events: {changes.DeletedEvents.Count}");

            // Display added events
            if (changes.AddedEvents.Count > 0)
            {
                Console.WriteLine("\nAdded events:");
                foreach (var evt in changes.AddedEvents)
                {
                    Console.WriteLine($"- {evt.Title} ({evt.StartTime:yyyy-MM-dd HH:mm} - {evt.EndTime:yyyy-MM-dd HH:mm})");
                }
            }

            // Clean up
            Console.WriteLine("\nCleaning up test event...");
            await _calendarService.DeleteEventAsync(primaryCalendar.Id, testEvent.Id);
            Console.WriteLine("Test event deleted.");
        }

        /// <summary>
        /// Test recurring events
        /// </summary>
        private static async Task TestRecurringEventsAsync()
        {
            Console.WriteLine("Testing Recurring Events");
            Console.WriteLine("=======================\n");

            // Ensure we're authenticated
            if (!await EnsureAuthenticatedAsync())
                return;

            // Get the primary calendar
            Console.WriteLine("Getting the primary calendar...");
            var calendars = await _calendarService.GetCalendarsAsync();
            var primaryCalendar = calendars.Find(c => c.IsPrimary) ?? calendars[0];
            Console.WriteLine($"Using calendar: {primaryCalendar.Name} (ID: {primaryCalendar.Id})");

            // Create a recurring event
            Console.WriteLine("\nCreating a recurring event...");
            var testEventTitle = $"Recurring Test Event {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var testEvent = await _calendarService.CreateEventAsync(primaryCalendar.Id, new EventInfo
            {
                Title = testEventTitle,
                Description = "This is a recurring test event.",
                Location = "Test Location",
                StartTime = DateTime.Now.AddDays(1).Date.AddHours(9),
                EndTime = DateTime.Now.AddDays(1).Date.AddHours(10),
                RecurrenceRule = "RRULE:FREQ=WEEKLY;COUNT=4;BYDAY=MO,WE,FR",
                Attendees = new List<AttendeeInfo>()
            });
            Console.WriteLine($"Created recurring event: {testEvent.Title} (ID: {testEvent.Id})");
            Console.WriteLine($"Recurrence rule: {testEvent.RecurrenceRule}");

            // Get instances of the recurring event
            Console.WriteLine("\nGetting instances of the recurring event...");
            var startDate = DateTime.Now;
            var endDate = DateTime.Now.AddDays(30);
            var instances = await _calendarService.GetEventInstancesAsync(primaryCalendar.Id, testEvent.Id, startDate, endDate);
            Console.WriteLine($"Found {instances.Count} instances between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}:");
            foreach (var instance in instances)
            {
                Console.WriteLine($"- {instance.Title} ({instance.StartTime:yyyy-MM-dd HH:mm} - {instance.EndTime:yyyy-MM-dd HH:mm})");
                Console.WriteLine($"  Instance ID: {instance.Id}");
            }

            // Update a single instance
            if (instances.Count > 0)
            {
                Console.WriteLine("\nUpdating a single instance...");
                var instance = instances[0];
                instance.Title = $"{instance.Title} (Modified)";
                instance.Description = "This instance has been modified.";
                var updatedInstance = await _calendarService.UpdateEventInstanceAsync(primaryCalendar.Id, instance);
                Console.WriteLine($"Updated instance: {updatedInstance.Title}");
                Console.WriteLine($"Description: {updatedInstance.Description}");
            }

            // Delete the recurring event
            Console.WriteLine("\nDeleting the recurring event...");
            var deleteResult = await _calendarService.DeleteEventAsync(primaryCalendar.Id, testEvent.Id);
            Console.WriteLine($"Delete result: {deleteResult}");
        }

        /// <summary>
        /// Ensure that we're authenticated with the calendar service
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        private static async Task<bool> EnsureAuthenticatedAsync()
        {
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
                    Console.WriteLine("Authentication failed.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Set up credentials if needed
        /// </summary>
        private static async Task SetupCredentialsIfNeededAsync()
        {
            // Get OAuth configuration
            var clientId = _configuration["OAuth:Google:ClientId"];
            var clientSecret = _configuration["OAuth:Google:ClientSecret"];

            // Check if credentials are configured
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("OAuth credentials are not configured in appsettings.json.");
                Console.WriteLine("Please enter your Google OAuth credentials:");

                Console.Write("Client ID: ");
                clientId = Console.ReadLine() ?? "";

                Console.Write("Client Secret: ");
                clientSecret = Console.ReadLine() ?? "";

                // Store credentials securely
                await _secureStorageService.StoreSecretAsync("GoogleOAuth:ClientId", clientId);
                await _secureStorageService.StoreSecretAsync("GoogleOAuth:ClientSecret", clientSecret);
            }
        }
    }

    /// <summary>
    /// A simplified calendar service for testing
    /// </summary>
    public class SimpleCalendarService : ICalendarService
    {
        private readonly GoogleCalendarService _googleCalendarService;
        private readonly ILogger<SimpleCalendarService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleCalendarService"/> class
        /// </summary>
        /// <param name="googleCalendarService">The Google Calendar service</param>
        /// <param name="logger">The logger</param>
        public SimpleCalendarService(
            GoogleCalendarService googleCalendarService,
            ILogger<SimpleCalendarService> logger)
        {
            _googleCalendarService = googleCalendarService;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the calendar service
        /// </summary>
        public async Task InitializeAsync()
        {
            await _googleCalendarService.InitializeAsync();
        }

        /// <summary>
        /// Authenticates with the calendar service
        /// </summary>
        /// <returns>True if authentication was successful, false otherwise</returns>
        public async Task<bool> AuthenticateAsync()
        {
            return await _googleCalendarService.AuthenticateAsync();
        }

        /// <summary>
        /// Checks if the user is authenticated with the calendar service
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        public async Task<bool> IsAuthenticatedAsync()
        {
            return await _googleCalendarService.IsAuthenticatedAsync();
        }

        /// <summary>
        /// Gets a list of calendars
        /// </summary>
        /// <returns>A list of calendars</returns>
        public async Task<List<CalendarInfo>> GetCalendarsAsync()
        {
            return await _googleCalendarService.GetCalendarsAsync();
        }

        /// <summary>
        /// Gets a calendar by ID
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <returns>The calendar information</returns>
        public async Task<CalendarInfo> GetCalendarAsync(string calendarId)
        {
            return await _googleCalendarService.GetCalendarAsync(calendarId);
        }

        /// <summary>
        /// Creates a new calendar
        /// </summary>
        /// <param name="calendar">The calendar information</param>
        /// <returns>The created calendar</returns>
        public async Task<CalendarInfo> CreateCalendarAsync(CalendarInfo calendar)
        {
            return await _googleCalendarService.CreateCalendarAsync(calendar);
        }

        /// <summary>
        /// Updates a calendar
        /// </summary>
        /// <param name="calendar">The calendar information</param>
        /// <returns>The updated calendar</returns>
        public async Task<CalendarInfo> UpdateCalendarAsync(CalendarInfo calendar)
        {
            return await _googleCalendarService.UpdateCalendarAsync(calendar);
        }

        /// <summary>
        /// Deletes a calendar
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteCalendarAsync(string calendarId)
        {
            return await _googleCalendarService.DeleteCalendarAsync(calendarId);
        }

        /// <summary>
        /// Gets a list of events from a calendar
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <returns>A list of events</returns>
        public async Task<List<EventInfo>> GetEventsAsync(string calendarId, DateTime startTime, DateTime endTime)
        {
            return await _googleCalendarService.GetEventsAsync(calendarId, startTime, endTime);
        }

        /// <summary>
        /// Gets an event by ID
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <param name="eventId">The event ID</param>
        /// <returns>The event information</returns>
        public async Task<EventInfo> GetEventAsync(string calendarId, string eventId)
        {
            return await _googleCalendarService.GetEventAsync(calendarId, eventId);
        }

        /// <summary>
        /// Creates a new event
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <param name="eventInfo">The event information</param>
        /// <returns>The created event</returns>
        public async Task<EventInfo> CreateEventAsync(string calendarId, EventInfo eventInfo)
        {
            return await _googleCalendarService.CreateEventAsync(calendarId, eventInfo);
        }

        /// <summary>
        /// Updates an event
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <param name="eventInfo">The event information</param>
        /// <returns>The updated event</returns>
        public async Task<EventInfo> UpdateEventAsync(string calendarId, EventInfo eventInfo)
        {
            return await _googleCalendarService.UpdateEventAsync(calendarId, eventInfo);
        }

        /// <summary>
        /// Deletes an event
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <param name="eventId">The event ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteEventAsync(string calendarId, string eventId)
        {
            return await _googleCalendarService.DeleteEventAsync(calendarId, eventId);
        }

        /// <summary>
        /// Gets a sync token for a calendar
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <returns>The sync token</returns>
        public async Task<string> GetSyncTokenAsync(string calendarId)
        {
            return await _googleCalendarService.GetSyncTokenAsync(calendarId);
        }

        /// <summary>
        /// Syncs events from a calendar
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <param name="syncToken">The sync token</param>
        /// <returns>The sync results</returns>
        public async Task<SyncResult> SyncEventsAsync(string calendarId, string syncToken)
        {
            return await _googleCalendarService.SyncEventsAsync(calendarId, syncToken);
        }

        /// <summary>
        /// Gets instances of a recurring event
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <param name="eventId">The event ID</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <returns>A list of event instances</returns>
        public async Task<List<EventInfo>> GetEventInstancesAsync(string calendarId, string eventId, DateTime startTime, DateTime endTime)
        {
            return await _googleCalendarService.GetEventInstancesAsync(calendarId, eventId, startTime, endTime);
        }

        /// <summary>
        /// Updates an instance of a recurring event
        /// </summary>
        /// <param name="calendarId">The calendar ID</param>
        /// <param name="eventInfo">The event information</param>
        /// <returns>The updated event instance</returns>
        public async Task<EventInfo> UpdateEventInstanceAsync(string calendarId, EventInfo eventInfo)
        {
            return await _googleCalendarService.UpdateEventInstanceAsync(calendarId, eventInfo);
        }
    }

    /// <summary>
    /// A simple in-memory secure storage service for testing
    /// </summary>
    public class InMemorySecureStorageService : ISecureStorageService
    {
        private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();

        /// <summary>
        /// Stores a secret
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public Task StoreSecretAsync(string key, string value)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a secret
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The value</returns>
        public Task<string> GetSecretAsync(string key)
        {
            if (_storage.TryGetValue(key, out var value))
            {
                return Task.FromResult(value);
            }
            return Task.FromResult(string.Empty);
        }

        /// <summary>
        /// Deletes a secret
        /// </summary>
        /// <param name="key">The key</param>
        public Task DeleteSecretAsync(string key)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if a secret exists
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>True if the secret exists, false otherwise</returns>
        public Task<bool> SecretExistsAsync(string key)
        {
            return Task.FromResult(_storage.ContainsKey(key));
        }
    }
}
