using Adept.Core.Models;

namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Repository for lesson plan data operations
    /// </summary>
    public interface ILessonRepository
    {
        /// <summary>
        /// Gets all lesson plans
        /// </summary>
        /// <returns>All lesson plans</returns>
        Task<IEnumerable<LessonPlan>> GetAllLessonPlansAsync();

        /// <summary>
        /// Gets a lesson plan by ID
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        /// <returns>The lesson plan or null if not found</returns>
        Task<LessonPlan?> GetLessonByIdAsync(string lessonId);

        /// <summary>
        /// Gets lesson plans for a class
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Lesson plans for the class</returns>
        Task<IEnumerable<LessonPlan>> GetLessonsByClassIdAsync(string classId);

        /// <summary>
        /// Gets lesson plans for a date
        /// </summary>
        /// <param name="date">The date (YYYY-MM-DD)</param>
        /// <returns>Lesson plans for the date</returns>
        Task<IEnumerable<LessonPlan>> GetLessonsByDateAsync(string date);

        /// <summary>
        /// Gets a lesson plan by class, date, and time slot
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <param name="date">The date (YYYY-MM-DD)</param>
        /// <param name="timeSlot">The time slot (0-4)</param>
        /// <returns>The lesson plan or null if not found</returns>
        Task<LessonPlan?> GetLessonByClassDateSlotAsync(string classId, string date, int timeSlot);

        /// <summary>
        /// Adds a new lesson plan
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to add</param>
        /// <returns>The ID of the added lesson plan</returns>
        Task<string> AddLessonAsync(LessonPlan lessonPlan);

        /// <summary>
        /// Updates an existing lesson plan
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to update</param>
        Task UpdateLessonAsync(LessonPlan lessonPlan);

        /// <summary>
        /// Deletes a lesson plan
        /// </summary>
        /// <param name="lessonId">The ID of the lesson plan to delete</param>
        Task DeleteLessonAsync(string lessonId);

        /// <summary>
        /// Updates the calendar event ID for a lesson plan
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        /// <param name="calendarEventId">The calendar event ID</param>
        Task UpdateCalendarEventIdAsync(string lessonId, string calendarEventId);
    }
}
