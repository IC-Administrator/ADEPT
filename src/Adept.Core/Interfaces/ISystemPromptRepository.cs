using Adept.Core.Models;

namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Repository for system prompt data operations
    /// </summary>
    public interface ISystemPromptRepository
    {
        /// <summary>
        /// Gets all system prompts
        /// </summary>
        /// <returns>All system prompts</returns>
        Task<IEnumerable<SystemPrompt>> GetAllPromptsAsync();

        /// <summary>
        /// Gets a system prompt by ID
        /// </summary>
        /// <param name="promptId">The prompt ID</param>
        /// <returns>The system prompt or null if not found</returns>
        Task<SystemPrompt?> GetPromptByIdAsync(string promptId);

        /// <summary>
        /// Gets the default system prompt
        /// </summary>
        /// <returns>The default system prompt</returns>
        Task<SystemPrompt?> GetDefaultPromptAsync();

        /// <summary>
        /// Adds a new system prompt
        /// </summary>
        /// <param name="prompt">The prompt to add</param>
        /// <returns>The added prompt</returns>
        Task<SystemPrompt> AddPromptAsync(SystemPrompt prompt);

        /// <summary>
        /// Updates an existing system prompt
        /// </summary>
        /// <param name="prompt">The prompt to update</param>
        /// <returns>The updated prompt or null if not found</returns>
        Task<SystemPrompt?> UpdatePromptAsync(SystemPrompt prompt);

        /// <summary>
        /// Deletes a system prompt
        /// </summary>
        /// <param name="promptId">The ID of the prompt to delete</param>
        /// <returns>True if the prompt was deleted, false otherwise</returns>
        Task<bool> DeletePromptAsync(string promptId);

        /// <summary>
        /// Sets the default system prompt
        /// </summary>
        /// <param name="promptId">The ID of the prompt to set as default</param>
        /// <returns>True if the prompt was set as default, false otherwise</returns>
        Task<bool> SetDefaultPromptAsync(string promptId);
    }
}
