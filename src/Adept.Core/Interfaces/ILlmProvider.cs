namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Provider for Large Language Model services
    /// </summary>
    public interface ILlmProvider
    {
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the name of the currently selected model
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// Gets the available models for this provider
        /// </summary>
        IEnumerable<LlmModel> AvailableModels { get; }

        /// <summary>
        /// Gets the currently selected model
        /// </summary>
        LlmModel CurrentModel { get; }

        /// <summary>
        /// Gets whether the provider requires an API key
        /// </summary>
        bool RequiresApiKey { get; }

        /// <summary>
        /// Gets whether the provider has a valid API key
        /// </summary>
        bool HasValidApiKey { get; }

        /// <summary>
        /// Gets whether the provider supports streaming
        /// </summary>
        bool SupportsStreaming { get; }

        /// <summary>
        /// Gets whether the provider supports tool calls
        /// </summary>
        bool SupportsToolCalls { get; }

        /// <summary>
        /// Gets whether the provider supports vision
        /// </summary>
        bool SupportsVision { get; }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Fetches the latest available models from the provider's API
        /// </summary>
        /// <returns>A collection of available models</returns>
        Task<IEnumerable<LlmModel>> FetchAvailableModelsAsync();

        /// <summary>
        /// Sets the API key for the provider
        /// </summary>
        /// <param name="apiKey">The API key</param>
        Task SetApiKeyAsync(string apiKey);

        /// <summary>
        /// Sets the current model
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>True if the model was set, false if the model was not found</returns>
        Task<bool> SetModelAsync(string modelId);

        /// <summary>
        /// Sends a message to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        Task<LlmResponse> SendMessageAsync(
            string message,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        Task<LlmResponse> SendMessagesAsync(
            IEnumerable<LlmMessage> messages,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a streaming response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="onPartialResponse">Callback for partial responses</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complete LLM response</returns>
        Task<LlmResponse> SendMessagesStreamingAsync(
            IEnumerable<LlmMessage> messages,
            string? systemPrompt = null,
            Action<string>? onPartialResponse = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message with tool definitions to the LLM and gets a response with tool calls
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="tools">The tool definitions</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response with tool calls</returns>
        Task<LlmResponse> SendMessagesWithToolsAsync(
            IEnumerable<LlmMessage> messages,
            IEnumerable<LlmTool> tools,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// LLM model information
    /// </summary>
    public class LlmModel
    {
        /// <summary>
        /// The ID of the model
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the model
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The maximum context length in tokens
        /// </summary>
        public int MaxContextLength { get; set; }

        /// <summary>
        /// Whether the model supports tool calls
        /// </summary>
        public bool SupportsToolCalls { get; set; }

        /// <summary>
        /// Whether the model supports vision
        /// </summary>
        public bool SupportsVision { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmModel"/> class
        /// </summary>
        public LlmModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmModel"/> class
        /// </summary>
        /// <param name="id">The ID of the model</param>
        /// <param name="name">The name of the model</param>
        /// <param name="maxContextLength">The maximum context length in tokens</param>
        /// <param name="supportsToolCalls">Whether the model supports tool calls</param>
        /// <param name="supportsVision">Whether the model supports vision</param>
        public LlmModel(string id, string name, int maxContextLength, bool supportsToolCalls = false, bool supportsVision = false)
        {
            Id = id;
            Name = name;
            MaxContextLength = maxContextLength;
            SupportsToolCalls = supportsToolCalls;
            SupportsVision = supportsVision;
        }
    }

    /// <summary>
    /// LLM tool definition
    /// </summary>
    public class LlmTool
    {
        /// <summary>
        /// The type of the tool
        /// </summary>
        public string Type { get; set; } = "function";

        /// <summary>
        /// The function definition
        /// </summary>
        public LlmFunction Function { get; set; } = new LlmFunction();

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmTool"/> class
        /// </summary>
        public LlmTool()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmTool"/> class
        /// </summary>
        /// <param name="name">The name of the function</param>
        /// <param name="description">The description of the function</param>
        /// <param name="parameters">The parameters schema</param>
        public LlmTool(string name, string description, object parameters)
        {
            Function = new LlmFunction
            {
                Name = name,
                Description = description,
                Parameters = parameters
            };
        }
    }

    /// <summary>
    /// LLM function definition
    /// </summary>
    public class LlmFunction
    {
        /// <summary>
        /// The name of the function
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The description of the function
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The parameters schema
        /// </summary>
        public object Parameters { get; set; } = new object();
    }
}
