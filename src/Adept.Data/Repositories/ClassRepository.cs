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
        /// Adds a new class
        /// </summary>
        /// <param name="classEntity">The class to add</param>
        /// <returns>The ID of the added class</returns>
        public async Task<string> AddClassAsync(Class classEntity)
        {
            try
            {
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
        public async Task UpdateClassAsync(Class classEntity)
        {
            try
            {
                classEntity.UpdatedAt = DateTime.UtcNow;

                await _databaseContext.ExecuteNonQueryAsync(
                    @"UPDATE Classes SET 
                        class_code = @ClassCode, 
                        education_level = @EducationLevel, 
                        current_topic = @CurrentTopic, 
                        updated_at = @UpdatedAt 
                      WHERE class_id = @ClassId",
                    classEntity);
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
        public async Task DeleteClassAsync(string classId)
        {
            try
            {
                await _databaseContext.ExecuteNonQueryAsync(
                    "DELETE FROM Classes WHERE class_id = @ClassId",
                    new { ClassId = classId });
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
        public async Task<IEnumerable<Student>> GetStudentsForClassAsync(string classId)
        {
            try
            {
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
