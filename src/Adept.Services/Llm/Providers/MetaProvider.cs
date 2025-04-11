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
    /// Provider for Meta LLM services (Llama)
    /// </summary>
    public class MetaProvider : ILlmProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<MetaProvider> _logger;
        private string _apiKey = string.Empty;
        private LlmModel _currentModel;
        private readonly List<LlmModel> _availableModels = new();
        private bool _isInitialized;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Meta";

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
        /// Initializes a new instance of the <see cref="MetaProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public MetaProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<MetaProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secureStorageService = secureStorageService;
            _logger = logger;

            // Initialize available models
            // Llama 3.1 models (latest generation)
            _availableModels.Add(new LlmModel("llama-3.1-405b-instruct", "Llama 3.1 405B Instruct", 128000, true, true));
            _availableModels.Add(new LlmModel("llama-3.1-70b-instruct", "Llama 3.1 70B Instruct", 128000, true, true));
            _availableModels.Add(new LlmModel("llama-3.1-8b-instruct", "Llama 3.1 8B Instruct", 128000, true, false));
            _availableModels.Add(new LlmModel("llama-3.1-70b-vision", "Llama 3.1 70B Vision", 128000, true, true));

            // Llama 3 models
            _availableModels.Add(new LlmModel("llama-3-70b-instruct", "Llama 3 70B Instruct", 128000, true, false));
            _availableModels.Add(new LlmModel("llama-3-8b-instruct", "Llama 3 8B Instruct", 128000, true, false));

            // Llama 2 models (previous generation)
            _availableModels.Add(new LlmModel("llama-2-70b-chat", "Llama 2 70B Chat", 4096, false, false));
            _availableModels.Add(new LlmModel("llama-2-13b-chat", "Llama 2 13B Chat", 4096, false, false));
            _availableModels.Add(new LlmModel("llama-2-7b-chat", "Llama 2 7B Chat", 4096, false, false));

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
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("meta_api_key") ?? string.Empty;
                _isInitialized = true;
                _logger.LogInformation("Meta provider initialized");

                // Fetch available models if we have an API key
                if (HasValidApiKey)
                {
                    await FetchAvailableModelsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Meta provider");
            }
        }

        /// <summary>
        /// Fetches the latest available models from the Meta API
        /// </summary>
        /// <returns>A collection of available models</returns>
        public async Task<IEnumerable<LlmModel>> FetchAvailableModelsAsync()
        {
            if (!HasValidApiKey)
            {
                _logger.LogWarning("Cannot fetch Meta models: API key not set");
                return _availableModels;
            }

            try
            {
                // Meta doesn't have a public models endpoint, so we'll return the hardcoded list
                // but we'll make sure it includes the latest models
                _availableModels.Clear();

                // Llama 3.1 models
                _availableModels.Add(new LlmModel("llama-3.1-405b-instruct", "Llama 3.1 405B Instruct", 128000, true, true));
                _availableModels.Add(new LlmModel("llama-3.1-70b-instruct", "Llama 3.1 70B Instruct", 128000, true, true));
                _availableModels.Add(new LlmModel("llama-3.1-8b-instruct", "Llama 3.1 8B Instruct", 128000, true, false));
                _availableModels.Add(new LlmModel("llama-3.1-70b-vision", "Llama 3.1 70B Vision", 128000, true, true));

                // Llama 3 models
                _availableModels.Add(new LlmModel("llama-3-70b-instruct", "Llama 3 70B Instruct", 128000, true, false));
                _availableModels.Add(new LlmModel("llama-3-8b-instruct", "Llama 3 8B Instruct", 128000, true, false));

                // Llama 2 models
                _availableModels.Add(new LlmModel("llama-2-70b-chat", "Llama 2 70B Chat", 4096, false, false));
                _availableModels.Add(new LlmModel("llama-2-13b-chat", "Llama 2 13B Chat", 4096, false, false));
                _availableModels.Add(new LlmModel("llama-2-7b-chat", "Llama 2 7B Chat", 4096, false, false));

                // Set current model if not already set
                if (_currentModel == null)
                {
                    _currentModel = _availableModels.First();
                }

                _logger.LogInformation("Fetched {Count} Meta models", _availableModels.Count);
                return _availableModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Meta models");
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
                await _secureStorageService.StoreSecureValueAsync("meta_api_key", apiKey);
                _logger.LogInformation("Meta API key set");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Meta API key");
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
                _logger.LogInformation("Meta model set to {ModelName}", model.Name);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Meta model {ModelId} not found", modelId);
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
                throw new InvalidOperationException("Meta API key not set");
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
                    var messageRole = message.Role == LlmRole.User ? "user" : "assistant";
                    requestMessages.Add(new { role = messageRole, content = message.Content });
                }

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    temperature = 0.7,
                    max_tokens = 2000,
                    top_p = 1.0
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://llama.meta.ai/v1/chat/completions", content, cancellationToken);
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
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = responseObject.GetProperty("usage").GetProperty("prompt_tokens").GetInt32(),
                        CompletionTokens = responseObject.GetProperty("usage").GetProperty("completion_tokens").GetInt32(),
                        TotalTokens = responseObject.GetProperty("usage").GetProperty("total_tokens").GetInt32()
                    }
                };

                _logger.LogInformation("Meta response received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Meta");
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
                throw new InvalidOperationException("Meta API key not set");
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
                    var role = message.Role == LlmRole.User ? "user" : "assistant";
                    requestMessages.Add(new { role = role, content = message.Content });
                }

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    temperature = 0.7,
                    max_tokens = 2000,
                    top_p = 1.0,
                    stream = true
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://llama.meta.ai/v1/chat/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                var fullContent = new StringBuilder();
                var promptTokens = 0;
                var completionTokens = 0;

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
                            }
                        }

                        // Try to get usage information
                        if (chunk.TryGetProperty("usage", out var usage))
                        {
                            if (usage.TryGetProperty("prompt_tokens", out var pt))
                            {
                                promptTokens = pt.GetInt32();
                            }
                            if (usage.TryGetProperty("completion_tokens", out var ct))
                            {
                                completionTokens = ct.GetInt32();
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
                        PromptTokens = promptTokens,
                        CompletionTokens = completionTokens,
                        TotalTokens = promptTokens + completionTokens
                    }
                };

                _logger.LogInformation("Meta streaming response completed");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming message to Meta");
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
                throw new InvalidOperationException("Meta API key not set");
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
                var mimeType = "image/jpeg"; // Assuming JPEG format, adjust if needed

                // Prepare the request with multimodal content
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
                            image_url = new
                            {
                                url = $"data:{mimeType};base64,{base64Image}"
                            }
                        }
                    }
                });

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    temperature = 0.7,
                    max_tokens = 2000,
                    top_p = 1.0
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://llama.meta.ai/v1/chat/completions", content, cancellationToken);
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
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = responseObject.GetProperty("usage").GetProperty("prompt_tokens").GetInt32(),
                        CompletionTokens = responseObject.GetProperty("usage").GetProperty("completion_tokens").GetInt32(),
                        TotalTokens = responseObject.GetProperty("usage").GetProperty("total_tokens").GetInt32()
                    }
                };

                _logger.LogInformation("Meta vision response received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with image to Meta");
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
                throw new InvalidOperationException("Meta API key not set");
            }

            if (!_currentModel.SupportsToolCalls)
            {
                throw new InvalidOperationException($"Model {_currentModel.Name} does not support tool calls");
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
                    var messageRole = message.Role == LlmRole.User ? "user" : "assistant";
                    requestMessages.Add(new { role = messageRole, content = message.Content });
                }

                // Convert tools to the format expected by the API
                var toolsFormatted = tools.Select(t => new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Function.Name,
                        description = t.Function.Description,
                        parameters = t.Function.Parameters
                    }
                }).ToArray();

                var requestBody = new
                {
                    model = _currentModel.Id,
                    messages = requestMessages,
                    temperature = 0.7,
                    max_tokens = 2000,
                    top_p = 1.0,
                    tools = toolsFormatted
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://llama.meta.ai/v1/chat/completions", content, cancellationToken);
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
                    ModelName = _currentModel.Name,
                    Usage = new LlmUsage
                    {
                        PromptTokens = responseObject.GetProperty("usage").GetProperty("prompt_tokens").GetInt32(),
                        CompletionTokens = responseObject.GetProperty("usage").GetProperty("completion_tokens").GetInt32(),
                        TotalTokens = responseObject.GetProperty("usage").GetProperty("total_tokens").GetInt32()
                    }
                };

                _logger.LogInformation("Meta response with tools received");
                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with tools to Meta");
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
                _logger.LogError(ex, "Error sending streaming message with tools to {ProviderName}", ProviderName);
                throw;
            }
        }
    }
}
