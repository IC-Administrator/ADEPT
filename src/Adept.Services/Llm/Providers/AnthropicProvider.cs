using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Llm.Providers
{
    /// <summary>
    /// Provider for Anthropic LLM services
    /// </summary>
    public class AnthropicProvider : ILlmProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<AnthropicProvider> _logger;
        private string _apiKey = string.Empty;
        private LlmModel _currentModel;
        private readonly List<LlmModel> _availableModels = new();
        private bool _isInitialized;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Anthropic";

        /// <summary>
        /// Gets the name of the currently selected model
        /// </summary>
        public string ModelName => _currentModel.Id;

        /// <summary>
        /// Sends a list of messages to the LLM and gets a response
        /// </summary>
        /// <param name="messages">The messages to send</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public async Task<LlmResponse> SendMessagesAsync(
            IEnumerable<LlmMessage> messages,
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            // Convert LlmMessages to ChatMessages
            var chatMessages = messages.Select(m => new ChatMessage
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList();

            return await SendMessagesAsync(chatMessages, systemPrompt, cancellationToken);
        }

        /// <summary>
        /// Sends messages to the LLM with streaming and gets a response
        /// </summary>
        /// <param name="messages">The messages to send</param>
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
            // Convert LlmMessages to ChatMessages
            var chatMessages = messages.Select(m => new ChatMessage
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList();

            // TODO: Implement streaming
            return await SendMessagesAsync(chatMessages, systemPrompt, cancellationToken);
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
            // Convert LlmMessages to ChatMessages
            var chatMessages = messages.Select(m => new ChatMessage
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList();

            // TODO: Implement tool calls
            return await SendMessagesAsync(chatMessages, systemPrompt, cancellationToken);
        }

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
        /// Initializes a new instance of the <see cref="AnthropicProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public AnthropicProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<AnthropicProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secureStorageService = secureStorageService;
            _logger = logger;

            // Initialize available models
            _availableModels.Add(new LlmModel("claude-3-opus-20240229", "Claude 3 Opus", 200000, true, true));
            _availableModels.Add(new LlmModel("claude-3-sonnet-20240229", "Claude 3 Sonnet", 200000, true, true));
            _availableModels.Add(new LlmModel("claude-3-haiku-20240307", "Claude 3 Haiku", 200000, true, true));

            // Set default model
            _currentModel = _availableModels.First(m => m.Id == "claude-3-sonnet-20240229");
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
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("anthropic_api_key") ?? string.Empty;
                _isInitialized = true;
                _logger.LogInformation("Anthropic provider initialized");

                // Fetch available models if we have an API key
                if (HasValidApiKey)
                {
                    await FetchAvailableModelsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Anthropic provider");
            }
        }

        /// <summary>
        /// Fetches the latest available models from the Anthropic API
        /// </summary>
        /// <returns>A collection of available models</returns>
        public async Task<IEnumerable<LlmModel>> FetchAvailableModelsAsync()
        {
            if (!HasValidApiKey)
            {
                _logger.LogWarning("Cannot fetch Anthropic models: API key not set");
                return _availableModels;
            }

            try
            {
                // Anthropic doesn't have a models endpoint, so we'll return the hardcoded list
                // but we'll make sure it includes the latest models
                _availableModels.Clear();

                // Claude 3 models
                _availableModels.Add(new LlmModel("claude-3-opus-20240229", "Claude 3 Opus", 200000, true, true));
                _availableModels.Add(new LlmModel("claude-3-sonnet-20240229", "Claude 3 Sonnet", 200000, true, true));
                _availableModels.Add(new LlmModel("claude-3-haiku-20240307", "Claude 3 Haiku", 200000, true, false));

                // Claude 2 models
                _availableModels.Add(new LlmModel("claude-2.1", "Claude 2.1", 200000, true, false));
                _availableModels.Add(new LlmModel("claude-2.0", "Claude 2.0", 100000, true, false));

                // Claude Instant models
                _availableModels.Add(new LlmModel("claude-instant-1.2", "Claude Instant 1.2", 100000, true, false));

                // Set current model if not already set
                if (_currentModel == null)
                {
                    _currentModel = _availableModels.First(m => m.Id == "claude-3-sonnet-20240229");
                }

                _logger.LogInformation("Fetched {Count} Anthropic models", _availableModels.Count);
                return _availableModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Anthropic models");
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
                await _secureStorageService.StoreSecureValueAsync("anthropic_api_key", apiKey);
                _logger.LogInformation("Anthropic API key set");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Anthropic API key");
            }
        }

        /// <summary>
        /// Sets the current model
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>True if the model was set, false if the model was not found</returns>
        public Task<bool> SetModelAsync(string modelId)
        {
            var model = _availableModels.FirstOrDefault(m => m.Id == modelId);
            if (model != null)
            {
                _currentModel = model;
                _logger.LogInformation("Anthropic model set to {ModelName}", model.Name);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Anthropic model {ModelId} not found", modelId);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Sends a message to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public async Task<LlmResponse> SendMessageAsync(string message, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = message }
            };

            return await SendMessagesAsync(messages, systemPrompt, cancellationToken);
        }

        /// <summary>
        /// Sends messages to the LLM and gets a response
        /// </summary>
        /// <param name="messages">The messages to send</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public async Task<LlmResponse> SendMessagesAsync(IEnumerable<ChatMessage> messages, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            if (!HasValidApiKey)
            {
                throw new InvalidOperationException("Anthropic API key not set");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                // Prepare the request
                var requestMessages = new List<object>();

                // Convert messages to Anthropic format
                foreach (var message in messages)
                {
                    var role = message.Role.ToLower() == "user" ? "user" : "assistant";
                    requestMessages.Add(new { role = role, content = message.Content });
                }

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    system = systemPrompt,
                    temperature = 0.7,
                    max_tokens = 2000,
                    tools = new[]
                    {
                        new
                        {
                            name = "get_current_weather",
                            description = "Get the current weather in a given location",
                            input_schema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    location = new
                                    {
                                        type = "string",
                                        description = "The city and state, e.g. San Francisco, CA"
                                    },
                                    unit = new
                                    {
                                        type = "string",
                                        @enum = new[] { "celsius", "fahrenheit" },
                                        description = "The temperature unit to use"
                                    }
                                },
                                required = new[] { "location" }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Extract the response message
                var content_text = responseObject.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;

                // Extract tool calls if present
                var toolCalls = new List<LlmToolCall>();
                if (responseObject.TryGetProperty("tool_use", out var toolUseElement))
                {
                    foreach (var toolUse in toolUseElement.EnumerateArray())
                    {
                        var id = Guid.NewGuid().ToString(); // Anthropic doesn't provide IDs
                        var name = toolUse.GetProperty("name").GetString() ?? string.Empty;
                        var input = toolUse.GetProperty("input").ToString() ?? string.Empty;

                        toolCalls.Add(new LlmToolCall
                        {
                            Id = id,
                            ToolName = name,
                            Arguments = input
                        });
                    }
                }

                // Create the response
                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = LlmRole.Assistant,
                        Content = content_text
                    },
                    ToolCalls = toolCalls,
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name
                };

                _logger.LogInformation("Anthropic response received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Anthropic");
                throw;
            }
        }

        /// <summary>
        /// Sends messages to the LLM with streaming and gets a response
        /// </summary>
        /// <param name="messages">The messages to send</param>
        /// <param name="onChunk">Callback for each chunk of the response</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complete LLM response</returns>
        public async Task<LlmResponse> SendMessagesStreamingAsync(IEnumerable<ChatMessage> messages, Action<string> onChunk, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            if (!HasValidApiKey)
            {
                throw new InvalidOperationException("Anthropic API key not set");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                // Prepare the request
                var requestMessages = new List<object>();

                // Convert messages to Anthropic format
                foreach (var message in messages)
                {
                    var role = message.Role.ToLower() == "user" ? "user" : "assistant";
                    requestMessages.Add(new { role = role, content = message.Content });
                }

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    system = systemPrompt,
                    temperature = 0.7,
                    max_tokens = 2000,
                    stream = true
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                var fullContent = new StringBuilder();
                var toolCalls = new List<LlmToolCall>();

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                    {
                        continue;
                    }

                    var data = line.Substring(6);
                    if (data == "[DONE]")
                    {
                        break;
                    }

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<JsonElement>(data);
                        if (chunk.TryGetProperty("type", out var type) && type.GetString() == "content_block_delta")
                        {
                            if (chunk.TryGetProperty("delta", out var delta) &&
                                delta.TryGetProperty("text", out var text))
                            {
                                var content_text = text.GetString();
                                if (!string.IsNullOrEmpty(content_text))
                                {
                                    fullContent.Append(content_text);
                                    onChunk(content_text);
                                }
                            }
                        }
                        else if (chunk.TryGetProperty("type", out var toolType) && toolType.GetString() == "tool_use")
                        {
                            // Process tool calls (simplified for now)
                            // In a real implementation, you would need to accumulate tool call chunks
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip invalid JSON
                    }
                }

                // Create the response
                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = LlmRole.Assistant,
                        Content = fullContent.ToString()
                    },
                    ToolCalls = toolCalls,
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name
                };

                _logger.LogInformation("Anthropic streaming response completed");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming message to Anthropic");
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
        public async Task<LlmResponse> SendMessageWithImageAsync(string message, byte[] imageData, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            if (!HasValidApiKey)
            {
                throw new InvalidOperationException("Anthropic API key not set");
            }

            if (!_currentModel.SupportsVision)
            {
                throw new InvalidOperationException($"Model {_currentModel.Name} does not support vision");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                // Convert image to base64
                var base64Image = Convert.ToBase64String(imageData);
                var mediaType = "image/jpeg"; // Assuming JPEG format

                // Prepare the request
                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = message },
                                new
                                {
                                    type = "image",
                                    source = new
                                    {
                                        type = "base64",
                                        media_type = mediaType,
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    system = systemPrompt,
                    temperature = 0.7,
                    max_tokens = 2000
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Extract the response message
                var content_text = responseObject.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;

                // Create the response
                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = LlmRole.Assistant,
                        Content = content_text
                    },
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name
                };

                _logger.LogInformation("Anthropic vision response received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with image to Anthropic");
                throw;
            }
        }
    }
}
