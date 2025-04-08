using Adept.Common.Interfaces;
using Adept.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCalendarTest
{
    /// <summary>
    /// Test for enhanced Google Calendar integration
    /// </summary>
    public class EnhancedCalendarTest
    {
        private static IServiceProvider _serviceProvider = null!;
        private static ICalendarService _calendarService = null!;
        private static ISecureStorageService _secureStorageService = null!;
        private static ILogger<EnhancedCalendarTest> _logger = null!;

        /// <summary>
        /// Main entry point
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Enhanced Google Calendar Integration Test");
            Console.WriteLine("========================================");

            // Initialize services
            InitializeServices();

            try
            {
                // Get services
                _logger = _serviceProvider.GetRequiredService<ILogger<EnhancedCalendarTest>>();
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

        /// <summary>
        /// Initializes the services
        /// </summary>
        private static void InitializeServices()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add services
            Program.ConfigureServices(services);

            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Sets up credentials if needed
        /// </summary>
        private static async Task SetupCredentialsIfNeededAsync()
        {
            var clientId = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_client_id");
            var clientSecret = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_client_secret");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("OAuth credentials not found. Please enter your credentials:");
                
                Console.Write("Client ID: ");
                clientId = Console.ReadLine() ?? string.Empty;
                
                Console.Write("Client Secret: ");
                clientSecret = Console.ReadLine() ?? string.Empty;

                if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    await _secureStorageService.StoreSecureValueAsync("google_calendar_client_id", clientId);
                    await _secureStorageService.StoreSecureValueAsync("google_calendar_client_secret", clientSecret);
                    Console.WriteLine("Credentials saved.");
                }
                else
                {
                    Console.WriteLine("Invalid credentials. Exiting.");
                    Environment.Exit(1);
                }
            }
        }

        /// <summary>
        /// Shows the menu
        /// </summary>
        private static async Task ShowMenuAsync()
        {
            while (true)
            {
                Console.WriteLine("\nEnhanced Calendar Features Menu:");
                Console.WriteLine("1. Test Color Coding");
                Console.WriteLine("2. Test Reminders");
                Console.WriteLine("3. Test Attendees");
                Console.WriteLine("4. Test Visibility");
                Console.WriteLine("5. Test All Features");
                Console.WriteLine("6. Exit");

                Console.Write("\nEnter your choice: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await TestColorCodingAsync();
                        break;
                    case "2":
                        await TestRemindersAsync();
                        break;
                    case "3":
                        await TestAttendeesAsync();
                        break;
                    case "4":
                        await TestVisibilityAsync();
                        break;
                    case "5":
                        await TestAllFeaturesAsync();
                        break;
                    case "6":
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        /// <summary>
        /// Tests color coding
        /// </summary>
        private static async Task TestColorCodingAsync()
        {
            try
            {
                Console.WriteLine("\nTesting Color Coding:");
                Console.WriteLine("--------------------");

                // Get the color palette
                var colorPalette = await _calendarService.GetColorPaletteAsync();
                Console.WriteLine($"Retrieved {colorPalette.Count} colors from the API.");

                // Display the colors
                Console.WriteLine("\nAvailable Colors:");
                foreach (var color in colorPalette)
                {
                    Console.WriteLine($"  Color ID: {color.Key}, Background: {color.Value.Background}, Foreground: {color.Value.Foreground}");
                }

                // Ask the user to select a color
                Console.Write("\nEnter a color ID to use (or press Enter for default): ");
                var colorId = Console.ReadLine();

                // Create an event with the selected color
                var now = DateTime.Now;
                var startTime = now.AddHours(1);
                var endTime = startTime.AddHours(1);

                var eventId = await _calendarService.CreateEventAsync(
                    "Test Color Coded Event",
                    "This is a test event with color coding",
                    "Test Location",
                    startTime,
                    endTime,
                    "Europe/London",
                    colorId);

                if (!string.IsNullOrEmpty(eventId))
                {
                    Console.WriteLine($"Event created with ID: {eventId}");
                    Console.WriteLine($"Color ID: {colorId ?? "Default"}");

                    // Store the event ID for later use
                    await _secureStorageService.StoreSecureValueAsync("test_event_id", eventId);
                }
                else
                {
                    Console.WriteLine("Failed to create event.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests reminders
        /// </summary>
        private static async Task TestRemindersAsync()
        {
            try
            {
                Console.WriteLine("\nTesting Reminders:");
                Console.WriteLine("-----------------");

                // Create reminders
                var reminders = new CalendarReminders
                {
                    UseDefault = false,
                    Overrides = new List<CalendarReminder>
                    {
                        new CalendarReminder { Method = "email", Minutes = 30 },
                        new CalendarReminder { Method = "popup", Minutes = 10 }
                    }
                };

                // Create an event with reminders
                var now = DateTime.Now;
                var startTime = now.AddHours(2);
                var endTime = startTime.AddHours(1);

                var eventId = await _calendarService.CreateEventAsync(
                    "Test Event with Reminders",
                    "This is a test event with custom reminders",
                    "Test Location",
                    startTime,
                    endTime,
                    "Europe/London",
                    null,
                    reminders);

                if (!string.IsNullOrEmpty(eventId))
                {
                    Console.WriteLine($"Event created with ID: {eventId}");
                    Console.WriteLine("Reminders: Email (30 minutes before), Popup (10 minutes before)");

                    // Store the event ID for later use
                    await _secureStorageService.StoreSecureValueAsync("test_reminder_event_id", eventId);
                }
                else
                {
                    Console.WriteLine("Failed to create event.");
                }

                // Test adding a reminder to an existing event
                var existingEventId = await _secureStorageService.RetrieveSecureValueAsync("test_event_id");
                if (!string.IsNullOrEmpty(existingEventId))
                {
                    Console.WriteLine("\nAdding a reminder to an existing event...");
                    var success = await _calendarService.AddReminderAsync(existingEventId, "popup", 5);
                    if (success)
                    {
                        Console.WriteLine("Reminder added successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to add reminder.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests attendees
        /// </summary>
        private static async Task TestAttendeesAsync()
        {
            try
            {
                Console.WriteLine("\nTesting Attendees:");
                Console.WriteLine("-----------------");

                // Ask for attendee email
                Console.Write("Enter an email address to add as an attendee: ");
                var email = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(email))
                {
                    Console.WriteLine("No email provided. Skipping attendee test.");
                    return;
                }

                // Create attendees list
                var attendees = new List<CalendarAttendee>
                {
                    new CalendarAttendee
                    {
                        Email = email,
                        DisplayName = "Test Attendee",
                        Optional = false,
                        ResponseStatus = "needsAction"
                    }
                };

                // Create an event with attendees
                var now = DateTime.Now;
                var startTime = now.AddHours(3);
                var endTime = startTime.AddHours(1);

                var eventId = await _calendarService.CreateEventAsync(
                    "Test Event with Attendees",
                    "This is a test event with attendees",
                    "Test Location",
                    startTime,
                    endTime,
                    "Europe/London",
                    null,
                    null,
                    attendees);

                if (!string.IsNullOrEmpty(eventId))
                {
                    Console.WriteLine($"Event created with ID: {eventId}");
                    Console.WriteLine($"Attendee: {email}");

                    // Store the event ID for later use
                    await _secureStorageService.StoreSecureValueAsync("test_attendee_event_id", eventId);
                }
                else
                {
                    Console.WriteLine("Failed to create event.");
                }

                // Test adding an attendee to an existing event
                var existingEventId = await _secureStorageService.RetrieveSecureValueAsync("test_event_id");
                if (!string.IsNullOrEmpty(existingEventId))
                {
                    Console.WriteLine("\nAdding an attendee to an existing event...");
                    Console.Write("Enter another email address: ");
                    var anotherEmail = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(anotherEmail))
                    {
                        var success = await _calendarService.AddAttendeeAsync(existingEventId, anotherEmail, "Another Test Attendee");
                        if (success)
                        {
                            Console.WriteLine("Attendee added successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Failed to add attendee.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests visibility
        /// </summary>
        private static async Task TestVisibilityAsync()
        {
            try
            {
                Console.WriteLine("\nTesting Visibility:");
                Console.WriteLine("------------------");

                // Ask for visibility
                Console.WriteLine("Available visibility options: default, public, private, confidential");
                Console.Write("Enter visibility option: ");
                var visibility = Console.ReadLine()?.ToLower();
                if (string.IsNullOrWhiteSpace(visibility) || 
                    (visibility != "default" && visibility != "public" && visibility != "private" && visibility != "confidential"))
                {
                    visibility = "default";
                    Console.WriteLine("Using default visibility.");
                }

                // Create an event with the specified visibility
                var now = DateTime.Now;
                var startTime = now.AddHours(4);
                var endTime = startTime.AddHours(1);

                var eventId = await _calendarService.CreateEventAsync(
                    "Test Event with Visibility",
                    "This is a test event with custom visibility",
                    "Test Location",
                    startTime,
                    endTime,
                    "Europe/London",
                    null,
                    null,
                    null,
                    null,
                    visibility);

                if (!string.IsNullOrEmpty(eventId))
                {
                    Console.WriteLine($"Event created with ID: {eventId}");
                    Console.WriteLine($"Visibility: {visibility}");

                    // Store the event ID for later use
                    await _secureStorageService.StoreSecureValueAsync("test_visibility_event_id", eventId);
                }
                else
                {
                    Console.WriteLine("Failed to create event.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests all features
        /// </summary>
        private static async Task TestAllFeaturesAsync()
        {
            try
            {
                Console.WriteLine("\nTesting All Features:");
                Console.WriteLine("-------------------");

                // Get the color palette
                var colorPalette = await _calendarService.GetColorPaletteAsync();
                var colorId = colorPalette.Count > 0 ? colorPalette.Keys.First() : null;

                // Create reminders
                var reminders = new CalendarReminders
                {
                    UseDefault = false,
                    Overrides = new List<CalendarReminder>
                    {
                        new CalendarReminder { Method = "email", Minutes = 30 },
                        new CalendarReminder { Method = "popup", Minutes = 10 }
                    }
                };

                // Ask for attendee email
                Console.Write("Enter an email address to add as an attendee: ");
                var email = Console.ReadLine();
                List<CalendarAttendee>? attendees = null;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    attendees = new List<CalendarAttendee>
                    {
                        new CalendarAttendee
                        {
                            Email = email,
                            DisplayName = "Test Attendee",
                            Optional = false,
                            ResponseStatus = "needsAction"
                        }
                    };
                }

                // Create an event with all features
                var now = DateTime.Now;
                var startTime = now.AddHours(5);
                var endTime = startTime.AddHours(1);

                var eventId = await _calendarService.CreateEventAsync(
                    "Test Event with All Features",
                    "This is a test event with all enhanced features: color coding, reminders, attendees, and visibility",
                    "Test Location",
                    startTime,
                    endTime,
                    "Europe/London",
                    colorId,
                    reminders,
                    attendees,
                    null,
                    "private");

                if (!string.IsNullOrEmpty(eventId))
                {
                    Console.WriteLine($"Event created with ID: {eventId}");
                    Console.WriteLine($"Color ID: {colorId ?? "Default"}");
                    Console.WriteLine("Reminders: Email (30 minutes before), Popup (10 minutes before)");
                    if (attendees != null)
                    {
                        Console.WriteLine($"Attendee: {email}");
                    }
                    Console.WriteLine("Visibility: private");

                    // Store the event ID for later use
                    await _secureStorageService.StoreSecureValueAsync("test_all_features_event_id", eventId);
                }
                else
                {
                    Console.WriteLine("Failed to create event.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
