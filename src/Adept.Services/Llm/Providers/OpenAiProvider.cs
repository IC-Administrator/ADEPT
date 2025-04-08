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
    /// Provider for OpenAI LLM services
    /// </summary>
    public class OpenAiProvider : ILlmProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<OpenAiProvider> _logger;
        private string _apiKey = string.Empty;
        private LlmModel _currentModel;
        private readonly List<LlmModel> _availableModels = new();
        private bool _isInitialized;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "OpenAI";

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
        /// Initializes a new instance of the <see cref="OpenAiProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public OpenAiProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<OpenAiProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secureStorageService = secureStorageService;
            _logger = logger;

            // Initialize available models
            _availableModels.Add(new LlmModel("gpt-4o", "GPT-4o", 128000, true, true));
            _availableModels.Add(new LlmModel("gpt-4-turbo", "GPT-4 Turbo", 128000, true, false));
            _availableModels.Add(new LlmModel("gpt-3.5-turbo", "GPT-3.5 Turbo", 16000, true, false));

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
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("openai_api_key") ?? string.Empty;
                _isInitialized = true;
                _logger.LogInformation("OpenAI provider initialized");

                // Fetch available models if we have an API key
                if (HasValidApiKey)
                {
                    await FetchAvailableModelsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing OpenAI provider");
            }
        }

        /// <summary>
        /// Fetches the latest available models from the OpenAI API
        /// </summary>
        /// <returns>A collection of available models</returns>
        public async Task<IEnumerable<LlmModel>> FetchAvailableModelsAsync()
        {
            if (!HasValidApiKey)
            {
                _logger.LogWarning("Cannot fetch OpenAI models: API key not set");
                return _availableModels;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await client.GetAsync("https://api.openai.com/v1/models");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var modelsResponse = JsonSerializer.Deserialize<OpenAiModelsResponse>(content);

                if (modelsResponse?.Data == null)
                {
                    _logger.LogWarning("Failed to parse OpenAI models response");
                    return _availableModels;
                }

                // Filter for chat models only
                var chatModels = modelsResponse.Data
                    .Where(m => m.Id.StartsWith("gpt-"))
                    .Where(m => !m.Id.Contains("instruct") && !m.Id.Contains("vision") && !m.Id.Contains("preview"))
                    .ToList();

                // Clear existing models and add the fetched ones
                _availableModels.Clear();

                // Add GPT-4o models
                var gpt4oModels = chatModels.Where(m => m.Id.StartsWith("gpt-4o")).ToList();
                foreach (var model in gpt4oModels)
                {
                    _availableModels.Add(new LlmModel(model.Id, model.Id, 128000, true, true));
                }

                // Add GPT-4 models
                var gpt4Models = chatModels.Where(m => m.Id.StartsWith("gpt-4") && !m.Id.StartsWith("gpt-4o")).ToList();
                foreach (var model in gpt4Models)
                {
                    _availableModels.Add(new LlmModel(model.Id, model.Id, 128000, true, true));
                }

                // Add GPT-3.5 models
                var gpt35Models = chatModels.Where(m => m.Id.StartsWith("gpt-3.5")).ToList();
                foreach (var model in gpt35Models)
                {
                    _availableModels.Add(new LlmModel(model.Id, model.Id, 16000, true, false));
                }

                // If no models were found, add default models
                if (_availableModels.Count == 0)
                {
                    _availableModels.Add(new LlmModel("gpt-4o", "GPT-4o", 128000, true, true));
                    _availableModels.Add(new LlmModel("gpt-4-turbo", "GPT-4 Turbo", 128000, true, false));
                    _availableModels.Add(new LlmModel("gpt-3.5-turbo", "GPT-3.5 Turbo", 16000, true, false));
                }

                // Set current model if not already set
                if (_currentModel == null)
                {
                    _currentModel = _availableModels.First();
                }

                _logger.LogInformation("Fetched {Count} OpenAI models", _availableModels.Count);
                return _availableModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OpenAI models");
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
                await _secureStorageService.StoreSecureValueAsync("openai_api_key", apiKey);
                _logger.LogInformation("OpenAI API key set");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting OpenAI API key");
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
                _logger.LogInformation("OpenAI model set to {ModelName}", model.Name);
                return Task.FromResult(true);
            }

            _logger.LogWarning("OpenAI model {ModelId} not found", modelId);
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
        private async Task<LlmResponse> SendMessagesAsync(IEnumerable<ChatMessage> messages, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            if (!HasValidApiKey)
            {
                throw new InvalidOperationException("OpenAI API key not set");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                // Prepare the request
                var requestMessages = new List<object>();

                // Add system prompt if provided
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    requestMessages.Add(new { role = "system", content = systemPrompt });
                }

                // Add conversation history
                foreach (var message in messages)
                {
                    requestMessages.Add(new { role = message.Role, content = message.Content });
                }

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    temperature = 0.7,
                    max_tokens = 2000,
                    top_p = 1.0,
                    frequency_penalty = 0.0,
                    presence_penalty = 0.0,
                    tools = new[]
                    {
                        new
                        {
                            type = "function",
                            function = new
                            {
                                name = "get_current_weather",
                                description = "Get the current weather in a given location",
                                parameters = new
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
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Extract the response message
                var choices = responseObject.GetProperty("choices");
                var firstChoice = choices[0];
                var messageObj = firstChoice.GetProperty("message");
                var role = messageObj.GetProperty("role").GetString() ?? "assistant";
                var content_text = messageObj.TryGetProperty("content", out var contentElement)
                    ? contentElement.GetString() ?? string.Empty
                    : string.Empty;

                // Extract tool calls if present
                var toolCalls = new List<LlmToolCall>();
                if (messageObj.TryGetProperty("tool_calls", out var toolCallsElement))
                {
                    foreach (var toolCall in toolCallsElement.EnumerateArray())
                    {
                        var id = toolCall.GetProperty("id").GetString() ?? string.Empty;
                        var type = toolCall.GetProperty("type").GetString() ?? string.Empty;

                        if (type == "function")
                        {
                            var function = toolCall.GetProperty("function");
                            var name = function.GetProperty("name").GetString() ?? string.Empty;
                            var arguments = function.GetProperty("arguments").GetString() ?? string.Empty;

                            toolCalls.Add(new LlmToolCall
                            {
                                Id = id,
                                ToolName = name,
                                Arguments = arguments
                            });
                        }
                    }
                }

                // Create the response
                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = role == "assistant" ? LlmRole.Assistant : LlmRole.User,
                        Content = content_text
                    },
                    ToolCalls = toolCalls,
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name
                };

                _logger.LogInformation("OpenAI response received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to OpenAI");
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
                throw new InvalidOperationException("OpenAI API key not set");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                // Prepare the request
                var requestMessages = new List<object>();

                // Add system prompt if provided
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    requestMessages.Add(new { role = "system", content = systemPrompt });
                }

                // Add conversation history
                foreach (var message in messages)
                {
                    requestMessages.Add(new { role = message.Role, content = message.Content });
                }

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    temperature = 0.7,
                    max_tokens = 2000,
                    top_p = 1.0,
                    frequency_penalty = 0.0,
                    presence_penalty = 0.0,
                    stream = true
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
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
                        if (chunk.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            if (choice.TryGetProperty("delta", out var delta))
                            {
                                if (delta.TryGetProperty("content", out var content_delta))
                                {
                                    var content_text = content_delta.GetString();
                                    if (!string.IsNullOrEmpty(content_text))
                                    {
                                        fullContent.Append(content_text);
                                        onChunk(content_text);
                                    }
                                }

                                // Check for tool calls
                                if (delta.TryGetProperty("tool_calls", out var toolCallsDelta))
                                {
                                    // Process tool calls (simplified for now)
                                    // In a real implementation, you would need to accumulate tool call chunks
                                }
                            }
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

                _logger.LogInformation("OpenAI streaming response completed");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming message to OpenAI");
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
                throw new InvalidOperationException("OpenAI API key not set");
            }

            if (!_currentModel.SupportsVision)
            {
                throw new InvalidOperationException($"Model {_currentModel.Name} does not support vision");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                // Convert image to base64
                var base64Image = Convert.ToBase64String(imageData);
                var imageUrl = $"data:image/jpeg;base64,{base64Image}";

                // Prepare the request
                var requestMessages = new List<object>();

                // Add system prompt if provided
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    requestMessages.Add(new { role = "system", content = systemPrompt });
                }

                // Add user message with image
                requestMessages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = message },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = imageUrl }
                        }
                    }
                });

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    temperature = 0.7,
                    max_tokens = 2000,
                    top_p = 1.0,
                    frequency_penalty = 0.0,
                    presence_penalty = 0.0
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Extract the response message
                var choices = responseObject.GetProperty("choices");
                var firstChoice = choices[0];
                var messageObj = firstChoice.GetProperty("message");
                var role = messageObj.GetProperty("role").GetString() ?? "assistant";
                var content_text = messageObj.GetProperty("content").GetString() ?? string.Empty;

                // Create the response
                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = role == "assistant" ? LlmRole.Assistant : LlmRole.User,
                        Content = content_text
                    },
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name
                };

                _logger.LogInformation("OpenAI vision response received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with image to OpenAI");
                throw;
            }
        }
    }

    /// <summary>
    /// Response from the OpenAI models API
    /// </summary>
    internal class OpenAiModelsResponse
    {
        /// <summary>
        /// Gets or sets the list of models
        /// </summary>
        public List<OpenAiModel> Data { get; set; } = new();
    }

    /// <summary>
    /// OpenAI model information
    /// </summary>
    internal class OpenAiModel
    {
        /// <summary>
        /// Gets or sets the model ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model object type
        /// </summary>
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model creation timestamp
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model owner
        /// </summary>
        public string OwnedBy { get; set; } = string.Empty;
    }
}
