using Adept.Core.Models;

namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Repository for class data operations
    /// </summary>
    public interface IClassRepository
    {
        /// <summary>
        /// Gets all classes
        /// </summary>
        /// <returns>All classes</returns>
        Task<IEnumerable<Class>> GetAllClassesAsync();

        /// <summary>
        /// Gets a class by ID
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>The class or null if not found</returns>
        Task<Class?> GetClassByIdAsync(string classId);

        /// <summary>
        /// Gets a class by code
        /// </summary>
        /// <param name="classCode">The class code</param>
        /// <returns>The class or null if not found</returns>
        Task<Class?> GetClassByCodeAsync(string classCode);

        /// <summary>
        /// Adds a new class
        /// </summary>
        /// <param name="classEntity">The class to add</param>
        /// <returns>The ID of the added class</returns>
        Task<string> AddClassAsync(Class classEntity);

        /// <summary>
        /// Updates an existing class
        /// </summary>
        /// <param name="classEntity">The class to update</param>
        Task UpdateClassAsync(Class classEntity);

        /// <summary>
        /// Deletes a class
        /// </summary>
        /// <param name="classId">The ID of the class to delete</param>
        /// <returns>True if the class was deleted, false otherwise</returns>
        Task<bool> DeleteClassAsync(string classId);

        /// <summary>
        /// Gets all students for a class
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Students in the class</returns>
        Task<IEnumerable<Student>> GetStudentsForClassAsync(string classId);
    }
}
