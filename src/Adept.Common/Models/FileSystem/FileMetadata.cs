using System;
using System.Collections.Generic;

namespace Adept.Common.Models.FileSystem
{
    /// <summary>
    /// Metadata for a file
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Gets or sets the file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file path relative to the scratchpad
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets the full file path
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the last modified date
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets the file extension
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the file type (based on extension)
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Gets or sets the file tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the file category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets additional metadata properties
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Metadata for a markdown file
    /// </summary>
    public class MarkdownMetadata : FileMetadata
    {
        /// <summary>
        /// Gets or sets the title extracted from the markdown
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description extracted from the markdown
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the author extracted from the markdown
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the creation date extracted from the markdown
        /// </summary>
        public DateTime? CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the keywords extracted from the markdown
        /// </summary>
        public List<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the headings extracted from the markdown
        /// </summary>
        public List<string> Headings { get; set; } = new List<string>();
    }
}
