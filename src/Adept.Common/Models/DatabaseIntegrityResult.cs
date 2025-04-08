using System;
using System.Collections.Generic;

namespace Adept.Common.Models
{
    /// <summary>
    /// Result of a database integrity check
    /// </summary>
    public class DatabaseIntegrityResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the database passed the integrity check
        /// </summary>
        public bool IsIntegrityOk { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the database passed the foreign key check
        /// </summary>
        public bool IsForeignKeysOk { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the database is valid overall
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the time when the check was performed
        /// </summary>
        public DateTime CheckedAt { get; set; }

        /// <summary>
        /// Gets or sets the list of issues found
        /// </summary>
        public List<string> Issues { get; set; } = new List<string>();
    }
}
