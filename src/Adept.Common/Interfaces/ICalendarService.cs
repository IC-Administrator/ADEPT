using Adept.Common.Models;

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
        /// <returns>The ID of the created event</returns>
        Task<string> CreateEventAsync(string summary, string description, string location, DateTime startDateTime, DateTime endDateTime, string timeZone = "Europe/London");

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
        /// <returns>True if the update was successful, false otherwise</returns>
        Task<bool> UpdateEventAsync(string eventId, string summary, string description, string location, DateTime startDateTime, DateTime endDateTime, string timeZone = "Europe/London");

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
    }
}
