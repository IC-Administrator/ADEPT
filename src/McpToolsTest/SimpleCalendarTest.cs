using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpToolsTest
{
    class SimpleCalendarTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Simple Calendar Test");
            Console.WriteLine("===================");

            // Test calendar operations with mock data
            await TestCalendarOperationsAsync();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestCalendarOperationsAsync()
        {
            Console.WriteLine("\nTesting Calendar Operations (Mock):");
            Console.WriteLine("----------------------------------");

            try
            {
                // Mock listing calendars
                Console.WriteLine("\nListing Calendars:");
                Console.WriteLine("-----------------");
                
                var mockCalendars = new List<(string Id, string Summary)>
                {
                    ("primary", "Primary Calendar"),
                    ("work@example.com", "Work Calendar"),
                    ("family@example.com", "Family Calendar"),
                    ("holidays@example.com", "Holidays")
                };

                Console.WriteLine("Your calendars:");
                foreach (var calendar in mockCalendars)
                {
                    Console.WriteLine($"  • {calendar.Summary} (ID: {calendar.Id})");
                }

                // Mock listing events
                Console.WriteLine("\nListing Events:");
                Console.WriteLine("--------------");
                
                var now = DateTime.Now;
                var mockEvents = new List<(string Id, string Summary, DateTime Start, DateTime End)>
                {
                    ("event1", "Team Meeting", now.AddDays(1).AddHours(10), now.AddDays(1).AddHours(11)),
                    ("event2", "Lunch with Client", now.AddDays(2).AddHours(12), now.AddDays(2).AddHours(13.5)),
                    ("event3", "Project Deadline", now.AddDays(3).AddHours(17), now.AddDays(3).AddHours(18)),
                    ("event4", "Dentist Appointment", now.AddDays(5).AddHours(9), now.AddDays(5).AddHours(10))
                };

                Console.WriteLine("Upcoming events (next 7 days):");
                foreach (var eventItem in mockEvents)
                {
                    Console.WriteLine($"  • {eventItem.Summary}");
                    Console.WriteLine($"    Start: {eventItem.Start:yyyy-MM-dd HH:mm}");
                    Console.WriteLine($"    End: {eventItem.End:yyyy-MM-dd HH:mm}");
                    Console.WriteLine($"    ID: {eventItem.Id}");
                    Console.WriteLine();
                }

                // Mock creating an event
                Console.WriteLine("\nCreating a Test Event:");
                Console.WriteLine("--------------------");
                
                var startTime = now.AddDays(1).AddHours(14);
                var endTime = startTime.AddHours(1);
                var eventId = Guid.NewGuid().ToString("N");
                
                var eventData = new
                {
                    id = eventId,
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
                    },
                    htmlLink = $"https://calendar.google.com/calendar/event?eid={eventId}"
                };

                var eventJson = JsonSerializer.Serialize(eventData, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Event created successfully!");
                Console.WriteLine($"  Event ID: {eventId}");
                Console.WriteLine($"  Event Link: {eventData.htmlLink}");
                Console.WriteLine($"  Start Time: {startTime:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"  End Time: {endTime:yyyy-MM-dd HH:mm}");
                
                Console.WriteLine("\nEvent JSON:");
                Console.WriteLine(eventJson);

                // Mock updating an event
                Console.WriteLine("\nUpdating an Event:");
                Console.WriteLine("-----------------");
                
                var updatedStartTime = startTime.AddHours(1);
                var updatedEndTime = endTime.AddHours(1);
                
                var updatedEventData = new
                {
                    id = eventId,
                    summary = "Updated Test Event",
                    description = "This event has been updated by the Calendar API Test application.",
                    start = new
                    {
                        dateTime = updatedStartTime.ToString("o"),
                        timeZone = "UTC"
                    },
                    end = new
                    {
                        dateTime = updatedEndTime.ToString("o"),
                        timeZone = "UTC"
                    },
                    htmlLink = $"https://calendar.google.com/calendar/event?eid={eventId}"
                };

                var updatedEventJson = JsonSerializer.Serialize(updatedEventData, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Event updated successfully!");
                Console.WriteLine($"  Event ID: {eventId}");
                Console.WriteLine($"  New Summary: {updatedEventData.summary}");
                Console.WriteLine($"  New Start Time: {updatedStartTime:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"  New End Time: {updatedEndTime:yyyy-MM-dd HH:mm}");

                // Mock deleting an event
                Console.WriteLine("\nDeleting an Event:");
                Console.WriteLine("-----------------");
                Console.WriteLine($"Event with ID {eventId} deleted successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
