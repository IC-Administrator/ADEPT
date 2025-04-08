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
    /// Provider for Google LLM services (Gemini)
    /// </summary>
    public class GoogleProvider : ILlmProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<GoogleProvider> _logger;
        private string _apiKey = string.Empty;
        private LlmModel _currentModel;
        private readonly List<LlmModel> _availableModels = new();
        private bool _isInitialized;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Google";

        /// <summary>
        /// Gets the name of the currently selected model
        /// </summary>
        public string ModelName => _currentModel.Id;



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
        public bool SupportsToolCalls => _currentModel.SupportsToolCalls;

        /// <summary>
        /// Gets whether the provider supports vision
        /// </summary>
        public bool SupportsVision => _currentModel.SupportsVision;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public GoogleProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<GoogleProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secureStorageService = secureStorageService;
            _logger = logger;

            // Initialize available models
            _availableModels.Add(new LlmModel("gemini-1.5-pro", "Gemini 1.5 Pro", 1000000, true, true));
            _availableModels.Add(new LlmModel("gemini-1.5-flash", "Gemini 1.5 Flash", 1000000, true, true));
            _availableModels.Add(new LlmModel("gemini-1.0-pro", "Gemini 1.0 Pro", 32000, true, true));

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
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("google_api_key") ?? string.Empty;
                _isInitialized = true;
                _logger.LogInformation("Google provider initialized");

                // Fetch available models if we have an API key
                if (HasValidApiKey)
                {
                    await FetchAvailableModelsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Google provider");
            }
        }

        /// <summary>
        /// Fetches the latest available models from the Google API
        /// </summary>
        /// <returns>A collection of available models</returns>
        public async Task<IEnumerable<LlmModel>> FetchAvailableModelsAsync()
        {
            if (!HasValidApiKey)
            {
                _logger.LogWarning("Cannot fetch Google models: API key not set");
                return _availableModels;
            }

            try
            {
                // Google doesn't have a public models endpoint, so we'll return the hardcoded list
                // but we'll make sure it includes the latest models
                _availableModels.Clear();

                // Gemini models
                _availableModels.Add(new LlmModel("gemini-1.5-pro", "Gemini 1.5 Pro", 1000000, true, true));
                _availableModels.Add(new LlmModel("gemini-1.5-flash", "Gemini 1.5 Flash", 1000000, true, false));
                _availableModels.Add(new LlmModel("gemini-1.0-pro", "Gemini 1.0 Pro", 32000, true, false));
                _availableModels.Add(new LlmModel("gemini-1.0-pro-vision", "Gemini 1.0 Pro Vision", 32000, true, true));
                _availableModels.Add(new LlmModel("gemini-ultra", "Gemini Ultra", 32000, true, true));

                // Set current model if not already set
                if (_currentModel == null)
                {
                    _currentModel = _availableModels.First(m => m.Id == "gemini-1.5-pro");
                }

                _logger.LogInformation("Fetched {Count} Google models", _availableModels.Count);
                return _availableModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Google models");
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
                await _secureStorageService.StoreSecureValueAsync("google_api_key", apiKey);
                _logger.LogInformation("Google API key set");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Google API key");
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
                _logger.LogInformation("Google model set to {ModelName}", model.Name);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Google model {ModelId} not found", modelId);
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
            var messages = new List<LlmMessage>
            {
                new LlmMessage { Role = LlmRole.User, Content = message }
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
        public async Task<LlmResponse> SendMessagesAsync(IEnumerable<LlmMessage> messages, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            if (!HasValidApiKey)
            {
                throw new InvalidOperationException("Google API key not set");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Prepare the request
                var requestMessages = new List<object>();

                // Convert messages to Google format
                foreach (var message in messages)
                {
                    var role = message.Role == LlmRole.User ? "user" : "model";
                    requestMessages.Add(new { role = role, parts = new[] { new { text = message.Content } } });
                }

                // Add system prompt if provided
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    // For Gemini, system prompts are added as a special user message at the beginning
                    requestMessages.Insert(0, new { role = "user", parts = new[] { new { text = systemPrompt } } });
                }

                var requestBody = new
                {
                    contents = requestMessages,
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 2000,
                        topP = 1.0
                    }
                };

                var modelEndpoint = _currentModel.Id.Replace(".", "-");
                var url = $"https://generativelanguage.googleapis.com/v1/models/{modelEndpoint}:generateContent?key={_apiKey}";

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Extract the response message
                var candidates = responseObject.GetProperty("candidates");
                var firstCandidate = candidates[0];
                var content_obj = firstCandidate.GetProperty("content");
                var parts = content_obj.GetProperty("parts");
                var firstPart = parts[0];
                var content_text = firstPart.GetProperty("text").GetString() ?? string.Empty;

                // Create the response
                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = LlmRole.Assistant,
                        Content = content_text
                    },
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = 0, // Google doesn't provide token counts
                        CompletionTokens = 0,
                        TotalTokens = 0
                    }
                };

                _logger.LogInformation("Google response received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Google");
                throw;
            }
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
            return await SendMessagesStreamingAsync(messages, onPartialResponse ?? (_ => {}), systemPrompt, cancellationToken);
        }

        /// <summary>
        /// Sends messages to the LLM with streaming and gets a response
        /// </summary>
        /// <param name="messages">The messages to send</param>
        /// <param name="onChunk">Callback for each chunk of the response</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complete LLM response</returns>
        private async Task<LlmResponse> SendMessagesStreamingAsync(IEnumerable<LlmMessage> messages, Action<string> onChunk, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            if (!HasValidApiKey)
            {
                throw new InvalidOperationException("Google API key not set");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Prepare the request
                var requestMessages = new List<object>();

                // Convert messages to Google format
                foreach (var message in messages)
                {
                    var role = message.Role == LlmRole.User ? "user" : "model";
                    requestMessages.Add(new { role = role, parts = new[] { new { text = message.Content } } });
                }

                // Add system prompt if provided
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    // For Gemini, system prompts are added as a special user message at the beginning
                    requestMessages.Insert(0, new { role = "user", parts = new[] { new { text = systemPrompt } } });
                }

                var requestBody = new
                {
                    contents = requestMessages,
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 2000,
                        topP = 1.0
                    },
                    streamGenerationConfig = new
                    {
                        streamContentTokens = true
                    }
                };

                var modelEndpoint = _currentModel.Id.Replace(".", "-");
                var url = $"https://generativelanguage.googleapis.com/v1/models/{modelEndpoint}:streamGenerateContent?key={_apiKey}";

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                var fullContent = new StringBuilder();

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
                        if (chunk.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                        {
                            var candidate = candidates[0];
                            if (candidate.TryGetProperty("content", out var content_obj) &&
                                content_obj.TryGetProperty("parts", out var parts) &&
                                parts.GetArrayLength() > 0)
                            {
                                var part = parts[0];
                                if (part.TryGetProperty("text", out var text))
                                {
                                    var content_text = text.GetString();
                                    if (!string.IsNullOrEmpty(content_text))
                                    {
                                        fullContent.Append(content_text);
                                        onChunk(content_text);
                                    }
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
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = 0, // Google doesn't provide token counts
                        CompletionTokens = 0,
                        TotalTokens = 0
                    }
                };

                _logger.LogInformation("Google streaming response completed");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming message to Google");
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
                throw new InvalidOperationException("Google API key not set");
            }

            if (!_currentModel.SupportsVision)
            {
                throw new InvalidOperationException($"Model {_currentModel.Name} does not support vision");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Convert image to base64
                var base64Image = Convert.ToBase64String(imageData);

                // Prepare the request
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new { text = message },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = "image/jpeg",
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 2000,
                        topP = 1.0
                    }
                };

                var modelEndpoint = _currentModel.Id.Replace(".", "-");
                var url = $"https://generativelanguage.googleapis.com/v1/models/{modelEndpoint}:generateContent?key={_apiKey}";

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Extract the response message
                var candidates = responseObject.GetProperty("candidates");
                var firstCandidate = candidates[0];
                var content_obj = firstCandidate.GetProperty("content");
                var parts = content_obj.GetProperty("parts");
                var firstPart = parts[0];
                var content_text = firstPart.GetProperty("text").GetString() ?? string.Empty;

                // Create the response
                var llmResponse = new LlmResponse
                {
                    Message = new LlmMessage
                    {
                        Role = LlmRole.Assistant,
                        Content = content_text
                    },
                    ProviderName = ProviderName,
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = 0, // Google doesn't provide token counts
                        CompletionTokens = 0,
                        TotalTokens = 0
                    }
                };

                _logger.LogInformation("Google vision response received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with image to Google");
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
        public async Task<LlmResponse> SendMessagesWithToolsAsync(IEnumerable<LlmMessage> messages, IEnumerable<LlmTool> tools, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            if (!HasValidApiKey)
            {
                throw new InvalidOperationException("Google API key not set");
            }

            if (!_currentModel.SupportsToolCalls)
            {
                throw new InvalidOperationException($"Model {_currentModel.Name} does not support tool calls");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Prepare the request
                var requestMessages = new List<object>();

                // Convert messages to Google format
                foreach (var message in messages)
                {
                    var role = message.Role == LlmRole.User ? "user" : "model";
                    requestMessages.Add(new { role = role, parts = new[] { new { text = message.Content } } });
                }

                // Add system prompt if provided
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    // For Gemini, system prompts are added as a special user message at the beginning
                    requestMessages.Insert(0, new { role = "user", parts = new[] { new { text = systemPrompt } } });
                }

                // Convert tools to the format expected by the API
                var toolsFormatted = tools.Select(t => new
                {
                    function_declarations = new[]
                    {
                        new
                        {
                            name = t.Function.Name,
                            description = t.Function.Description,
                            parameters = t.Function.Parameters
                        }
                    }
                }).ToArray();

                var requestBody = new
                {
                    contents = requestMessages,
                    tools = toolsFormatted,
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 2000,
                        topP = 1.0
                    }
                };

                var modelEndpoint = _currentModel.Id.Replace(".", "-");
                var url = $"https://generativelanguage.googleapis.com/v1/models/{modelEndpoint}:generateContent?key={_apiKey}";

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Extract the response message
                var candidates = responseObject.GetProperty("candidates");
                var firstCandidate = candidates[0];
                var content_obj = firstCandidate.GetProperty("content");
                var parts = content_obj.GetProperty("parts");
                var firstPart = parts[0];
                var content_text = firstPart.TryGetProperty("text", out var textElement)
                    ? textElement.GetString() ?? string.Empty
                    : string.Empty;

                // Extract tool calls if present
                var toolCalls = new List<LlmToolCall>();
                if (content_obj.TryGetProperty("parts", out var contentParts))
                {
                    foreach (var part in contentParts.EnumerateArray())
                    {
                        if (part.TryGetProperty("functionCall", out var functionCall))
                        {
                            var name = functionCall.GetProperty("name").GetString() ?? string.Empty;
                            var args = functionCall.TryGetProperty("args", out var argsElement)
                                ? JsonSerializer.Serialize(argsElement)
                                : "{}";

                            toolCalls.Add(new LlmToolCall
                            {
                                Id = Guid.NewGuid().ToString(), // Google doesn't provide IDs
                                ToolName = name,
                                Arguments = args
                            });
                        }
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
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = 0, // Google doesn't provide token counts
                        CompletionTokens = 0,
                        TotalTokens = 0
                    }
                };

                _logger.LogInformation("Google response with tools received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with tools to Google");
                throw;
            }
        }
    }
}
