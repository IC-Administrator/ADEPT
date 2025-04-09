using System;

namespace Adept.Common.Models
{
    /// <summary>
    /// Represents information about a calendar
    /// </summary>
    public class CalendarInfo
    {
        /// <summary>
        /// Gets or sets the calendar ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the calendar name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the calendar description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the calendar time zone
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the calendar color
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the primary calendar
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets the calendar access role
        /// </summary>
        public string AccessRole { get; set; }

        /// <summary>
        /// Gets or sets the calendar URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the calendar creation date
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the calendar last modified date
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }
    }
}
