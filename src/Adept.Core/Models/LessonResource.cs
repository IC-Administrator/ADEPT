using System;

namespace Adept.Core.Models
{
    /// <summary>
    /// Represents a resource attached to a lesson
    /// </summary>
    public class LessonResource
    {
        /// <summary>
        /// Gets or sets the resource ID
        /// </summary>
        public Guid ResourceId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the lesson ID
        /// </summary>
        public Guid LessonId { get; set; }
        
        /// <summary>
        /// Gets or sets the resource name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the resource type
        /// </summary>
        public ResourceType Type { get; set; }
        
        /// <summary>
        /// Gets or sets the resource URL or file path
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Gets or sets the date the resource was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the date the resource was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Represents the type of a lesson resource
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// A file resource
        /// </summary>
        File,
        
        /// <summary>
        /// A link resource
        /// </summary>
        Link,
        
        /// <summary>
        /// An image resource
        /// </summary>
        Image,
        
        /// <summary>
        /// A document resource
        /// </summary>
        Document,
        
        /// <summary>
        /// A presentation resource
        /// </summary>
        Presentation,
        
        /// <summary>
        /// A spreadsheet resource
        /// </summary>
        Spreadsheet,
        
        /// <summary>
        /// A video resource
        /// </summary>
        Video,
        
        /// <summary>
        /// An audio resource
        /// </summary>
        Audio,
        
        /// <summary>
        /// Other resource type
        /// </summary>
        Other
    }
}
