using Adept.Core.Models;

namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Service for managing system prompts
    /// </summary>
    public interface ISystemPromptService
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
        Task<SystemPrompt> GetDefaultPromptAsync();

        /// <summary>
        /// Sets the default system prompt
        /// </summary>
        /// <param name="promptId">The ID of the prompt to set as default</param>
        Task SetDefaultPromptAsync(string promptId);

        /// <summary>
        /// Adds a new system prompt
        /// </summary>
        /// <param name="prompt">The prompt to add</param>
        /// <returns>The ID of the added prompt</returns>
        Task<string> AddPromptAsync(SystemPrompt prompt);

        /// <summary>
        /// Updates an existing system prompt
        /// </summary>
        /// <param name="prompt">The prompt to update</param>
        Task UpdatePromptAsync(SystemPrompt prompt);

        /// <summary>
        /// Deletes a system prompt
        /// </summary>
        /// <param name="promptId">The ID of the prompt to delete</param>
        Task DeletePromptAsync(string promptId);
    }
}
