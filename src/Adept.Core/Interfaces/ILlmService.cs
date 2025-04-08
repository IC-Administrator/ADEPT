namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Service for interacting with Large Language Models
    /// </summary>
    public interface ILlmService
    {
        /// <summary>
        /// Gets the currently active LLM provider
        /// </summary>
        ILlmProvider ActiveProvider { get; }

        /// <summary>
        /// Gets all available LLM providers
        /// </summary>
        IEnumerable<ILlmProvider> AvailableProviders { get; }

        /// <summary>
        /// Sets the active LLM provider
        /// </summary>
        /// <param name="providerName">The name of the provider to set as active</param>
        /// <returns>True if the provider was set, false if the provider was not found</returns>
        Task<bool> SetActiveProviderAsync(string providerName);

        /// <summary>
        /// Gets a provider by name
        /// </summary>
        /// <param name="providerName">The name of the provider</param>
        /// <returns>The provider or null if not found</returns>
        ILlmProvider? GetProvider(string providerName);

        /// <summary>
        /// Sends a message to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        Task<LlmResponse> SendMessageAsync(
            string message,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        Task<LlmResponse> SendMessagesAsync(
            IEnumerable<LlmMessage> messages,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a streaming response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="onChunk">Callback for each chunk of the response</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complete LLM response</returns>
        Task<LlmResponse> SendMessagesStreamingAsync(
            IEnumerable<LlmMessage> messages,
            Action<string> onChunk,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message with an image to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="imageData">The image data</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        Task<LlmResponse> SendMessageWithImageAsync(
            string message,
            byte[] imageData,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the conversation history for a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The conversation history</returns>
        Task<IEnumerable<LlmMessage>> GetConversationHistoryAsync(string conversationId);

        /// <summary>
        /// Creates a new conversation
        /// </summary>
        /// <param name="name">Optional name for the conversation</param>
        /// <returns>The conversation ID</returns>
        Task<string> CreateConversationAsync(string? name = null);

        /// <summary>
        /// Deletes a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        Task DeleteConversationAsync(string conversationId);
    }

    /// <summary>
    /// LLM message role
    /// </summary>
    public enum LlmRole
    {
        /// <summary>
        /// System message
        /// </summary>
        System,

        /// <summary>
        /// User message
        /// </summary>
        User,

        /// <summary>
        /// Assistant message
        /// </summary>
        Assistant,

        /// <summary>
        /// Tool message
        /// </summary>
        Tool
    }

    /// <summary>
    /// LLM message
    /// </summary>
    public class LlmMessage
    {
        /// <summary>
        /// The role of the message sender
        /// </summary>
        public LlmRole Role { get; set; }

        /// <summary>
        /// The content of the message
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// The name of the tool (for tool messages)
        /// </summary>
        public string? ToolName { get; set; }

        /// <summary>
        /// The timestamp of the message
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmMessage"/> class
        /// </summary>
        public LlmMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmMessage"/> class
        /// </summary>
        /// <param name="role">The role of the message sender</param>
        /// <param name="content">The content of the message</param>
        public LlmMessage(LlmRole role, string content)
        {
            Role = role;
            Content = content;
        }

        /// <summary>
        /// Creates a system message
        /// </summary>
        /// <param name="content">The content of the message</param>
        /// <returns>A system message</returns>
        public static LlmMessage System(string content) => new LlmMessage(LlmRole.System, content);

        /// <summary>
        /// Creates a user message
        /// </summary>
        /// <param name="content">The content of the message</param>
        /// <returns>A user message</returns>
        public static LlmMessage User(string content) => new LlmMessage(LlmRole.User, content);

        /// <summary>
        /// Creates an assistant message
        /// </summary>
        /// <param name="content">The content of the message</param>
        /// <returns>An assistant message</returns>
        public static LlmMessage Assistant(string content) => new LlmMessage(LlmRole.Assistant, content);

        /// <summary>
        /// Creates a tool message
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="content">The content of the message</param>
        /// <returns>A tool message</returns>
        public static LlmMessage Tool(string toolName, string content)
        {
            return new LlmMessage(LlmRole.Tool, content)
            {
                ToolName = toolName
            };
        }
    }

    /// <summary>
    /// LLM response
    /// </summary>
    public class LlmResponse
    {
        /// <summary>
        /// The response message
        /// </summary>
        public LlmMessage Message { get; set; } = new LlmMessage();

        /// <summary>
        /// The conversation ID
        /// </summary>
        public string? ConversationId { get; set; }

        /// <summary>
        /// The provider that generated the response
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// The model that generated the response
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// The usage information
        /// </summary>
        public LlmUsage Usage { get; set; } = new LlmUsage();

        /// <summary>
        /// Any tool calls in the response
        /// </summary>
        public List<LlmToolCall> ToolCalls { get; set; } = new List<LlmToolCall>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmResponse"/> class
        /// </summary>
        public LlmResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmResponse"/> class
        /// </summary>
        /// <param name="content">The content of the response</param>
        /// <param name="providerName">The provider that generated the response</param>
        /// <param name="modelName">The model that generated the response</param>
        public LlmResponse(string content, string providerName, string modelName)
        {
            Message = LlmMessage.Assistant(content);
            ProviderName = providerName;
            ModelName = modelName;
        }
    }

    /// <summary>
    /// LLM usage information
    /// </summary>
    public class LlmUsage
    {
        /// <summary>
        /// The number of prompt tokens
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// The number of completion tokens
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// The total number of tokens
        /// </summary>
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// LLM tool call
    /// </summary>
    public class LlmToolCall
    {
        /// <summary>
        /// The ID of the tool call
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the tool
        /// </summary>
        public string ToolName { get; set; } = string.Empty;

        /// <summary>
        /// The arguments for the tool call
        /// </summary>
        public string Arguments { get; set; } = string.Empty;
    }
}
