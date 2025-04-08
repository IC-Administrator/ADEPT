namespace Adept.Core.Models
{
    /// <summary>
    /// Represents a student in a class
    /// </summary>
    public class Student
    {
        /// <summary>
        /// Unique identifier for the student
        /// </summary>
        public string StudentId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Class ID this student belongs to
        /// </summary>
        public string ClassId { get; set; } = string.Empty;

        /// <summary>
        /// Student's name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Free School Meals status (0 = No, 1 = Yes)
        /// </summary>
        public int? FsmStatus { get; set; }

        /// <summary>
        /// Special Educational Needs status (0 = No, 1 = Yes)
        /// </summary>
        public int? SenStatus { get; set; }

        /// <summary>
        /// English as an Additional Language status (0 = No, 1 = Yes)
        /// </summary>
        public int? EalStatus { get; set; }

        /// <summary>
        /// Ability level (e.g., "H", "M", "L")
        /// </summary>
        public string? AbilityLevel { get; set; }

        /// <summary>
        /// Reading age
        /// </summary>
        public string? ReadingAge { get; set; }

        /// <summary>
        /// Target grade
        /// </summary>
        public string? TargetGrade { get; set; }

        /// <summary>
        /// Additional notes about the student
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// When the student record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the student record was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
