using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for lesson plan data operations
    /// </summary>
    public class LessonRepository : ILessonRepository
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly ILogger<LessonRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LessonRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public LessonRepository(IDatabaseContext databaseContext, ILogger<LessonRepository> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets all lesson plans
        /// </summary>
        /// <returns>All lesson plans</returns>
        public async Task<IEnumerable<LessonPlan>> GetAllLessonPlansAsync()
        {
            try
            {
                return await _databaseContext.QueryAsync<LessonPlan>(
                    @"SELECT 
                        lesson_id AS LessonId, 
                        class_id AS ClassId, 
                        date AS Date, 
                        time_slot AS TimeSlot, 
                        title AS Title, 
                        learning_objectives AS LearningObjectives, 
                        calendar_event_id AS CalendarEventId, 
                        components_json AS ComponentsJson, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                      FROM LessonPlans 
                      ORDER BY date DESC, time_slot ASC");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all lesson plans");
                return Enumerable.Empty<LessonPlan>();
            }
        }

        /// <summary>
        /// Gets a lesson plan by ID
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        /// <returns>The lesson plan or null if not found</returns>
        public async Task<LessonPlan?> GetLessonByIdAsync(string lessonId)
        {
            try
            {
                return await _databaseContext.QuerySingleOrDefaultAsync<LessonPlan>(
                    @"SELECT 
                        lesson_id AS LessonId, 
                        class_id AS ClassId, 
                        date AS Date, 
                        time_slot AS TimeSlot, 
                        title AS Title, 
                        learning_objectives AS LearningObjectives, 
                        calendar_event_id AS CalendarEventId, 
                        components_json AS ComponentsJson, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                      FROM LessonPlans 
                      WHERE lesson_id = @LessonId",
                    new { LessonId = lessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson plan by ID {LessonId}", lessonId);
                return null;
            }
        }

        /// <summary>
        /// Gets lesson plans for a class
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Lesson plans for the class</returns>
        public async Task<IEnumerable<LessonPlan>> GetLessonsByClassIdAsync(string classId)
        {
            try
            {
                return await _databaseContext.QueryAsync<LessonPlan>(
                    @"SELECT 
                        lesson_id AS LessonId, 
                        class_id AS ClassId, 
                        date AS Date, 
                        time_slot AS TimeSlot, 
                        title AS Title, 
                        learning_objectives AS LearningObjectives, 
                        calendar_event_id AS CalendarEventId, 
                        components_json AS ComponentsJson, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                      FROM LessonPlans 
                      WHERE class_id = @ClassId 
                      ORDER BY date DESC, time_slot ASC",
                    new { ClassId = classId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson plans for class {ClassId}", classId);
                return Enumerable.Empty<LessonPlan>();
            }
        }

        /// <summary>
        /// Gets lesson plans for a date
        /// </summary>
        /// <param name="date">The date (YYYY-MM-DD)</param>
        /// <returns>Lesson plans for the date</returns>
        public async Task<IEnumerable<LessonPlan>> GetLessonsByDateAsync(string date)
        {
            try
            {
                return await _databaseContext.QueryAsync<LessonPlan>(
                    @"SELECT 
                        lesson_id AS LessonId, 
                        class_id AS ClassId, 
                        date AS Date, 
                        time_slot AS TimeSlot, 
                        title AS Title, 
                        learning_objectives AS LearningObjectives, 
                        calendar_event_id AS CalendarEventId, 
                        components_json AS ComponentsJson, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                      FROM LessonPlans 
                      WHERE date = @Date 
                      ORDER BY time_slot ASC",
                    new { Date = date });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson plans for date {Date}", date);
                return Enumerable.Empty<LessonPlan>();
            }
        }

        /// <summary>
        /// Gets a lesson plan by class, date, and time slot
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <param name="date">The date (YYYY-MM-DD)</param>
        /// <param name="timeSlot">The time slot (0-4)</param>
        /// <returns>The lesson plan or null if not found</returns>
        public async Task<LessonPlan?> GetLessonByClassDateSlotAsync(string classId, string date, int timeSlot)
        {
            try
            {
                return await _databaseContext.QuerySingleOrDefaultAsync<LessonPlan>(
                    @"SELECT 
                        lesson_id AS LessonId, 
                        class_id AS ClassId, 
                        date AS Date, 
                        time_slot AS TimeSlot, 
                        title AS Title, 
                        learning_objectives AS LearningObjectives, 
                        calendar_event_id AS CalendarEventId, 
                        components_json AS ComponentsJson, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                      FROM LessonPlans 
                      WHERE class_id = @ClassId AND date = @Date AND time_slot = @TimeSlot",
                    new { ClassId = classId, Date = date, TimeSlot = timeSlot });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson plan for class {ClassId}, date {Date}, slot {TimeSlot}", classId, date, timeSlot);
                return null;
            }
        }

        /// <summary>
        /// Adds a new lesson plan
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to add</param>
        /// <returns>The ID of the added lesson plan</returns>
        public async Task<string> AddLessonAsync(LessonPlan lessonPlan)
        {
            try
            {
                if (string.IsNullOrEmpty(lessonPlan.LessonId))
                {
                    lessonPlan.LessonId = Guid.NewGuid().ToString();
                }

                lessonPlan.CreatedAt = DateTime.UtcNow;
                lessonPlan.UpdatedAt = DateTime.UtcNow;

                await _databaseContext.ExecuteNonQueryAsync(
                    @"INSERT INTO LessonPlans (
                        lesson_id, 
                        class_id, 
                        date, 
                        time_slot, 
                        title, 
                        learning_objectives, 
                        calendar_event_id, 
                        components_json, 
                        created_at, 
                        updated_at
                      ) VALUES (
                        @LessonId, 
                        @ClassId, 
                        @Date, 
                        @TimeSlot, 
                        @Title, 
                        @LearningObjectives, 
                        @CalendarEventId, 
                        @ComponentsJson, 
                        @CreatedAt, 
                        @UpdatedAt
                      )",
                    lessonPlan);

                return lessonPlan.LessonId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding lesson plan {Title}", lessonPlan.Title);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing lesson plan
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to update</param>
        public async Task UpdateLessonAsync(LessonPlan lessonPlan)
        {
            try
            {
                lessonPlan.UpdatedAt = DateTime.UtcNow;

                await _databaseContext.ExecuteNonQueryAsync(
                    @"UPDATE LessonPlans SET 
                        class_id = @ClassId, 
                        date = @Date, 
                        time_slot = @TimeSlot, 
                        title = @Title, 
                        learning_objectives = @LearningObjectives, 
                        calendar_event_id = @CalendarEventId, 
                        components_json = @ComponentsJson, 
                        updated_at = @UpdatedAt 
                      WHERE lesson_id = @LessonId",
                    lessonPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson plan {LessonId}", lessonPlan.LessonId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a lesson plan
        /// </summary>
        /// <param name="lessonId">The ID of the lesson plan to delete</param>
        public async Task DeleteLessonAsync(string lessonId)
        {
            try
            {
                await _databaseContext.ExecuteNonQueryAsync(
                    "DELETE FROM LessonPlans WHERE lesson_id = @LessonId",
                    new { LessonId = lessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson plan {LessonId}", lessonId);
                throw;
            }
        }

        /// <summary>
        /// Updates the calendar event ID for a lesson plan
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        /// <param name="calendarEventId">The calendar event ID</param>
        public async Task UpdateCalendarEventIdAsync(string lessonId, string calendarEventId)
        {
            try
            {
                await _databaseContext.ExecuteNonQueryAsync(
                    @"UPDATE LessonPlans SET 
                        calendar_event_id = @CalendarEventId, 
                        updated_at = CURRENT_TIMESTAMP 
                      WHERE lesson_id = @LessonId",
                    new { LessonId = lessonId, CalendarEventId = calendarEventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calendar event ID for lesson {LessonId}", lessonId);
                throw;
            }
        }
    }
}
