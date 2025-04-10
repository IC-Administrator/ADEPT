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
    /// Repository for class data operations
    /// </summary>
    public class ClassRepository : BaseRepository<Class>, IClassRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public ClassRepository(IDatabaseContext databaseContext, ILogger<ClassRepository> logger)
            : base(databaseContext, logger)
        {
        }

        /// <summary>
        /// Gets all classes
        /// </summary>
        /// <returns>All classes</returns>
        public async Task<IEnumerable<Class>> GetAllClassesAsync()
        {
            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<Class>(
                    @"SELECT
                        class_id AS ClassId,
                        class_code AS ClassCode,
                        education_level AS EducationLevel,
                        current_topic AS CurrentTopic,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Classes
                      ORDER BY class_code"),
                "Error getting all classes",
                Enumerable.Empty<Class>());
        }

        /// <summary>
        /// Gets a class by ID
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>The class or null if not found</returns>
        public async Task<Class?> GetClassByIdAsync(string classId)
        {
            ValidateId(classId, "class");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<Class>(
                    @"SELECT
                        class_id AS ClassId,
                        class_code AS ClassCode,
                        education_level AS EducationLevel,
                        current_topic AS CurrentTopic,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Classes
                      WHERE class_id = @ClassId",
                    new { ClassId = classId }),
                $"Error getting class by ID {classId}");
        }

        /// <summary>
        /// Gets a class by code
        /// </summary>
        /// <param name="classCode">The class code</param>
        /// <returns>The class or null if not found</returns>
        public async Task<Class?> GetClassByCodeAsync(string classCode)
        {
            ValidateStringNotNullOrEmpty(classCode, "classCode");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<Class>(
                    @"SELECT
                        class_id AS ClassId,
                        class_code AS ClassCode,
                        education_level AS EducationLevel,
                        current_topic AS CurrentTopic,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Classes
                      WHERE class_code = @ClassCode",
                    new { ClassCode = classCode }),
                $"Error getting class by code {classCode}");
        }

        /// <summary>
        /// Validates a class entity
        /// </summary>
        /// <param name="classEntity">The class to validate</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        private void ValidateClass(Class classEntity)
        {
            ValidateEntityNotNull(classEntity, "class");
            ValidateStringNotNullOrEmpty(classEntity.ClassCode, "ClassCode");
            ValidateStringNotNullOrEmpty(classEntity.EducationLevel, "EducationLevel");

            // Additional validation rules can be added here
        }

        /// <summary>
        /// Adds a new class
        /// </summary>
        /// <param name="classEntity">The class to add</param>
        /// <returns>The ID of the added class</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database error occurs</exception>
        public async Task<string> AddClassAsync(Class classEntity)
        {
            return await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    // Validate the class entity
                    ValidateClass(classEntity);

                    // Check if a class with the same code already exists
                    var existingClass = await GetClassByCodeAsync(classEntity.ClassCode);
                    if (existingClass != null)
                    {
                        throw new InvalidOperationException($"A class with code '{classEntity.ClassCode}' already exists");
                    }

                    if (string.IsNullOrEmpty(classEntity.ClassId))
                    {
                        classEntity.ClassId = Guid.NewGuid().ToString();
                    }

                    classEntity.CreatedAt = DateTime.UtcNow;
                    classEntity.UpdatedAt = DateTime.UtcNow;

                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"INSERT INTO Classes (
                            class_id,
                            class_code,
                            education_level,
                            current_topic,
                            created_at,
                            updated_at
                          ) VALUES (
                            @ClassId,
                            @ClassCode,
                            @EducationLevel,
                            @CurrentTopic,
                            @CreatedAt,
                            @UpdatedAt
                          )",
                        classEntity);

                    Logger.LogInformation("Added new class: {ClassCode}", classEntity.ClassCode);
                    return classEntity.ClassId;
                },
                $"Error adding class {classEntity?.ClassCode ?? "<unknown>"}");
        }

        /// <summary>
        /// Updates an existing class
        /// </summary>
        /// <param name="classEntity">The class to update</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database error occurs</exception>
        public async Task UpdateClassAsync(Class classEntity)
        {
            await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Validate the class entity
                    ValidateClass(classEntity);
                    ValidateId(classEntity.ClassId, "class");

                    // Check if the class exists
                    var existingClass = await GetClassByIdAsync(classEntity.ClassId);
                    if (existingClass == null)
                    {
                        throw new InvalidOperationException($"Class with ID '{classEntity.ClassId}' not found");
                    }

                    // Check if the class code is being changed and if it conflicts with another class
                    if (existingClass.ClassCode != classEntity.ClassCode)
                    {
                        var conflictingClass = await GetClassByCodeAsync(classEntity.ClassCode);
                        if (conflictingClass != null && conflictingClass.ClassId != classEntity.ClassId)
                        {
                            throw new InvalidOperationException($"A class with code '{classEntity.ClassCode}' already exists");
                        }
                    }

                    classEntity.UpdatedAt = DateTime.UtcNow;
                    classEntity.CreatedAt = existingClass.CreatedAt; // Preserve the original creation date

                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"UPDATE Classes SET
                            class_code = @ClassCode,
                            education_level = @EducationLevel,
                            current_topic = @CurrentTopic,
                            updated_at = @UpdatedAt
                          WHERE class_id = @ClassId",
                        classEntity);

                    Logger.LogInformation("Updated class: {ClassId}, {ClassCode}", classEntity.ClassId, classEntity.ClassCode);
                },
                $"Error updating class {classEntity?.ClassId ?? "<unknown>"}");
        }

        /// <summary>
        /// Deletes a class
        /// </summary>
        /// <param name="classId">The ID of the class to delete</param>
        /// <exception cref="ArgumentException">Thrown when the class ID is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database error occurs</exception>
        public async Task DeleteClassAsync(string classId)
        {
            await ExecuteInTransactionAsync(
                async (transaction) =>
                {
                    ValidateId(classId, "class");

                    // Check if the class exists
                    var existingClass = await GetClassByIdAsync(classId);
                    if (existingClass == null)
                    {
                        throw new InvalidOperationException($"Class with ID '{classId}' not found");
                    }

                    // Delete the class (cascade will handle related records)
                    await DatabaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM Classes WHERE class_id = @ClassId",
                        new { ClassId = classId });

                    Logger.LogInformation("Deleted class: {ClassId}, {ClassCode}", classId, existingClass.ClassCode);
                },
                $"Error deleting class {classId}");
        }

        /// <summary>
        /// Gets all students for a class
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Students in the class</returns>
        /// <exception cref="ArgumentException">Thrown when the class ID is invalid</exception>
        public async Task<IEnumerable<Student>> GetStudentsForClassAsync(string classId)
        {
            ValidateId(classId, "class");

            return await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Check if the class exists
                    var existingClass = await GetClassByIdAsync(classId);
                    if (existingClass == null)
                    {
                        Logger.LogWarning("Attempted to get students for non-existent class {ClassId}", classId);
                        return Enumerable.Empty<Student>();
                    }

                    return await DatabaseContext.QueryAsync<Student>(
                        @"SELECT
                            student_id AS StudentId,
                            class_id AS ClassId,
                            name AS Name,
                            fsm_status AS FsmStatus,
                            sen_status AS SenStatus,
                            eal_status AS EalStatus,
                            ability_level AS AbilityLevel,
                            reading_age AS ReadingAge,
                            target_grade AS TargetGrade,
                            notes AS Notes,
                            created_at AS CreatedAt,
                            updated_at AS UpdatedAt
                          FROM Students
                          WHERE class_id = @ClassId
                          ORDER BY name",
                        new { ClassId = classId });
                },
                $"Error getting students for class {classId}",
                Enumerable.Empty<Student>());
        }
    }
}
