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

    /// <summary>
    /// System prompt for LLM interactions
    /// </summary>
    public class SystemPrompt
    {
        /// <summary>
        /// The ID of the prompt
        /// </summary>
        public string PromptId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The name of the prompt
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The content of the prompt
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is the default prompt
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// When the prompt was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the prompt was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
