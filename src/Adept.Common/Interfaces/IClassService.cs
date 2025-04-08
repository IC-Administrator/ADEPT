using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Service for managing classes
    /// </summary>
    public interface IClassService
    {
        /// <summary>
        /// Gets a class by ID
        /// </summary>
        /// <param name="id">The class ID</param>
        /// <returns>The class</returns>
        Task<ClassInfo> GetClassAsync(int id);

        /// <summary>
        /// Gets all classes
        /// </summary>
        /// <returns>List of classes</returns>
        Task<IEnumerable<ClassInfo>> GetClassesAsync();

        /// <summary>
        /// Gets a class by ID
        /// </summary>
        /// <param name="id">The class ID</param>
        /// <returns>The class</returns>
        Task<ClassInfo> GetClassByIdAsync(int id);

        /// <summary>
        /// Gets all classes
        /// </summary>
        /// <returns>List of classes</returns>
        Task<IEnumerable<ClassInfo>> GetAllClassesAsync();
    }

    /// <summary>
    /// Represents a class
    /// </summary>
    public class ClassInfo
    {
        /// <summary>
        /// Gets or sets the ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the start date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets the start time
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the duration in minutes
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Gets or sets the subject
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the location
        /// </summary>
        public string Location { get; set; } = string.Empty;
    }
}
