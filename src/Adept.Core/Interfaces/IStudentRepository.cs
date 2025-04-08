using Adept.Core.Models;

namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Repository for student data operations
    /// </summary>
    public interface IStudentRepository
    {
        /// <summary>
        /// Gets all students
        /// </summary>
        /// <returns>All students</returns>
        Task<IEnumerable<Student>> GetAllStudentsAsync();

        /// <summary>
        /// Gets a student by ID
        /// </summary>
        /// <param name="studentId">The student ID</param>
        /// <returns>The student or null if not found</returns>
        Task<Student?> GetStudentByIdAsync(string studentId);

        /// <summary>
        /// Gets students by class ID
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Students in the class</returns>
        Task<IEnumerable<Student>> GetStudentsByClassIdAsync(string classId);

        /// <summary>
        /// Adds a new student
        /// </summary>
        /// <param name="student">The student to add</param>
        /// <returns>The ID of the added student</returns>
        Task<string> AddStudentAsync(Student student);

        /// <summary>
        /// Updates an existing student
        /// </summary>
        /// <param name="student">The student to update</param>
        Task UpdateStudentAsync(Student student);

        /// <summary>
        /// Deletes a student
        /// </summary>
        /// <param name="studentId">The ID of the student to delete</param>
        Task DeleteStudentAsync(string studentId);

        /// <summary>
        /// Adds multiple students
        /// </summary>
        /// <param name="students">The students to add</param>
        /// <returns>The IDs of the added students</returns>
        Task<IEnumerable<string>> AddStudentsAsync(IEnumerable<Student> students);
    }
}
