using System.Text.Json.Serialization;

namespace Adept.Services.Llm.Providers
{
    /// <summary>
    /// OpenAI chat message
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Gets or sets the role of the message
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the message
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool calls
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }

        /// <summary>
        /// Gets or sets the tool call ID
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }
    }

    /// <summary>
    /// OpenAI tool call
    /// </summary>
    public class ToolCall
    {
        /// <summary>
        /// Gets or sets the ID of the tool call
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the tool call
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the function of the tool call
        /// </summary>
        [JsonPropertyName("function")]
        public ToolCallFunction Function { get; set; } = new ToolCallFunction();
    }

    /// <summary>
    /// OpenAI tool call function
    /// </summary>
    public class ToolCallFunction
    {
        /// <summary>
        /// Gets or sets the name of the function
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the arguments of the function
        /// </summary>
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty;
    }
}
