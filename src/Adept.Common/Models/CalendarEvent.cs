using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Adept.Common.Models
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

        /// <summary>
        /// The color ID of the event
        /// </summary>
        [JsonPropertyName("colorId")]
        public string? ColorId { get; set; }

        /// <summary>
        /// The reminders for the event
        /// </summary>
        [JsonPropertyName("reminders")]
        public CalendarReminders? Reminders { get; set; }

        /// <summary>
        /// The attendees of the event
        /// </summary>
        [JsonPropertyName("attendees")]
        public List<CalendarAttendee>? Attendees { get; set; }

        /// <summary>
        /// The attachments for the event
        /// </summary>
        [JsonPropertyName("attachments")]
        public List<CalendarAttachment>? Attachments { get; set; }

        /// <summary>
        /// The transparency of the event (whether it blocks time in the calendar)
        /// </summary>
        [JsonPropertyName("transparency")]
        public string? Transparency { get; set; }

        /// <summary>
        /// The visibility of the event (default, public, private, confidential)
        /// </summary>
        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; }
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

    /// <summary>
    /// Represents reminders for a calendar event
    /// </summary>
    public class CalendarReminders
    {
        /// <summary>
        /// Whether to use the default reminders
        /// </summary>
        [JsonPropertyName("useDefault")]
        public bool UseDefault { get; set; }

        /// <summary>
        /// The overrides for the reminders
        /// </summary>
        [JsonPropertyName("overrides")]
        public List<CalendarReminder>? Overrides { get; set; }
    }

    /// <summary>
    /// Represents a reminder for a calendar event
    /// </summary>
    public class CalendarReminder
    {
        /// <summary>
        /// The method of the reminder (email, popup)
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = "popup";

        /// <summary>
        /// The minutes before the event to trigger the reminder
        /// </summary>
        [JsonPropertyName("minutes")]
        public int Minutes { get; set; } = 10;
    }

    /// <summary>
    /// Represents an attendee for a calendar event
    /// </summary>
    public class CalendarAttendee
    {
        /// <summary>
        /// The attendee's email
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The attendee's display name
        /// </summary>
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Whether the attendee is optional
        /// </summary>
        [JsonPropertyName("optional")]
        public bool Optional { get; set; }

        /// <summary>
        /// The attendee's response status (needsAction, declined, tentative, accepted)
        /// </summary>
        [JsonPropertyName("responseStatus")]
        public string ResponseStatus { get; set; } = "needsAction";
    }

    /// <summary>
    /// Represents an attachment for a calendar event
    /// </summary>
    public class CalendarAttachment
    {
        /// <summary>
        /// The file ID of the attachment
        /// </summary>
        [JsonPropertyName("fileId")]
        public string? FileId { get; set; }

        /// <summary>
        /// The file URL of the attachment
        /// </summary>
        [JsonPropertyName("fileUrl")]
        public string? FileUrl { get; set; }

        /// <summary>
        /// The MIME type of the attachment
        /// </summary>
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        /// <summary>
        /// The title of the attachment
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// The icon link of the attachment
        /// </summary>
        [JsonPropertyName("iconLink")]
        public string? IconLink { get; set; }
    }
}
