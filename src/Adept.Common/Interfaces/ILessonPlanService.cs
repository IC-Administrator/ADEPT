using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Service for managing lesson plans
    /// </summary>
    public interface ILessonPlanService
    {
        /// <summary>
        /// Gets a lesson plan by ID
        /// </summary>
        /// <param name="id">The lesson plan ID</param>
        /// <returns>The lesson plan</returns>
        Task<LessonPlanInfo> GetLessonPlanAsync(int id);

        /// <summary>
        /// Gets all lesson plans
        /// </summary>
        /// <returns>List of lesson plans</returns>
        Task<IEnumerable<LessonPlanInfo>> GetLessonPlansAsync();

        /// <summary>
        /// Gets a lesson plan by ID
        /// </summary>
        /// <param name="id">The lesson plan ID</param>
        /// <returns>The lesson plan</returns>
        Task<LessonPlanInfo> GetLessonPlanByIdAsync(int id);

        /// <summary>
        /// Gets all lesson plans
        /// </summary>
        /// <returns>List of lesson plans</returns>
        Task<IEnumerable<LessonPlanInfo>> GetAllLessonPlansAsync();

        /// <summary>
        /// Updates a lesson plan
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to update</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateLessonPlanAsync(LessonPlanInfo lessonPlan);
    }

    /// <summary>
    /// Represents a lesson plan
    /// </summary>
    public class LessonPlanInfo
    {
        /// <summary>
        /// Gets or sets the ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        public string Title { get; set; } = string.Empty;

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
        /// Gets or sets the class ID
        /// </summary>
        public int ClassId { get; set; }

        /// <summary>
        /// Gets or sets the calendar event ID
        /// </summary>
        public string? CalendarEventId { get; set; }

        /// <summary>
        /// Gets or sets the date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the learning objectives
        /// </summary>
        public string LearningObjectives { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the lesson components
        /// </summary>
        public string LessonComponents { get; set; } = string.Empty;
    }
}
