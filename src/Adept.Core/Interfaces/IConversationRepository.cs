using Adept.Core.Models;

namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Repository for conversation data operations
    /// </summary>
    public interface IConversationRepository
    {
        /// <summary>
        /// Gets all conversations
        /// </summary>
        /// <returns>All conversations</returns>
        Task<IEnumerable<Conversation>> GetAllConversationsAsync();

        /// <summary>
        /// Gets a conversation by ID
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The conversation or null if not found</returns>
        Task<Conversation?> GetConversationByIdAsync(string conversationId);

        /// <summary>
        /// Gets a conversation by ID (alias for GetConversationByIdAsync)
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The conversation or null if not found</returns>
        Task<Conversation?> GetConversationAsync(string conversationId);

        /// <summary>
        /// Gets conversations for a class
        /// </summary>
        /// <param name="classId">The class ID</param>
        /// <returns>Conversations for the class</returns>
        Task<IEnumerable<Conversation>> GetConversationsByClassIdAsync(string classId);

        /// <summary>
        /// Gets conversations for a date
        /// </summary>
        /// <param name="date">The date (YYYY-MM-DD)</param>
        /// <returns>Conversations for the date</returns>
        Task<IEnumerable<Conversation>> GetConversationsByDateAsync(string date);

        /// <summary>
        /// Adds a new conversation
        /// </summary>
        /// <param name="conversation">The conversation to add</param>
        /// <returns>The ID of the added conversation</returns>
        Task<string> AddConversationAsync(Conversation conversation);

        /// <summary>
        /// Updates an existing conversation
        /// </summary>
        /// <param name="conversation">The conversation to update</param>
        Task UpdateConversationAsync(Conversation conversation);

        /// <summary>
        /// Deletes a conversation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to delete</param>
        Task DeleteConversationAsync(string conversationId);

        /// <summary>
        /// Adds a message to a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="message">The message to add</param>
        Task AddMessageToConversationAsync(string conversationId, LlmMessage message);
    }
}
