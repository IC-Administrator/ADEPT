using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Data.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for student data operations
    /// </summary>
    public class StudentRepository : BaseRepository<Student>, IStudentRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StudentRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public StudentRepository(IDatabaseContext databaseContext, ILogger<StudentRepository> logger)
            : base(databaseContext, logger)
        {
        }

        /// <summary>
        /// Gets all students
        /// </summary>
        /// <returns>All students</returns>
        public async Task<IEnumerable<Student>> GetAllStudentsAsync()
        {
            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<Student>(
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
                      ORDER BY name"),
                "Error getting all students",
                Enumerable.Empty<Student>());
        }

        /// <summary>
        /// Gets a student by ID
        /// </summary>
        /// <param name="studentId">The student ID</param>
        /// <returns>The student or null if not found</returns>
        public async Task<Student?> GetStudentByIdAsync(string studentId)
        {
            ValidateId(studentId, "student");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<Student>(
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
                    new { StudentId = studentId }),
                $"Error getting student by ID {studentId}");
        }

        /// <summary>
        /// Gets students by class ID
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Students in the class</returns>
        public async Task<IEnumerable<Student>> GetStudentsByClassIdAsync(string classId)
        {
            ValidateId(classId, "class");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<Student>(
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
                    new { ClassId = classId }),
                $"Error getting students for class {classId}",
                Enumerable.Empty<Student>());
        }

        /// <summary>
        /// Adds a new student
        /// </summary>
        /// <param name="student">The student to add</param>
        /// <returns>The ID of the added student</returns>
        public async Task<string> AddStudentAsync(Student student)
        {
            return await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    // Validate student data using the EntityValidator
                    var validationResult = EntityValidator.ValidateStudent(student);
                    validationResult.ThrowIfInvalid();

                    if (string.IsNullOrEmpty(student.StudentId))
                    {
                        student.StudentId = Guid.NewGuid().ToString();
                    }

                    student.CreatedAt = DateTime.UtcNow;
                    student.UpdatedAt = DateTime.UtcNow;

                    await DatabaseContext.ExecuteNonQueryAsync(
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

                    Logger.LogInformation("Added new student: {Name} (ID: {StudentId})", student.Name, student.StudentId);
                    return student.StudentId;
                },
                $"Error adding student {student?.Name ?? "<unnamed>"}");
        }

        /// <summary>
        /// Updates an existing student
        /// </summary>
        /// <param name="student">The student to update</param>
        public async Task UpdateStudentAsync(Student student)
        {
            await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Validate student data using the EntityValidator
                    var validationResult = EntityValidator.ValidateStudent(student);
                    validationResult.ThrowIfInvalid();
                    ValidateId(student.StudentId, "student");

                    student.UpdatedAt = DateTime.UtcNow;

                    await DatabaseContext.ExecuteNonQueryAsync(
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

                    Logger.LogInformation("Updated student: {Name} (ID: {StudentId})", student.Name, student.StudentId);
                },
                $"Error updating student {student?.StudentId ?? "<unknown>"}");
        }

        /// <summary>
        /// Deletes a student
        /// </summary>
        /// <param name="studentId">The ID of the student to delete</param>
        public async Task DeleteStudentAsync(string studentId)
        {
            await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    ValidateId(studentId, "student");

                    await DatabaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM Students WHERE student_id = @StudentId",
                        new { StudentId = studentId });

                    Logger.LogInformation("Deleted student: {StudentId}", studentId);
                },
                $"Error deleting student {studentId}");
        }

        /// <summary>
        /// Adds multiple students
        /// </summary>
        /// <param name="students">The students to add</param>
        /// <returns>The IDs of the added students</returns>
        public async Task<IEnumerable<string>> AddStudentsAsync(IEnumerable<Student> students)
        {
            if (students == null)
            {
                throw new ArgumentNullException(nameof(students), "Students collection cannot be null");
            }

            // Validate all students before adding any
            foreach (var student in students)
            {
                var validationResult = EntityValidator.ValidateStudent(student);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException($"Validation failed for student {student.Name}: {string.Join(", ", validationResult.Errors)}");
                }
            }

            return await ExecuteWithErrorHandlingAsync<IEnumerable<string>>(
                async () =>
                {
                    var studentIds = new List<string>();

                    foreach (var student in students)
                    {
                        var studentId = await AddStudentAsync(student);
                        studentIds.Add(studentId);
                    }

                    Logger.LogInformation("Added {Count} students", studentIds.Count);
                    return studentIds;
                },
                "Error adding multiple students");
        }
    }
}
