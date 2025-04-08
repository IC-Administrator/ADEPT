using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for class data operations
    /// </summary>
    public class ClassRepository : IClassRepository
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly ILogger<ClassRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public ClassRepository(IDatabaseContext databaseContext, ILogger<ClassRepository> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets all classes
        /// </summary>
        /// <returns>All classes</returns>
        public async Task<IEnumerable<Class>> GetAllClassesAsync()
        {
            try
            {
                return await _databaseContext.QueryAsync<Class>(
                    @"SELECT
                        class_id AS ClassId,
                        class_code AS ClassCode,
                        education_level AS EducationLevel,
                        current_topic AS CurrentTopic,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Classes
                      ORDER BY class_code");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all classes");
                return Enumerable.Empty<Class>();
            }
        }

        /// <summary>
        /// Gets a class by ID
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>The class or null if not found</returns>
        public async Task<Class?> GetClassByIdAsync(string classId)
        {
            try
            {
                return await _databaseContext.QuerySingleOrDefaultAsync<Class>(
                    @"SELECT
                        class_id AS ClassId,
                        class_code AS ClassCode,
                        education_level AS EducationLevel,
                        current_topic AS CurrentTopic,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Classes
                      WHERE class_id = @ClassId",
                    new { ClassId = classId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class by ID {ClassId}", classId);
                return null;
            }
        }

        /// <summary>
        /// Gets a class by code
        /// </summary>
        /// <param name="classCode">The class code</param>
        /// <returns>The class or null if not found</returns>
        public async Task<Class?> GetClassByCodeAsync(string classCode)
        {
            try
            {
                return await _databaseContext.QuerySingleOrDefaultAsync<Class>(
                    @"SELECT
                        class_id AS ClassId,
                        class_code AS ClassCode,
                        education_level AS EducationLevel,
                        current_topic AS CurrentTopic,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Classes
                      WHERE class_code = @ClassCode",
                    new { ClassCode = classCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class by code {ClassCode}", classCode);
                return null;
            }
        }

        /// <summary>
        /// Validates a class entity
        /// </summary>
        /// <param name="classEntity">The class to validate</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        private void ValidateClass(Class classEntity)
        {
            if (classEntity == null)
            {
                throw new ArgumentNullException(nameof(classEntity), "Class entity cannot be null");
            }

            if (string.IsNullOrWhiteSpace(classEntity.ClassCode))
            {
                throw new ArgumentException("Class code cannot be empty", nameof(classEntity));
            }

            if (string.IsNullOrWhiteSpace(classEntity.EducationLevel))
            {
                throw new ArgumentException("Education level cannot be empty", nameof(classEntity));
            }

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
            try
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

                await _databaseContext.ExecuteNonQueryAsync(
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

                _logger.LogInformation("Added new class: {ClassCode}", classEntity.ClassCode);
                return classEntity.ClassId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding class {ClassCode}", classEntity.ClassCode);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing class
        /// </summary>
        /// <param name="classEntity">The class to update</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database error occurs</exception>
        public async Task UpdateClassAsync(Class classEntity)
        {
            try
            {
                // Validate the class entity
                ValidateClass(classEntity);

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

                await _databaseContext.ExecuteNonQueryAsync(
                    @"UPDATE Classes SET
                        class_code = @ClassCode,
                        education_level = @EducationLevel,
                        current_topic = @CurrentTopic,
                        updated_at = @UpdatedAt
                      WHERE class_id = @ClassId",
                    classEntity);

                _logger.LogInformation("Updated class: {ClassId}, {ClassCode}", classEntity.ClassId, classEntity.ClassCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating class {ClassId}", classEntity.ClassId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a class
        /// </summary>
        /// <param name="classId">The ID of the class to delete</param>
        /// <exception cref="ArgumentException">Thrown when the class ID is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database error occurs</exception>
        public async Task DeleteClassAsync(string classId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(classId))
                {
                    throw new ArgumentException("Class ID cannot be empty", nameof(classId));
                }

                // Check if the class exists
                var existingClass = await GetClassByIdAsync(classId);
                if (existingClass == null)
                {
                    throw new InvalidOperationException($"Class with ID '{classId}' not found");
                }

                // Begin a transaction to ensure data integrity
                using var transaction = await _databaseContext.BeginTransactionAsync();
                try
                {
                    // Delete the class (cascade will handle related records)
                    await _databaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM Classes WHERE class_id = @ClassId",
                        new { ClassId = classId });

                    await transaction.CommitAsync();
                    _logger.LogInformation("Deleted class: {ClassId}, {ClassCode}", classId, existingClass.ClassCode);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction rolled back while deleting class {ClassId}", classId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting class {ClassId}", classId);
                throw;
            }
        }

        /// <summary>
        /// Gets all students for a class
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Students in the class</returns>
        /// <exception cref="ArgumentException">Thrown when the class ID is invalid</exception>
        public async Task<IEnumerable<Student>> GetStudentsForClassAsync(string classId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(classId))
                {
                    throw new ArgumentException("Class ID cannot be empty", nameof(classId));
                }

                // Check if the class exists
                var existingClass = await GetClassByIdAsync(classId);
                if (existingClass == null)
                {
                    _logger.LogWarning("Attempted to get students for non-existent class {ClassId}", classId);
                    return Enumerable.Empty<Student>();
                }

                return await _databaseContext.QueryAsync<Student>(
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students for class {ClassId}", classId);
                return Enumerable.Empty<Student>();
            }
        }
    }
}
