using Adept.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Interface for lesson resource repository
    /// </summary>
    public interface ILessonResourceRepository
    {
        /// <summary>
        /// Gets all resources for a lesson
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        /// <returns>A collection of resources</returns>
        Task<IEnumerable<LessonResource>> GetResourcesByLessonIdAsync(Guid lessonId);
        
        /// <summary>
        /// Gets a resource by ID
        /// </summary>
        /// <param name="resourceId">The resource ID</param>
        /// <returns>The resource</returns>
        Task<LessonResource> GetResourceByIdAsync(Guid resourceId);
        
        /// <summary>
        /// Adds a resource
        /// </summary>
        /// <param name="resource">The resource to add</param>
        /// <returns>The added resource</returns>
        Task<LessonResource> AddResourceAsync(LessonResource resource);
        
        /// <summary>
        /// Updates a resource
        /// </summary>
        /// <param name="resource">The resource to update</param>
        /// <returns>The updated resource</returns>
        Task<LessonResource> UpdateResourceAsync(LessonResource resource);
        
        /// <summary>
        /// Deletes a resource
        /// </summary>
        /// <param name="resourceId">The resource ID</param>
        /// <returns>True if the resource was deleted, false otherwise</returns>
        Task<bool> DeleteResourceAsync(Guid resourceId);
        
        /// <summary>
        /// Deletes all resources for a lesson
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        /// <returns>True if the resources were deleted, false otherwise</returns>
        Task<bool> DeleteResourcesByLessonIdAsync(Guid lessonId);
    }
}
