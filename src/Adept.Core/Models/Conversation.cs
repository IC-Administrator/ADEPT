using Adept.Common.Json;
using Adept.Core.Interfaces;

namespace Adept.Core.Models
{
    /// <summary>
    /// Represents a conversation with an LLM
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// The ID of the conversation
        /// </summary>
        public string ConversationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The class ID associated with this conversation (optional)
        /// </summary>
        public string? ClassId { get; set; }

        /// <summary>
        /// The date of the conversation (YYYY-MM-DD)
        /// </summary>
        public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

        /// <summary>
        /// The time slot associated with this conversation (optional)
        /// </summary>
        public int? TimeSlot { get; set; }

        /// <summary>
        /// The conversation history as JSON
        /// </summary>
        public string HistoryJson { get; set; } = "[]";

        /// <summary>
        /// When the conversation was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the conversation was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the conversation history
        /// </summary>
        public List<LlmMessage> History
        {
            get
            {
                if (string.IsNullOrEmpty(HistoryJson) || HistoryJson == "[]")
                {
                    return new List<LlmMessage>();
                }

                if (HistoryJson.TryFromJson<List<LlmMessage>>(out var history) && history != null)
                {
                    return history;
                }

                return new List<LlmMessage>();
            }
            set
            {
                HistoryJson = value.ToJson();
            }
        }

        /// <summary>
        /// Adds a message to the conversation history
        /// </summary>
        /// <param name="message">The message to add</param>
        public void AddMessage(LlmMessage message)
        {
            var history = History;
            history.Add(message);
            History = history;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a user message to the conversation history
        /// </summary>
        /// <param name="content">The content of the message</param>
        public void AddUserMessage(string content)
        {
            AddMessage(LlmMessage.User(content));
        }

        /// <summary>
        /// Adds an assistant message to the conversation history
        /// </summary>
        /// <param name="content">The content of the message</param>
        public void AddAssistantMessage(string content)
        {
            AddMessage(LlmMessage.Assistant(content));
        }

        /// <summary>
        /// Adds a system message to the conversation history
        /// </summary>
        /// <param name="content">The content of the message</param>
        public void AddSystemMessage(string content)
        {
            AddMessage(LlmMessage.System(content));
        }

        /// <summary>
        /// Adds a tool message to the conversation history
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="content">The content of the message</param>
        public void AddToolMessage(string toolName, string content)
        {
            AddMessage(LlmMessage.Tool(toolName, content));
        }
    }
}
