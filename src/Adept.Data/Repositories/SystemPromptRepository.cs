using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for system prompt data operations
    /// </summary>
    public class SystemPromptRepository : BaseRepository<SystemPrompt>, ISystemPromptService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemPromptRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public SystemPromptRepository(IDatabaseContext databaseContext, ILogger<SystemPromptRepository> logger)
            : base(databaseContext, logger)
        {
        }

        /// <summary>
        /// Gets all system prompts
        /// </summary>
        /// <returns>All system prompts</returns>
        public async Task<IEnumerable<SystemPrompt>> GetAllPromptsAsync()
        {
            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<SystemPrompt>(
                    @"SELECT
                        prompt_id AS PromptId,
                        name AS Name,
                        content AS Content,
                        is_default AS IsDefault,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM SystemPrompts
                      ORDER BY name"),
                "Error getting all system prompts",
                Enumerable.Empty<SystemPrompt>());
        }

        /// <summary>
        /// Gets a system prompt by ID
        /// </summary>
        /// <param name="promptId">The prompt ID</param>
        /// <returns>The system prompt or null if not found</returns>
        public async Task<SystemPrompt?> GetPromptByIdAsync(string promptId)
        {
            ValidateId(promptId, "prompt");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<SystemPrompt>(
                    @"SELECT
                        prompt_id AS PromptId,
                        name AS Name,
                        content AS Content,
                        is_default AS IsDefault,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM SystemPrompts
                      WHERE prompt_id = @PromptId",
                    new { PromptId = promptId }),
                $"Error getting system prompt by ID {promptId}");
        }

        /// <summary>
        /// Gets the default system prompt
        /// </summary>
        /// <returns>The default system prompt</returns>
        public async Task<SystemPrompt> GetDefaultPromptAsync()
        {
            try
            {
                var defaultPrompt = await ExecuteWithErrorHandlingAsync(
                    async () => await DatabaseContext.QuerySingleOrDefaultAsync<SystemPrompt>(
                        @"SELECT
                            prompt_id AS PromptId,
                            name AS Name,
                            content AS Content,
                            is_default AS IsDefault,
                            created_at AS CreatedAt,
                            updated_at AS UpdatedAt
                          FROM SystemPrompts
                          WHERE is_default = 1"),
                    "Error getting default system prompt");

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
                Logger.LogError(ex, "Error getting default system prompt");

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
            ValidateId(promptId, "prompt");

            await ExecuteInTransactionAsync(
                async (transaction) =>
                {
                    // Check if prompt exists
                    var prompt = await GetPromptByIdAsync(promptId);
                    if (prompt == null)
                    {
                        throw new InvalidOperationException($"System prompt with ID {promptId} not found");
                    }

                    // Clear the current default
                    await DatabaseContext.ExecuteNonQueryAsync(
                        "UPDATE SystemPrompts SET is_default = 0");

                    // Set the new default
                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"UPDATE SystemPrompts SET
                            is_default = 1,
                            updated_at = CURRENT_TIMESTAMP
                          WHERE prompt_id = @PromptId",
                        new { PromptId = promptId });

                    return true;
                },
                $"Error setting default system prompt {promptId}");
        }

        /// <summary>
        /// Adds a new system prompt
        /// </summary>
        /// <param name="prompt">The prompt to add</param>
        /// <returns>The ID of the added prompt</returns>
        public async Task<string> AddPromptAsync(SystemPrompt prompt)
        {
            ValidateEntityNotNull(prompt, nameof(prompt));
            ValidateStringNotNullOrEmpty(prompt.Name, "Name");
            ValidateStringNotNullOrEmpty(prompt.Content, "Content");

            return await ExecuteInTransactionAsync(
                async (transaction) =>
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
                        await DatabaseContext.ExecuteNonQueryAsync(
                            "UPDATE SystemPrompts SET is_default = 0");
                    }

                    await DatabaseContext.ExecuteNonQueryAsync(
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

                    return prompt.PromptId;
                },
                $"Error adding system prompt {prompt.Name}");
        }

        /// <summary>
        /// Updates an existing system prompt
        /// </summary>
        /// <param name="prompt">The prompt to update</param>
        public async Task UpdatePromptAsync(SystemPrompt prompt)
        {
            ValidateEntityNotNull(prompt, nameof(prompt));
            ValidateId(prompt.PromptId, "prompt");
            ValidateStringNotNullOrEmpty(prompt.Name, "Name");
            ValidateStringNotNullOrEmpty(prompt.Content, "Content");

            await ExecuteInTransactionAsync(
                async (transaction) =>
                {
                    // Check if prompt exists
                    var existingPrompt = await GetPromptByIdAsync(prompt.PromptId);
                    if (existingPrompt == null)
                    {
                        throw new InvalidOperationException($"System prompt with ID {prompt.PromptId} not found");
                    }

                    prompt.CreatedAt = existingPrompt.CreatedAt; // Preserve original creation date
                    prompt.UpdatedAt = DateTime.UtcNow;

                    // If this is the default prompt, clear other defaults
                    if (prompt.IsDefault)
                    {
                        await DatabaseContext.ExecuteNonQueryAsync(
                            "UPDATE SystemPrompts SET is_default = 0");
                    }

                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"UPDATE SystemPrompts SET
                            name = @Name,
                            content = @Content,
                            is_default = @IsDefault,
                            updated_at = @UpdatedAt
                          WHERE prompt_id = @PromptId",
                        prompt);

                    return true;
                },
                $"Error updating system prompt {prompt.PromptId}");
        }

        /// <summary>
        /// Deletes a system prompt
        /// </summary>
        /// <param name="promptId">The ID of the prompt to delete</param>
        public async Task DeletePromptAsync(string promptId)
        {
            ValidateId(promptId, "prompt");

            await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    // Check if prompt exists
                    var prompt = await GetPromptByIdAsync(promptId);
                    if (prompt == null)
                    {
                        throw new InvalidOperationException($"System prompt with ID {promptId} not found");
                    }

                    // Check if this is the default prompt
                    if (prompt.IsDefault)
                    {
                        throw new InvalidOperationException("Cannot delete the default system prompt");
                    }

                    await DatabaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM SystemPrompts WHERE prompt_id = @PromptId",
                        new { PromptId = promptId });

                    return true;
                },
                $"Error deleting system prompt {promptId}");
        }
    }
}
