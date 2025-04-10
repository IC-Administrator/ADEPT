using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for lesson plan data operations
    /// </summary>
    public class LessonRepository : BaseRepository<LessonPlan>, ILessonRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LessonRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public LessonRepository(IDatabaseContext databaseContext, ILogger<LessonRepository> logger)
            : base(databaseContext, logger)
        {
        }

        /// <summary>
        /// Validates a lesson plan entity
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to validate</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        private void ValidateLesson(LessonPlan lessonPlan)
        {
            ValidateEntityNotNull(lessonPlan, "lesson plan");
            ValidateStringNotNullOrEmpty(lessonPlan.Title, "Title");
            ValidateStringNotNullOrEmpty(lessonPlan.Date, "Date");
            ValidateStringNotNullOrEmpty(lessonPlan.ClassId, "ClassId");

            if (lessonPlan.TimeSlot < 0 || lessonPlan.TimeSlot > 4)
            {
                throw new ArgumentException("Time slot must be between 0 and 4", nameof(lessonPlan));
            }

            // Validate date format (YYYY-MM-DD)
            if (!System.Text.RegularExpressions.Regex.IsMatch(lessonPlan.Date, @"^\d{4}-\d{2}-\d{2}$"))
            {
                throw new ArgumentException("Date must be in YYYY-MM-DD format", nameof(lessonPlan));
            }

            // Additional validation rules can be added here
        }

        /// <summary>
        /// Gets all lesson plans
        /// </summary>
        /// <returns>All lesson plans</returns>
        public async Task<IEnumerable<LessonPlan>> GetAllLessonPlansAsync()
        {
            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<LessonPlan>(
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
                      ORDER BY date DESC, time_slot ASC"),
                "Error getting all lesson plans",
                Enumerable.Empty<LessonPlan>());
        }

        /// <summary>
        /// Gets a lesson plan by ID
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        /// <returns>The lesson plan or null if not found</returns>
        public async Task<LessonPlan?> GetLessonByIdAsync(string lessonId)
        {
            ValidateId(lessonId, "lesson");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<LessonPlan>(
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
                    new { LessonId = lessonId }),
                $"Error getting lesson plan by ID {lessonId}");
        }

        /// <summary>
        /// Gets lesson plans for a class
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Lesson plans for the class</returns>
        public async Task<IEnumerable<LessonPlan>> GetLessonsByClassIdAsync(string classId)
        {
            ValidateId(classId, "class");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<LessonPlan>(
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
                    new { ClassId = classId }),
                $"Error getting lesson plans for class {classId}",
                Enumerable.Empty<LessonPlan>());
        }

        /// <summary>
        /// Gets lesson plans for a date
        /// </summary>
        /// <param name="date">The date (YYYY-MM-DD)</param>
        /// <returns>Lesson plans for the date</returns>
        public async Task<IEnumerable<LessonPlan>> GetLessonsByDateAsync(string date)
        {
            ValidateStringNotNullOrEmpty(date, "date");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<LessonPlan>(
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
                    new { Date = date }),
                $"Error getting lesson plans for date {date}",
                Enumerable.Empty<LessonPlan>());
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
            ValidateId(classId, "class");
            ValidateStringNotNullOrEmpty(date, "date");

            if (timeSlot < 0 || timeSlot > 4)
            {
                throw new ArgumentException("Time slot must be between 0 and 4", nameof(timeSlot));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<LessonPlan>(
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
                    new { ClassId = classId, Date = date, TimeSlot = timeSlot }),
                $"Error getting lesson plan for class {classId}, date {date}, slot {timeSlot}");
        }

        /// <summary>
        /// Adds a new lesson plan
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to add</param>
        /// <returns>The ID of the added lesson plan</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database error occurs</exception>
        public async Task<string> AddLessonAsync(LessonPlan lessonPlan)
        {
            return await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    // Validate the lesson plan
                    ValidateLesson(lessonPlan);

                    // Check if a lesson plan already exists for this class, date, and time slot
                    var existingLesson = await GetLessonByClassDateSlotAsync(lessonPlan.ClassId, lessonPlan.Date, lessonPlan.TimeSlot);
                    if (existingLesson != null)
                    {
                        throw new InvalidOperationException($"A lesson plan already exists for class {lessonPlan.ClassId} on {lessonPlan.Date} at time slot {lessonPlan.TimeSlot}");
                    }

                    // Check if the class exists
                    var classExists = await DatabaseContext.QuerySingleOrDefaultAsync<int>(
                        "SELECT 1 FROM Classes WHERE class_id = @ClassId",
                        new { ClassId = lessonPlan.ClassId });

                    if (classExists == 0)
                    {
                        throw new InvalidOperationException($"Class with ID '{lessonPlan.ClassId}' not found");
                    }

                    if (string.IsNullOrEmpty(lessonPlan.LessonId))
                    {
                        lessonPlan.LessonId = Guid.NewGuid().ToString();
                    }

                    // Ensure components_json is not null
                    if (string.IsNullOrEmpty(lessonPlan.ComponentsJson))
                    {
                        lessonPlan.ComponentsJson = "{}";
                    }

                    lessonPlan.CreatedAt = DateTime.UtcNow;
                    lessonPlan.UpdatedAt = DateTime.UtcNow;

                    await DatabaseContext.ExecuteNonQueryAsync(
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

                    Logger.LogInformation("Added new lesson plan: {LessonId}, {Title}, {Date}, Slot {TimeSlot}",
                        lessonPlan.LessonId, lessonPlan.Title, lessonPlan.Date, lessonPlan.TimeSlot);
                    return lessonPlan.LessonId;
                },
                $"Error adding lesson plan {lessonPlan?.Title ?? "<unnamed>"}");
        }

        /// <summary>
        /// Updates an existing lesson plan
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to update</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database error occurs</exception>
        public async Task UpdateLessonAsync(LessonPlan lessonPlan)
        {
            await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Validate the lesson plan
                    ValidateLesson(lessonPlan);
                    ValidateId(lessonPlan.LessonId, "lesson");

                    // Check if the lesson plan exists
                    var existingLesson = await GetLessonByIdAsync(lessonPlan.LessonId);
                    if (existingLesson == null)
                    {
                        throw new InvalidOperationException($"Lesson plan with ID '{lessonPlan.LessonId}' not found");
                    }

                    // Check if the class exists
                    var classExists = await DatabaseContext.QuerySingleOrDefaultAsync<int>(
                        "SELECT 1 FROM Classes WHERE class_id = @ClassId",
                        new { ClassId = lessonPlan.ClassId });

                    if (classExists == 0)
                    {
                        throw new InvalidOperationException($"Class with ID '{lessonPlan.ClassId}' not found");
                    }

                    // If changing date/time slot, check for conflicts
                    if (existingLesson.Date != lessonPlan.Date || existingLesson.TimeSlot != lessonPlan.TimeSlot || existingLesson.ClassId != lessonPlan.ClassId)
                    {
                        var conflictingLesson = await GetLessonByClassDateSlotAsync(lessonPlan.ClassId, lessonPlan.Date, lessonPlan.TimeSlot);
                        if (conflictingLesson != null && conflictingLesson.LessonId != lessonPlan.LessonId)
                        {
                            throw new InvalidOperationException($"A lesson plan already exists for class {lessonPlan.ClassId} on {lessonPlan.Date} at time slot {lessonPlan.TimeSlot}");
                        }
                    }

                    // Ensure components_json is not null
                    if (string.IsNullOrEmpty(lessonPlan.ComponentsJson))
                    {
                        lessonPlan.ComponentsJson = "{}";
                    }

                    lessonPlan.UpdatedAt = DateTime.UtcNow;
                    lessonPlan.CreatedAt = existingLesson.CreatedAt; // Preserve the original creation date

                    await DatabaseContext.ExecuteNonQueryAsync(
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

                    Logger.LogInformation("Updated lesson plan: {LessonId}, {Title}, {Date}, Slot {TimeSlot}",
                        lessonPlan.LessonId, lessonPlan.Title, lessonPlan.Date, lessonPlan.TimeSlot);
                },
                $"Error updating lesson plan {lessonPlan?.LessonId ?? "<unknown>"}");
        }

        /// <summary>
        /// Deletes a lesson plan
        /// </summary>
        /// <param name="lessonId">The ID of the lesson plan to delete</param>
        /// <exception cref="ArgumentException">Thrown when the lesson ID is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database error occurs</exception>
        public async Task DeleteLessonAsync(string lessonId)
        {
            await ExecuteInTransactionAsync(
                async (transaction) =>
                {
                    ValidateId(lessonId, "lesson");

                    // Check if the lesson plan exists
                    var existingLesson = await GetLessonByIdAsync(lessonId);
                    if (existingLesson == null)
                    {
                        throw new InvalidOperationException($"Lesson plan with ID '{lessonId}' not found");
                    }

                    // Delete the lesson plan
                    await DatabaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM LessonPlans WHERE lesson_id = @LessonId",
                        new { LessonId = lessonId });

                    Logger.LogInformation("Deleted lesson plan: {LessonId}, {Title}, {Date}, Slot {TimeSlot}",
                        lessonId, existingLesson.Title, existingLesson.Date, existingLesson.TimeSlot);
                },
                $"Error deleting lesson plan {lessonId}");
        }

        /// <summary>
        /// Updates the calendar event ID for a lesson plan
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        /// <param name="calendarEventId">The calendar event ID</param>
        public async Task UpdateCalendarEventIdAsync(string lessonId, string calendarEventId)
        {
            await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    ValidateId(lessonId, "lesson");

                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"UPDATE LessonPlans SET
                            calendar_event_id = @CalendarEventId,
                            updated_at = CURRENT_TIMESTAMP
                          WHERE lesson_id = @LessonId",
                        new { LessonId = lessonId, CalendarEventId = calendarEventId });

                    Logger.LogInformation("Updated calendar event ID for lesson {LessonId}: {CalendarEventId}",
                        lessonId, calendarEventId ?? "<null>");
                },
                $"Error updating calendar event ID for lesson {lessonId}");
        }
    }
}
