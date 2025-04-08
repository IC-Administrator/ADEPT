using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for system prompt data operations
    /// </summary>
    public class SystemPromptRepository : ISystemPromptService
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly ILogger<SystemPromptRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemPromptRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public SystemPromptRepository(IDatabaseContext databaseContext, ILogger<SystemPromptRepository> logger)
        {
            _databaseContext = databaseContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets all system prompts
        /// </summary>
        /// <returns>All system prompts</returns>
        public async Task<IEnumerable<SystemPrompt>> GetAllPromptsAsync()
        {
            try
            {
                return await _databaseContext.QueryAsync<SystemPrompt>(
                    @"SELECT 
                        prompt_id AS PromptId, 
                        name AS Name, 
                        content AS Content, 
                        is_default AS IsDefault, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                      FROM SystemPrompts 
                      ORDER BY name");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all system prompts");
                return Enumerable.Empty<SystemPrompt>();
            }
        }

        /// <summary>
        /// Gets a system prompt by ID
        /// </summary>
        /// <param name="promptId">The prompt ID</param>
        /// <returns>The system prompt or null if not found</returns>
        public async Task<SystemPrompt?> GetPromptByIdAsync(string promptId)
        {
            try
            {
                return await _databaseContext.QuerySingleOrDefaultAsync<SystemPrompt>(
                    @"SELECT 
                        prompt_id AS PromptId, 
                        name AS Name, 
                        content AS Content, 
                        is_default AS IsDefault, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                      FROM SystemPrompts 
                      WHERE prompt_id = @PromptId",
                    new { PromptId = promptId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system prompt by ID {PromptId}", promptId);
                return null;
            }
        }

        /// <summary>
        /// Gets the default system prompt
        /// </summary>
        /// <returns>The default system prompt</returns>
        public async Task<SystemPrompt> GetDefaultPromptAsync()
        {
            try
            {
                var defaultPrompt = await _databaseContext.QuerySingleOrDefaultAsync<SystemPrompt>(
                    @"SELECT 
                        prompt_id AS PromptId, 
                        name AS Name, 
                        content AS Content, 
                        is_default AS IsDefault, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                      FROM SystemPrompts 
                      WHERE is_default = 1");

                if (defaultPrompt != null)
                {
                    return defaultPrompt;
                }

                // If no default prompt exists, create one
                var defaultPromptContent = @"You are Adept, an AI teaching assistant for science teachers. 
Your goal is to help teachers with lesson planning, resource creation, and classroom instruction.
Be concise, helpful, and focus on providing accurate scientific information.
When asked about scientific concepts, provide clear explanations suitable for the specified education level.
For lesson planning, suggest activities, questions, and resources that align with curriculum standards.
Always consider the needs of diverse learners and suggest differentiation strategies when appropriate.";

                var newDefaultPrompt = new SystemPrompt
                {
                    Name = "Default Teaching Assistant",
                    Content = defaultPromptContent,
                    IsDefault = true
                };

                await AddPromptAsync(newDefaultPrompt);
                return newDefaultPrompt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default system prompt");
                
                // Return a basic default prompt if we can't get one from the database
                return new SystemPrompt
                {
                    PromptId = Guid.NewGuid().ToString(),
                    Name = "Basic Default",
                    Content = "You are Adept, an AI teaching assistant. Be helpful and concise.",
                    IsDefault = true
                };
            }
        }

        /// <summary>
        /// Sets the default system prompt
        /// </summary>
        /// <param name="promptId">The ID of the prompt to set as default</param>
        public async Task SetDefaultPromptAsync(string promptId)
        {
            using var transaction = await _databaseContext.BeginTransactionAsync();
            try
            {
                // Clear the current default
                await _databaseContext.ExecuteNonQueryAsync(
                    "UPDATE SystemPrompts SET is_default = 0");

                // Set the new default
                await _databaseContext.ExecuteNonQueryAsync(
                    @"UPDATE SystemPrompts SET 
                        is_default = 1, 
                        updated_at = CURRENT_TIMESTAMP 
                      WHERE prompt_id = @PromptId",
                    new { PromptId = promptId });

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error setting default system prompt {PromptId}", promptId);
                throw;
            }
        }

        /// <summary>
        /// Adds a new system prompt
        /// </summary>
        /// <param name="prompt">The prompt to add</param>
        /// <returns>The ID of the added prompt</returns>
        public async Task<string> AddPromptAsync(SystemPrompt prompt)
        {
            using var transaction = await _databaseContext.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(prompt.PromptId))
                {
                    prompt.PromptId = Guid.NewGuid().ToString();
                }

                prompt.CreatedAt = DateTime.UtcNow;
                prompt.UpdatedAt = DateTime.UtcNow;

                // If this is the default prompt, clear other defaults
                if (prompt.IsDefault)
                {
                    await _databaseContext.ExecuteNonQueryAsync(
                        "UPDATE SystemPrompts SET is_default = 0");
                }

                await _databaseContext.ExecuteNonQueryAsync(
                    @"INSERT INTO SystemPrompts (
                        prompt_id, 
                        name, 
                        content, 
                        is_default, 
                        created_at, 
                        updated_at
                      ) VALUES (
                        @PromptId, 
                        @Name, 
                        @Content, 
                        @IsDefault, 
                        @CreatedAt, 
                        @UpdatedAt
                      )",
                    prompt);

                await transaction.CommitAsync();
                return prompt.PromptId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding system prompt {Name}", prompt.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing system prompt
        /// </summary>
        /// <param name="prompt">The prompt to update</param>
        public async Task UpdatePromptAsync(SystemPrompt prompt)
        {
            using var transaction = await _databaseContext.BeginTransactionAsync();
            try
            {
                prompt.UpdatedAt = DateTime.UtcNow;

                // If this is the default prompt, clear other defaults
                if (prompt.IsDefault)
                {
                    await _databaseContext.ExecuteNonQueryAsync(
                        "UPDATE SystemPrompts SET is_default = 0");
                }

                await _databaseContext.ExecuteNonQueryAsync(
                    @"UPDATE SystemPrompts SET 
                        name = @Name, 
                        content = @Content, 
                        is_default = @IsDefault, 
                        updated_at = @UpdatedAt 
                      WHERE prompt_id = @PromptId",
                    prompt);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating system prompt {PromptId}", prompt.PromptId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a system prompt
        /// </summary>
        /// <param name="promptId">The ID of the prompt to delete</param>
        public async Task DeletePromptAsync(string promptId)
        {
            try
            {
                // Check if this is the default prompt
                var isDefault = await _databaseContext.ExecuteScalarAsync<int>(
                    "SELECT is_default FROM SystemPrompts WHERE prompt_id = @PromptId",
                    new { PromptId = promptId });

                if (isDefault == 1)
                {
                    throw new InvalidOperationException("Cannot delete the default system prompt");
                }

                await _databaseContext.ExecuteNonQueryAsync(
                    "DELETE FROM SystemPrompts WHERE prompt_id = @PromptId",
                    new { PromptId = promptId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system prompt {PromptId}", promptId);
                throw;
            }
        }
    }
}
