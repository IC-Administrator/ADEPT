using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Llm.Providers
{
    /// <summary>
    /// Provider for OpenRouter LLM services
    /// </summary>
    public class OpenRouterProvider : ILlmProvider
    {
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<OpenRouterProvider> _logger;
        private string _apiKey = string.Empty;
        private bool _isInitialized;
        private LlmModel _currentModel;
        private readonly List<LlmModel> _availableModels = new();

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "OpenRouter";

        /// <summary>
        /// Gets the name of the currently selected model
        /// </summary>
        public string ModelName => _currentModel?.Id ?? "Unknown";

        /// <summary>
        /// Gets the available models for this provider
        /// </summary>
        public IEnumerable<LlmModel> AvailableModels => _availableModels;

        /// <summary>
        /// Gets the currently selected model
        /// </summary>
        public LlmModel CurrentModel => _currentModel;

        /// <summary>
        /// Gets whether the provider requires an API key
        /// </summary>
        public bool RequiresApiKey => true;

        /// <summary>
        /// Gets whether the provider has a valid API key
        /// </summary>
        public bool HasValidApiKey => !string.IsNullOrEmpty(_apiKey);

        /// <summary>
        /// Gets whether the provider supports streaming
        /// </summary>
        public bool SupportsStreaming => true;

        /// <summary>
        /// Gets whether the provider supports tool calls
        /// </summary>
        public bool SupportsToolCalls => true;

        /// <summary>
        /// Gets whether the provider supports vision
        /// </summary>
        public bool SupportsVision => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenRouterProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public OpenRouterProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<OpenRouterProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secureStorageService = secureStorageService;
            _logger = logger;

            // Initialize default models
            _availableModels.Add(new LlmModel("anthropic/claude-3-opus", "Claude 3 Opus", 200000, true, true));
            _availableModels.Add(new LlmModel("anthropic/claude-3-sonnet", "Claude 3 Sonnet", 200000, true, true));
            _availableModels.Add(new LlmModel("anthropic/claude-3-haiku", "Claude 3 Haiku", 200000, true, true));
            _availableModels.Add(new LlmModel("openai/gpt-4o", "GPT-4o", 128000, true, true));
            _availableModels.Add(new LlmModel("google/gemini-1.5-pro", "Gemini 1.5 Pro", 128000, true, true));
            _availableModels.Add(new LlmModel("meta-llama/llama-3-70b-instruct", "Llama 3 70B", 128000, true, false));

            // Set default model
            _currentModel = _availableModels.First();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenRouterProvider"/> class
        /// </summary>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public OpenRouterProvider(
            ISecureStorageService secureStorageService,
            ILogger<OpenRouterProvider> logger)
        {
            _secureStorageService = secureStorageService;
            _logger = logger;

            // Initialize default models
            _availableModels.Add(new LlmModel("anthropic/claude-3-opus", "Claude 3 Opus", 200000, true, true));
            _availableModels.Add(new LlmModel("anthropic/claude-3-sonnet", "Claude 3 Sonnet", 200000, true, true));
            _availableModels.Add(new LlmModel("anthropic/claude-3-haiku", "Claude 3 Haiku", 200000, true, true));
            _availableModels.Add(new LlmModel("openai/gpt-4o", "GPT-4o", 128000, true, true));
            _availableModels.Add(new LlmModel("google/gemini-1.5-pro", "Gemini 1.5 Pro", 128000, true, true));
            _availableModels.Add(new LlmModel("meta-llama/llama-3-70b-instruct", "Llama 3 70B", 128000, true, false));

            // Set default model
            _currentModel = _availableModels.First();
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                // Get the API key from secure storage
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("openrouter_api_key") ?? string.Empty;
                _isInitialized = true;
                _logger.LogInformation("OpenRouter provider initialized");

                // Fetch available models if we have an API key
                if (HasValidApiKey)
                {
                    await FetchAvailableModelsAsync();
                }
                else
                {
                    _logger.LogWarning("OpenRouter API key not found in secure storage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing OpenRouter provider");
                throw;
            }
        }

        /// <summary>
        /// Fetches the latest available models from the provider's API
        /// </summary>
        /// <returns>A collection of available models</returns>
        public async Task<IEnumerable<LlmModel>> FetchAvailableModelsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching available OpenRouter models");

                // If we don't have an HTTP client factory, return the default models
                if (_httpClientFactory == null)
                {
                    _logger.LogWarning("HTTP client factory not available, using default models");
                    return _availableModels;
                }

                // If we don't have a valid API key, return the default models
                if (!HasValidApiKey)
                {
                    _logger.LogWarning("API key not set, using default models");
                    return _availableModels;
                }

                // TODO: Implement actual API call to fetch models
                // For now, we'll use the default models

                _logger.LogInformation("Fetched {Count} OpenRouter models", _availableModels.Count);
                return _availableModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OpenRouter models");
                return _availableModels;
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
                _apiKey = apiKey;
                await _secureStorageService.StoreSecureValueAsync("openrouter_api_key", apiKey);
                _logger.LogInformation("OpenRouter API key set");

                // Refresh models with the new API key
                if (_isInitialized && !string.IsNullOrEmpty(apiKey))
                {
                    await FetchAvailableModelsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting OpenRouter API key");
                throw;
            }
        }

        /// <summary>
        /// Sets the current model
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>True if the model was set, false if the model was not found</returns>
        public async Task<bool> SetModelAsync(string modelId)
        {
            try
            {
                var model = _availableModels.FirstOrDefault(m => m.Id == modelId);
                if (model != null)
                {
                    _currentModel = model;
                    _logger.LogInformation("Set OpenRouter model to {ModelName}", model.Name);
                    return true;
                }

                _logger.LogWarning("Model {ModelId} not found", modelId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting OpenRouter model");
                return false;
            }
        }

        /// <summary>
        /// Sends a message to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public async Task<LlmResponse> SendMessageAsync(
            string message,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<LlmMessage>
            {
                new LlmMessage { Role = LlmRole.User, Content = message }
            };

            return await SendMessagesAsync(messages, systemPrompt, cancellationToken);
        }

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public async Task<LlmResponse> SendMessagesAsync(
            IEnumerable<LlmMessage> messages,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Provider not initialized");
            }

            if (!HasValidApiKey)
            {
                throw new InvalidOperationException($"{ProviderName} API key not set");
            }

            if (_httpClientFactory == null)
            {
                throw new InvalidOperationException("HTTP client factory not available");
            }

            try
            {
                _logger.LogInformation("Sending message to OpenRouter using {ModelName}", _currentModel.Name);

                // TODO: Implement actual API call to OpenRouter
                // For now, return a simulated response

                var lastUserMessage = messages.LastOrDefault(m => m.Role == LlmRole.User)?.Content ?? "No user message";
                var response = $"This is a simulated response from {ProviderName} using {_currentModel.Name}. " +
                               $"You said: \"{lastUserMessage}\". " +
                               $"In a real implementation, this would be a response from the OpenRouter API.";

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

                _logger.LogInformation("Received response from OpenRouter");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to OpenRouter");
                throw;
            }
        }

        // Legacy methods - will be removed in future versions
        public async Task<LlmResponse> GetCompletionAsync(object request)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Provider not initialized");
            }

            // This method is deprecated and will be removed
            throw new NotImplementedException("This method is deprecated. Use SendMessageAsync instead.");
        }

        public async Task<Stream> GetStreamingCompletionAsync(object request)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Provider not initialized");
            }

            // This method is deprecated and will be removed
            throw new NotImplementedException("This method is deprecated. Use SendMessagesStreamingAsync instead.");
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
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Provider not initialized");
            }

            if (!HasValidApiKey)
            {
                throw new InvalidOperationException($"{ProviderName} API key not set");
            }

            try
            {
                // For now, we'll use the non-streaming version and simulate streaming
                // This should be replaced with actual streaming implementation
                var fullResponse = await SendMessagesAsync(messages, systemPrompt, cancellationToken);

                // Simulate streaming by sending the response in chunks
                if (onPartialResponse != null)
                {
                    var content = fullResponse.Message.Content;
                    var chunkSize = Math.Max(10, content.Length / 5); // Split into ~5 chunks

                    for (int i = 0; i < content.Length; i += chunkSize)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var chunk = content.Substring(i, Math.Min(chunkSize, content.Length - i));
                        onPartialResponse(chunk);
                        await Task.Delay(100, cancellationToken); // Simulate network delay
                    }
                }

                return fullResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming message to OpenRouter");
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
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Provider not initialized");
            }

            if (!HasValidApiKey)
            {
                throw new InvalidOperationException($"{ProviderName} API key not set");
            }

            if (!_currentModel.SupportsToolCalls)
            {
                _logger.LogWarning("Tool calls not supported by model {ModelName}, falling back to non-tool", _currentModel.Name);
                return await SendMessagesAsync(messages, systemPrompt, cancellationToken);
            }

            try
            {
                _logger.LogInformation("Sending message with tools to OpenRouter using {ModelName}", _currentModel.Name);

                // TODO: Implement actual API call to OpenRouter with tools
                // For now, return a simulated response with a tool call

                var lastUserMessage = messages.LastOrDefault(m => m.Role == LlmRole.User)?.Content ?? "No user message";
                var response = $"I'll help you with that request about \"{lastUserMessage}\".";

                // Create a simulated tool call
                var toolCalls = new List<LlmToolCall>
                {
                    new LlmToolCall
                    {
                        Id = Guid.NewGuid().ToString(),
                        ToolName = "get_information",
                        Arguments = "{\"query\": \"" + lastUserMessage.Replace("\"", "\\\"") + "\"}"
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

                _logger.LogInformation("Received response with tools from OpenRouter");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with tools to OpenRouter");
                throw;
            }
        }

        /// <summary>
        /// Sends a message with tool definitions to the LLM and gets a streaming response with tool calls
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="tools">The tool definitions</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="onPartialResponse">Callback for partial responses</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complete LLM response with tool calls</returns>
        public async Task<LlmResponse> SendMessagesWithToolsStreamingAsync(
            IEnumerable<LlmMessage> messages,
            IEnumerable<LlmTool> tools,
            string? systemPrompt = null,
            Action<string>? onPartialResponse = null,
            CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Provider not initialized");
            }

            if (!HasValidApiKey)
            {
                throw new InvalidOperationException($"{ProviderName} API key not set");
            }

            if (!_currentModel.SupportsToolCalls)
            {
                _logger.LogWarning("Tool calls not supported by model {ModelName}, falling back to non-tool streaming", _currentModel.Name);
                return await SendMessagesStreamingAsync(messages, systemPrompt, onPartialResponse, cancellationToken);
            }

            try
            {
                // For now, we'll use the non-streaming version and simulate streaming
                // This should be replaced with actual streaming implementation for each provider
                var fullResponse = await SendMessagesWithToolsAsync(messages, tools, systemPrompt, cancellationToken);

                // Simulate streaming by sending the response in chunks
                if (onPartialResponse != null)
                {
                    var content = fullResponse.Message.Content;
                    var chunkSize = Math.Max(10, content.Length / 5); // Split into ~5 chunks

                    for (int i = 0; i < content.Length; i += chunkSize)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var chunk = content.Substring(i, Math.Min(chunkSize, content.Length - i));
                        onPartialResponse(chunk);
                        await Task.Delay(100, cancellationToken); // Simulate network delay
                    }
                }

                return fullResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming message with tools to OpenRouter");
                throw;
            }
        }
    }
}