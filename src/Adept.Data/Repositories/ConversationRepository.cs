using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for conversation data operations
    /// </summary>
    public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public ConversationRepository(IDatabaseContext databaseContext, ILogger<ConversationRepository> logger)
            : base(databaseContext, logger)
        {
        }

        /// <summary>
        /// Gets all conversations
        /// </summary>
        /// <returns>All conversations</returns>
        public async Task<IEnumerable<Conversation>> GetAllConversationsAsync()
        {
            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<Conversation>(
                    @"SELECT
                        conversation_id AS ConversationId,
                        class_id AS ClassId,
                        date AS Date,
                        time_slot AS TimeSlot,
                        history_json AS HistoryJson,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Conversations
                      ORDER BY updated_at DESC"),
                "Error getting all conversations",
                Enumerable.Empty<Conversation>());
        }

        /// <summary>
        /// Gets a conversation by ID
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The conversation or null if not found</returns>
        public async Task<Conversation?> GetConversationByIdAsync(string conversationId)
        {
            ValidateId(conversationId, "conversation");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<Conversation>(
                    @"SELECT
                        conversation_id AS ConversationId,
                        class_id AS ClassId,
                        date AS Date,
                        time_slot AS TimeSlot,
                        history_json AS HistoryJson,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Conversations
                      WHERE conversation_id = @ConversationId",
                    new { ConversationId = conversationId }),
                $"Error getting conversation by ID {conversationId}");
        }

        /// <summary>
        /// Gets a conversation by ID (alias for GetConversationByIdAsync)
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The conversation or null if not found</returns>
        public async Task<Conversation?> GetConversationAsync(string conversationId)
        {
            return await GetConversationByIdAsync(conversationId);
        }

        /// <summary>
        /// Gets conversations for a class
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Conversations for the class</returns>
        public async Task<IEnumerable<Conversation>> GetConversationsByClassIdAsync(string classId)
        {
            ValidateId(classId, "class");

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<Conversation>(
                    @"SELECT
                        conversation_id AS ConversationId,
                        class_id AS ClassId,
                        date AS Date,
                        time_slot AS TimeSlot,
                        history_json AS HistoryJson,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Conversations
                      WHERE class_id = @ClassId
                      ORDER BY updated_at DESC",
                    new { ClassId = classId }),
                $"Error getting conversations for class {classId}",
                Enumerable.Empty<Conversation>());
        }

        /// <summary>
        /// Gets conversations for a date
        /// </summary>
        /// <param name="date">The date (YYYY-MM-DD)</param>
        /// <returns>Conversations for the date</returns>
        public async Task<IEnumerable<Conversation>> GetConversationsByDateAsync(string date)
        {
            ValidateStringNotNullOrEmpty(date, "date");

            // Validate date format
            if (!DateTime.TryParse(date, out _))
            {
                throw new ArgumentException($"Invalid date format: {date}. Expected format: YYYY-MM-DD", nameof(date));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<Conversation>(
                    @"SELECT
                        conversation_id AS ConversationId,
                        class_id AS ClassId,
                        date AS Date,
                        time_slot AS TimeSlot,
                        history_json AS HistoryJson,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM Conversations
                      WHERE date = @Date
                      ORDER BY updated_at DESC",
                    new { Date = date }),
                $"Error getting conversations for date {date}",
                Enumerable.Empty<Conversation>());
        }

        /// <summary>
        /// Adds a new conversation
        /// </summary>
        /// <param name="conversation">The conversation to add</param>
        /// <returns>The ID of the added conversation</returns>
        public async Task<string> AddConversationAsync(Conversation conversation)
        {
            ValidateEntityNotNull(conversation, nameof(conversation));
            ValidateStringNotNullOrEmpty(conversation.ClassId, "ClassId");
            ValidateStringNotNullOrEmpty(conversation.Date, "Date");

            // Validate date format
            if (!DateTime.TryParse(conversation.Date, out _))
            {
                throw new ArgumentException($"Invalid date format: {conversation.Date}. Expected format: YYYY-MM-DD", nameof(conversation));
            }

            // Validate time slot
            if (conversation.TimeSlot < 0 || conversation.TimeSlot > 4)
            {
                throw new ArgumentException($"Invalid time slot: {conversation.TimeSlot}. Expected range: 0-4", nameof(conversation));
            }

            return await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    if (string.IsNullOrEmpty(conversation.ConversationId))
                    {
                        conversation.ConversationId = Guid.NewGuid().ToString();
                    }

                    conversation.CreatedAt = DateTime.UtcNow;
                    conversation.UpdatedAt = DateTime.UtcNow;

                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"INSERT INTO Conversations (
                            conversation_id,
                            class_id,
                            date,
                            time_slot,
                            history_json,
                            created_at,
                            updated_at
                          ) VALUES (
                            @ConversationId,
                            @ClassId,
                            @Date,
                            @TimeSlot,
                            @HistoryJson,
                            @CreatedAt,
                            @UpdatedAt
                          )",
                        conversation);

                    return conversation.ConversationId;
                },
                $"Error adding conversation for class {conversation.ClassId}");
        }

        /// <summary>
        /// Updates an existing conversation
        /// </summary>
        /// <param name="conversation">The conversation to update</param>
        public async Task UpdateConversationAsync(Conversation conversation)
        {
            ValidateEntityNotNull(conversation, nameof(conversation));
            ValidateId(conversation.ConversationId, "conversation");
            ValidateStringNotNullOrEmpty(conversation.ClassId, "ClassId");
            ValidateStringNotNullOrEmpty(conversation.Date, "Date");

            // Validate date format
            if (!DateTime.TryParse(conversation.Date, out _))
            {
                throw new ArgumentException($"Invalid date format: {conversation.Date}. Expected format: YYYY-MM-DD", nameof(conversation));
            }

            // Validate time slot
            if (conversation.TimeSlot < 0 || conversation.TimeSlot > 4)
            {
                throw new ArgumentException($"Invalid time slot: {conversation.TimeSlot}. Expected range: 0-4", nameof(conversation));
            }

            await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    // Check if conversation exists
                    var existingConversation = await GetConversationByIdAsync(conversation.ConversationId);
                    if (existingConversation == null)
                    {
                        throw new InvalidOperationException($"Conversation with ID {conversation.ConversationId} not found");
                    }

                    conversation.UpdatedAt = DateTime.UtcNow;
                    conversation.CreatedAt = existingConversation.CreatedAt; // Preserve original creation date

                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"UPDATE Conversations SET
                            class_id = @ClassId,
                            date = @Date,
                            time_slot = @TimeSlot,
                            history_json = @HistoryJson,
                            updated_at = @UpdatedAt
                          WHERE conversation_id = @ConversationId",
                        conversation);

                    return true;
                },
                $"Error updating conversation {conversation.ConversationId}");
        }

        /// <summary>
        /// Deletes a conversation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to delete</param>
        public async Task DeleteConversationAsync(string conversationId)
        {
            ValidateId(conversationId, "conversation");

            await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    // Check if conversation exists
                    var existingConversation = await GetConversationByIdAsync(conversationId);
                    if (existingConversation == null)
                    {
                        throw new InvalidOperationException($"Conversation with ID {conversationId} not found");
                    }

                    await DatabaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM Conversations WHERE conversation_id = @ConversationId",
                        new { ConversationId = conversationId });

                    return true;
                },
                $"Error deleting conversation {conversationId}");
        }

        /// <summary>
        /// Adds a message to a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="message">The message to add</param>
        public async Task AddMessageToConversationAsync(string conversationId, Core.Interfaces.LlmMessage message)
        {
            ValidateId(conversationId, "conversation");

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message), "The message cannot be null");
            }

            await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    var conversation = await GetConversationByIdAsync(conversationId);
                    if (conversation == null)
                    {
                        throw new InvalidOperationException($"Conversation with ID {conversationId} not found");
                    }

                    conversation.AddMessage(message);
                    await UpdateConversationAsync(conversation);
                    return true;
                },
                $"Error adding message to conversation {conversationId}");
        }
    }
}
