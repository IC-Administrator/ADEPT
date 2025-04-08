using Adept.Common.Models;
using System.Collections.Generic;

namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Interface for calendar service operations
    /// </summary>
    public interface ICalendarService
    {
        /// <summary>
        /// Initializes the calendar service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Checks if the calendar service is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Starts the OAuth authentication process
        /// </summary>
        /// <returns>True if authentication was successful, false otherwise</returns>
        Task<bool> AuthenticateAsync();

        /// <summary>
        /// Creates a calendar event
        /// </summary>
        /// <param name="summary">The event summary</param>
        /// <param name="description">The event description</param>
        /// <param name="location">The event location</param>
        /// <param name="startDateTime">The start date and time</param>
        /// <param name="endDateTime">The end date and time</param>
        /// <param name="timeZone">The timezone</param>
        /// <param name="colorId">The color ID for the event</param>
        /// <param name="reminders">The reminders for the event</param>
        /// <param name="attendees">The attendees for the event</param>
        /// <param name="attachments">The attachments for the event</param>
        /// <param name="visibility">The visibility of the event (default, public, private, confidential)</param>
        /// <returns>The ID of the created event</returns>
        Task<string> CreateEventAsync(string summary, string description, string location, DateTime startDateTime, DateTime endDateTime, string timeZone = "Europe/London", string? colorId = null, CalendarReminders? reminders = null, List<CalendarAttendee>? attendees = null, List<CalendarAttachment>? attachments = null, string? visibility = null);

        /// <summary>
        /// Updates a calendar event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="summary">The event summary</param>
        /// <param name="description">The event description</param>
        /// <param name="location">The event location</param>
        /// <param name="startDateTime">The start date and time</param>
        /// <param name="endDateTime">The end date and time</param>
        /// <param name="timeZone">The timezone</param>
        /// <param name="colorId">The color ID for the event</param>
        /// <param name="reminders">The reminders for the event</param>
        /// <param name="attendees">The attendees for the event</param>
        /// <param name="attachments">The attachments for the event</param>
        /// <param name="visibility">The visibility of the event (default, public, private, confidential)</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        Task<bool> UpdateEventAsync(string eventId, string summary, string description, string location, DateTime startDateTime, DateTime endDateTime, string timeZone = "Europe/London", string? colorId = null, CalendarReminders? reminders = null, List<CalendarAttendee>? attendees = null, List<CalendarAttachment>? attachments = null, string? visibility = null);

        /// <summary>
        /// Deletes a calendar event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        Task<bool> DeleteEventAsync(string eventId);

        /// <summary>
        /// Gets events for a specific date
        /// </summary>
        /// <param name="date">The date in YYYY-MM-DD format</param>
        /// <returns>A collection of calendar events</returns>
        Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(string date);

        /// <summary>
        /// Gets events for a date range
        /// </summary>
        /// <param name="startDate">The start date in YYYY-MM-DD format</param>
        /// <param name="endDate">The end date in YYYY-MM-DD format</param>
        /// <returns>A collection of calendar events</returns>
        Task<IEnumerable<CalendarEvent>> GetEventsForDateRangeAsync(string startDate, string endDate);

        /// <summary>
        /// Gets the calendar ID for the primary calendar
        /// </summary>
        /// <returns>The calendar ID</returns>
        Task<string> GetPrimaryCalendarIdAsync();

        /// <summary>
        /// Gets the available color IDs for events
        /// </summary>
        /// <returns>A dictionary of color IDs and their corresponding colors</returns>
        Task<Dictionary<string, CalendarColorDefinition>> GetColorPaletteAsync();

        /// <summary>
        /// Adds a reminder to an event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="method">The reminder method (email, popup)</param>
        /// <param name="minutes">The minutes before the event to trigger the reminder</param>
        /// <returns>True if the reminder was added successfully, false otherwise</returns>
        Task<bool> AddReminderAsync(string eventId, string method, int minutes);

        /// <summary>
        /// Adds an attendee to an event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="email">The attendee's email</param>
        /// <param name="displayName">The attendee's display name</param>
        /// <param name="optional">Whether the attendee is optional</param>
        /// <returns>True if the attendee was added successfully, false otherwise</returns>
        Task<bool> AddAttendeeAsync(string eventId, string email, string? displayName = null, bool optional = false);

        /// <summary>
        /// Adds an attachment to an event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="fileUrl">The file URL</param>
        /// <param name="title">The attachment title</param>
        /// <param name="mimeType">The MIME type</param>
        /// <returns>True if the attachment was added successfully, false otherwise</returns>
        Task<bool> AddAttachmentAsync(string eventId, string fileUrl, string title, string mimeType);
    }

    /// <summary>
    /// Represents a color definition for a calendar event
    /// </summary>
    public class CalendarColorDefinition
    {
        /// <summary>
        /// The background color
        /// </summary>
        public string Background { get; set; } = string.Empty;

        /// <summary>
        /// The foreground color
        /// </summary>
        public string Foreground { get; set; } = string.Empty;
    }
}
