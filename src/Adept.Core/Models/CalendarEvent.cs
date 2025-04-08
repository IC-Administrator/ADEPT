using System;
using System.Text.Json.Serialization;

namespace Adept.Core.Models
{
    /// <summary>
    /// Represents a calendar event
    /// </summary>
    public class CalendarEvent
    {
        /// <summary>
        /// The event ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The event summary (title)
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// The event description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The event location
        /// </summary>
        [JsonPropertyName("location")]
        public string? Location { get; set; }

        /// <summary>
        /// The start date and time
        /// </summary>
        [JsonPropertyName("start")]
        public CalendarDateTime Start { get; set; } = new CalendarDateTime();

        /// <summary>
        /// The end date and time
        /// </summary>
        [JsonPropertyName("end")]
        public CalendarDateTime End { get; set; } = new CalendarDateTime();

        /// <summary>
        /// The HTML link to the event
        /// </summary>
        [JsonPropertyName("htmlLink")]
        public string? HtmlLink { get; set; }

        /// <summary>
        /// The event status
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// The event creator
        /// </summary>
        [JsonPropertyName("creator")]
        public CalendarPerson? Creator { get; set; }

        /// <summary>
        /// The event organizer
        /// </summary>
        [JsonPropertyName("organizer")]
        public CalendarPerson? Organizer { get; set; }

        /// <summary>
        /// When the event was created
        /// </summary>
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// When the event was last updated
        /// </summary>
        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }
    }

    /// <summary>
    /// Represents a date and time in a calendar event
    /// </summary>
    public class CalendarDateTime
    {
        /// <summary>
        /// The date and time in ISO format
        /// </summary>
        [JsonPropertyName("dateTime")]
        public string? DateTime { get; set; }

        /// <summary>
        /// The timezone
        /// </summary>
        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; set; }

        /// <summary>
        /// The date (for all-day events)
        /// </summary>
        [JsonPropertyName("date")]
        public string? Date { get; set; }
    }

    /// <summary>
    /// Represents a person in a calendar event
    /// </summary>
    public class CalendarPerson
    {
        /// <summary>
        /// The person's ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The person's email
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// The person's display name
        /// </summary>
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Whether the person is self
        /// </summary>
        [JsonPropertyName("self")]
        public bool Self { get; set; }
    }
}
