using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for student data operations
    /// </summary>
    public class StudentRepository : IStudentRepository
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly ILogger<StudentRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StudentRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public StudentRepository(IDatabaseContext databaseContext, ILogger<StudentRepository> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets all students
        /// </summary>
        /// <returns>All students</returns>
        public async Task<IEnumerable<Student>> GetAllStudentsAsync()
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
                      ORDER BY name");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all students");
                return Enumerable.Empty<Student>();
            }
        }

        /// <summary>
        /// Gets a student by ID
        /// </summary>
        /// <param name="studentId">The student ID</param>
        /// <returns>The student or null if not found</returns>
        public async Task<Student?> GetStudentByIdAsync(string studentId)
        {
            try
            {
                return await _databaseContext.QuerySingleOrDefaultAsync<Student>(
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
                      WHERE student_id = @StudentId",
                    new { StudentId = studentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student by ID {StudentId}", studentId);
                return null;
            }
        }

        /// <summary>
        /// Gets students by class ID
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Students in the class</returns>
        public async Task<IEnumerable<Student>> GetStudentsByClassIdAsync(string classId)
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

        /// <summary>
        /// Adds a new student
        /// </summary>
        /// <param name="student">The student to add</param>
        /// <returns>The ID of the added student</returns>
        public async Task<string> AddStudentAsync(Student student)
        {
            try
            {
                if (string.IsNullOrEmpty(student.StudentId))
                {
                    student.StudentId = Guid.NewGuid().ToString();
                }

                student.CreatedAt = DateTime.UtcNow;
                student.UpdatedAt = DateTime.UtcNow;

                await _databaseContext.ExecuteNonQueryAsync(
                    @"INSERT INTO Students (
                        student_id, 
                        class_id, 
                        name, 
                        fsm_status, 
                        sen_status, 
                        eal_status, 
                        ability_level, 
                        reading_age, 
                        target_grade, 
                        notes, 
                        created_at, 
                        updated_at
                      ) VALUES (
                        @StudentId, 
                        @ClassId, 
                        @Name, 
                        @FsmStatus, 
                        @SenStatus, 
                        @EalStatus, 
                        @AbilityLevel, 
                        @ReadingAge, 
                        @TargetGrade, 
                        @Notes, 
                        @CreatedAt, 
                        @UpdatedAt
                      )",
                    student);

                return student.StudentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding student {Name}", student.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing student
        /// </summary>
        /// <param name="student">The student to update</param>
        public async Task UpdateStudentAsync(Student student)
        {
            try
            {
                student.UpdatedAt = DateTime.UtcNow;

                await _databaseContext.ExecuteNonQueryAsync(
                    @"UPDATE Students SET 
                        class_id = @ClassId, 
                        name = @Name, 
                        fsm_status = @FsmStatus, 
                        sen_status = @SenStatus, 
                        eal_status = @EalStatus, 
                        ability_level = @AbilityLevel, 
                        reading_age = @ReadingAge, 
                        target_grade = @TargetGrade, 
                        notes = @Notes, 
                        updated_at = @UpdatedAt 
                      WHERE student_id = @StudentId",
                    student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student {StudentId}", student.StudentId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a student
        /// </summary>
        /// <param name="studentId">The ID of the student to delete</param>
        public async Task DeleteStudentAsync(string studentId)
        {
            try
            {
                await _databaseContext.ExecuteNonQueryAsync(
                    "DELETE FROM Students WHERE student_id = @StudentId",
                    new { StudentId = studentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student {StudentId}", studentId);
                throw;
            }
        }

        /// <summary>
        /// Adds multiple students
        /// </summary>
        /// <param name="students">The students to add</param>
        /// <returns>The IDs of the added students</returns>
        public async Task<IEnumerable<string>> AddStudentsAsync(IEnumerable<Student> students)
        {
            var studentIds = new List<string>();
            using var transaction = await _databaseContext.BeginTransactionAsync();

            try
            {
                foreach (var student in students)
                {
                    var studentId = await AddStudentAsync(student);
                    studentIds.Add(studentId);
                }

                await transaction.CommitAsync();
                return studentIds;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding multiple students");
                throw;
            }
        }
    }
}
