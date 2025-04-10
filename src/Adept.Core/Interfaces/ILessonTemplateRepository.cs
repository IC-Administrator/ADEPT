using Adept.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Interface for lesson template repository
    /// </summary>
    public interface ILessonTemplateRepository
    {
        /// <summary>
        /// Gets all lesson templates
        /// </summary>
        /// <returns>A collection of lesson templates</returns>
        Task<IEnumerable<LessonTemplate>> GetAllTemplatesAsync();
        
        /// <summary>
        /// Gets a lesson template by ID
        /// </summary>
        /// <param name="templateId">The template ID</param>
        /// <returns>The lesson template</returns>
        Task<LessonTemplate> GetTemplateByIdAsync(Guid templateId);
        
        /// <summary>
        /// Gets lesson templates by category
        /// </summary>
        /// <param name="category">The category</param>
        /// <returns>A collection of lesson templates</returns>
        Task<IEnumerable<LessonTemplate>> GetTemplatesByCategoryAsync(string category);
        
        /// <summary>
        /// Gets lesson templates by tag
        /// </summary>
        /// <param name="tag">The tag</param>
        /// <returns>A collection of lesson templates</returns>
        Task<IEnumerable<LessonTemplate>> GetTemplatesByTagAsync(string tag);
        
        /// <summary>
        /// Adds a lesson template
        /// </summary>
        /// <param name="template">The template to add</param>
        /// <returns>The added template</returns>
        Task<LessonTemplate> AddTemplateAsync(LessonTemplate template);
        
        /// <summary>
        /// Updates a lesson template
        /// </summary>
        /// <param name="template">The template to update</param>
        /// <returns>The updated template</returns>
        Task<LessonTemplate> UpdateTemplateAsync(LessonTemplate template);
        
        /// <summary>
        /// Deletes a lesson template
        /// </summary>
        /// <param name="templateId">The template ID</param>
        /// <returns>True if the template was deleted, false otherwise</returns>
        Task<bool> DeleteTemplateAsync(Guid templateId);
        
        /// <summary>
        /// Searches for lesson templates
        /// </summary>
        /// <param name="searchTerm">The search term</param>
        /// <returns>A collection of lesson templates</returns>
        Task<IEnumerable<LessonTemplate>> SearchTemplatesAsync(string searchTerm);
    }
}
