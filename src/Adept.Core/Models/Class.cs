namespace Adept.Core.Models
{
    /// <summary>
    /// Represents a class of students
    /// </summary>
    public class Class
    {
        /// <summary>
        /// Unique identifier for the class
        /// </summary>
        public string ClassId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Class code (e.g., "10A", "11B")
        /// </summary>
        public string ClassCode { get; set; } = string.Empty;

        /// <summary>
        /// Education level (e.g., "KS3", "KS4", "GCSE")
        /// </summary>
        public string EducationLevel { get; set; } = string.Empty;

        /// <summary>
        /// Current topic being taught
        /// </summary>
        public string? CurrentTopic { get; set; }

        /// <summary>
        /// When the class was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the class was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
