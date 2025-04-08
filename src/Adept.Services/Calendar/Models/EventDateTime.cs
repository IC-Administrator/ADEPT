namespace Adept.Services.Calendar.Models
{
    /// <summary>
    /// Represents a date and time for a calendar event
    /// </summary>
    public class EventDateTime
    {
        /// <summary>
        /// Gets or sets the date and time
        /// </summary>
        public string? DateTime { get; set; }

        /// <summary>
        /// Gets or sets the date
        /// </summary>
        public string? Date { get; set; }

        /// <summary>
        /// Gets or sets the time zone
        /// </summary>
        public string? TimeZone { get; set; }
    }
}
