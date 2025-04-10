using System;

namespace Adept.Data.Database
{
    /// <summary>
    /// Information about a database migration
    /// </summary>
    public class DatabaseMigrationInfo
    {
        /// <summary>
        /// Gets or sets the migration version
        /// </summary>
        public long Version { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the migration was applied
        /// </summary>
        public DateTime AppliedAt { get; set; }

        /// <summary>
        /// Gets or sets the description of the migration
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
