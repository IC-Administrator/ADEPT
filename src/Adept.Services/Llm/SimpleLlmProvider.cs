using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Adept.Services.Llm
{
    /// <summary>
    /// Simple LLM provider for testing
    /// </summary>
    public class SimpleLlmProvider : ILlmProvider
    {
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<SimpleLlmProvider> _logger;
        private readonly List<LlmModel> _models;
        private LlmModel _currentModel;
        private bool _hasApiKey;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Simple LLM Provider";

        /// <summary>
        /// Gets the available models for this provider
        /// </summary>
        public IEnumerable<LlmModel> AvailableModels => _models;

        /// <summary>
        /// Gets the currently selected model
        /// </summary>
        public LlmModel CurrentModel => _currentModel;

        /// <summary>
        /// Gets whether the provider requires an API key
        /// </summary>
        public bool RequiresApiKey => false;

        /// <summary>
        /// Gets whether the provider has a valid API key
        /// </summary>
        public bool HasValidApiKey => _hasApiKey;

        /// <summary>
        /// Gets whether the provider supports streaming
        /// </summary>
        public bool SupportsStreaming => true;

        /// <summary>
        /// Gets whether the provider supports tool calls
        /// </summary>
        public bool SupportsToolCalls => _currentModel.SupportsToolCalls;

        /// <summary>
        /// Gets whether the provider supports vision
        /// </summary>
        public bool SupportsVision => _currentModel.SupportsVision;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleLlmProvider"/> class
        /// </summary>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public SimpleLlmProvider(ISecureStorageService secureStorageService, ILogger<SimpleLlmProvider> logger)
        {
            _secureStorageService = secureStorageService;
            _logger = logger;

            // Define available models
            _models = new List<LlmModel>
            {
                new LlmModel("simple-basic", "Simple Basic", "Basic model for testing", 4000, false, false),
                new LlmModel("simple-advanced", "Simple Advanced", "Advanced model with tool and vision support", 8000, true, true)
            };

            // Set the default model
            _currentModel = _models.First();
            _hasApiKey = true;
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Check if we have an API key (not actually needed for this provider)
                var apiKey = await _secureStorageService.RetrieveSecureValueAsync($"api_key_{ProviderName}");
                _hasApiKey = !string.IsNullOrEmpty(apiKey);

                _logger.LogInformation("Initialized {ProviderName}", ProviderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing {ProviderName}", ProviderName);
                throw;
            }
        }

        /// <summary>
        /// Sets the API key for the provider
        /// </summary>
        /// <param name="apiKey">The API key</param>
        public async Task SetApiKeyAsync(string apiKey)
        {
            try
            {
                await _secureStorageService.StoreSecureValueAsync($"api_key_{ProviderName}", apiKey);
                _hasApiKey = true;
                _logger.LogInformation("API key set for {ProviderName}", ProviderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting API key for {ProviderName}", ProviderName);
                throw;
            }
        }

        /// <summary>
        /// Sets the current model
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>True if the model was set, false if the model was not found</returns>
        public Task<bool> SetModelAsync(string modelId)
        {
            var model = _models.FirstOrDefault(m => m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
            if (model != null)
            {
                _currentModel = model;
                _logger.LogInformation("Model set to {ModelName} for {ProviderName}", model.Name, ProviderName);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Model not found: {ModelId} for {ProviderName}", modelId, ProviderName);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Sends a message to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public Task<LlmResponse> SendMessageAsync(
            string message,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            return SendMessagesAsync(
                new List<LlmMessage> { LlmMessage.User(message) },
                systemPrompt,
                cancellationToken);
        }

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public Task<LlmResponse> SendMessagesAsync(
            IEnumerable<LlmMessage> messages,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // In a real implementation, this would call an API like OpenAI or Anthropic
                // For now, we'll just return a simulated response
                var lastUserMessage = messages.LastOrDefault(m => m.Role == LlmRole.User)?.Content ?? "No user message";

                var response = $"This is a simulated response from {ProviderName} using {_currentModel.Name}. " +
                               $"You said: \"{lastUserMessage}\". " +
                               $"In a real implementation, this would be a response from an actual LLM API.";

                var llmResponse = new LlmResponse(response, ProviderName, _currentModel.Name)
                {
                    Usage = new LlmUsage
                    {
                        PromptTokens = 100,
                        CompletionTokens = 50,
                        TotalTokens = 150
                    }
                };

                _logger.LogInformation("Sent message to {ProviderName} using {ModelName}", ProviderName, _currentModel.Name);
                return Task.FromResult(llmResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to {ProviderName}", ProviderName);
                throw;
            }
        }

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a streaming response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="onPartialResponse">Callback for partial responses</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complete LLM response</returns>
        public async Task<LlmResponse> SendMessagesStreamingAsync(
            IEnumerable<LlmMessage> messages,
            string? systemPrompt = null,
            Action<string>? onPartialResponse = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // In a real implementation, this would stream responses from an API
                // For now, we'll simulate streaming by sending chunks of the response
                var lastUserMessage = messages.LastOrDefault(m => m.Role == LlmRole.User)?.Content ?? "No user message";

                var fullResponse = $"This is a simulated streaming response from {ProviderName} using {_currentModel.Name}. " +
                                  $"You said: \"{lastUserMessage}\". " +
                                  $"In a real implementation, this would be a streaming response from an actual LLM API.";

                // Split the response into chunks to simulate streaming
                var chunks = SplitIntoChunks(fullResponse, 10);

                foreach (var chunk in chunks)
                {
                    // Simulate network delay
                    await Task.Delay(100, cancellationToken);

                    // Call the callback with the chunk
                    onPartialResponse?.Invoke(chunk);
                }

                var llmResponse = new LlmResponse(fullResponse, ProviderName, _currentModel.Name)
                {
                    Usage = new LlmUsage
                    {
                        PromptTokens = 100,
                        CompletionTokens = 50,
                        TotalTokens = 150
                    }
                };

                _logger.LogInformation("Sent streaming message to {ProviderName} using {ModelName}", ProviderName, _currentModel.Name);
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming message to {ProviderName}", ProviderName);
                throw;
            }
        }

        /// <summary>
        /// Sends a message with tool definitions to the LLM and gets a response with tool calls
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="tools">The tool definitions</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response with tool calls</returns>
        public async Task<LlmResponse> SendMessagesWithToolsAsync(
            IEnumerable<LlmMessage> messages,
            IEnumerable<LlmTool> tools,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_currentModel.SupportsToolCalls)
                {
                    _logger.LogWarning("Tool calls not supported by model {ModelName}, falling back to non-tool", _currentModel.Name);
                    return await SendMessagesAsync(messages, systemPrompt, cancellationToken);
                }

                // In a real implementation, this would call an API with tool definitions
                // For now, we'll simulate a response with a tool call
                var lastUserMessage = messages.LastOrDefault(m => m.Role == LlmRole.User)?.Content ?? "No user message";

                var response = $"I'll help you with that. Let me check the weather for you.";

                // Create a simulated tool call
                var toolCalls = new List<LlmToolCall>
                {
                    new LlmToolCall
                    {
                        Id = Guid.NewGuid().ToString(),
                        ToolName = "get_current_weather",
                        Arguments = "{\"location\": \"New York\", \"unit\": \"celsius\"}"
                    }
                };

                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = LlmRole.Assistant,
                        Content = response
                    },
                    ToolCalls = toolCalls,
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = 100,
                        CompletionTokens = 50,
                        TotalTokens = 150
                    }
                };

                _logger.LogInformation("Sent message with tools to {ProviderName} using {ModelName}", ProviderName, _currentModel.Name);
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with tools to {ProviderName}", ProviderName);
                throw;
            }
        }

        /// <summary>
        /// Sends a message with an image to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="imageData">The image data</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public Task<LlmResponse> SendMessageWithImageAsync(
            string message,
            byte[] imageData,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_currentModel.SupportsVision)
                {
                    _logger.LogWarning("Vision not supported by model {ModelName}", _currentModel.Name);
                    throw new InvalidOperationException($"Model {_currentModel.Name} does not support vision");
                }

                // In a real implementation, this would call an API with the image
                // For now, we'll simulate a response describing the image
                var response = $"This is a simulated vision response from {ProviderName} using {_currentModel.Name}. " +
                               $"I can see an image that is {imageData.Length} bytes in size. " +
                               $"Your message was: \"{message}\". " +
                               $"In a real implementation, this would be a response from an actual vision model.";

                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = LlmRole.Assistant,
                        Content = response
                    },
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = 100,
                        CompletionTokens = 50,
                        TotalTokens = 150
                    }
                };

                _logger.LogInformation("Sent message with image to {ProviderName} using {ModelName}", ProviderName, _currentModel.Name);
                return Task.FromResult(llmResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with image to {ProviderName}", ProviderName);
                throw;
            }
        }

        /// <summary>
        /// Splits a string into chunks of approximately the specified size
        /// </summary>
        /// <param name="text">The text to split</param>
        /// <param name="chunkSize">The approximate size of each chunk</param>
        /// <returns>The chunks</returns>
        private IEnumerable<string> SplitIntoChunks(string text, int chunkSize)
        {
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
            }
        }
    }
}
