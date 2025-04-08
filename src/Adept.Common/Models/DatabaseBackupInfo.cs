using System;

namespace Adept.Common.Models
{
    /// <summary>
    /// Information about a database backup file
    /// </summary>
    public class DatabaseBackupInfo
    {
        /// <summary>
        /// Gets or sets the full file path
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file name
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation time
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes
        /// </summary>
        public long SizeInBytes { get; set; }

        /// <summary>
        /// Gets or sets the file size in a human-readable format
        /// </summary>
        public string FormattedSize { get; set; } = string.Empty;
    }
}
