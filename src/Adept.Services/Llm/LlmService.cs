using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace Adept.Services.Llm
{
    /// <summary>
    /// Service for interacting with Large Language Models
    /// </summary>
    public partial class LlmService : ILlmService
    {
        private readonly IEnumerable<ILlmProvider> _providers;
        private readonly IConversationRepository _conversationRepository;
        private readonly ISystemPromptService _systemPromptService;
        private readonly LlmToolIntegrationService _toolIntegrationService;
        private readonly ILogger<LlmService> _logger;
        private ILlmProvider? _activeProvider;
        private readonly Dictionary<string, DateTime> _providerFailures = new();
        private readonly TimeSpan _failureBackoffTime = TimeSpan.FromMinutes(5);
        private readonly SemaphoreSlim _providerLock = new(1, 1);

        // Token limits for different models (conservative estimates)
        private readonly ConcurrentDictionary<string, int> _modelTokenLimits = new()
        {
            // OpenAI models
            ["gpt-3.5-turbo"] = 4000,
            ["gpt-3.5-turbo-16k"] = 16000,
            ["gpt-4"] = 8000,
            ["gpt-4-32k"] = 32000,
            ["gpt-4-turbo"] = 128000,
            ["gpt-4o"] = 128000,

            // Anthropic models
            ["claude-instant-1"] = 100000,
            ["claude-2"] = 100000,
            ["claude-3-opus"] = 200000,
            ["claude-3-sonnet"] = 200000,
            ["claude-3-haiku"] = 200000,

            // Google models
            ["gemini-pro"] = 32000,
            ["gemini-ultra"] = 32000,

            // Meta models
            ["llama-3-8b"] = 8000,
            ["llama-3-70b"] = 8000,

            // DeepSeek models
            ["deepseek-chat"] = 16000,
            ["deepseek-coder"] = 16000,

            // Default fallback
            ["default"] = 4000
        };

        // Reserve tokens for the response (to avoid hitting the limit)
        private const int ReservedResponseTokens = 1000;

        // Maximum tokens to use for the conversation history
        private int MaxHistoryTokens => GetActiveModelTokenLimit() - ReservedResponseTokens;

        /// <summary>
        /// Gets the currently active LLM provider
        /// </summary>
        public ILlmProvider ActiveProvider => _activeProvider ?? _providers.First();

        /// <summary>
        /// Gets all available LLM providers
        /// </summary>
        public IEnumerable<ILlmProvider> AvailableProviders => _providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmService"/> class
        /// </summary>
        /// <param name="providers">The LLM providers</param>
        /// <param name="conversationRepository">The conversation repository</param>
        /// <param name="systemPromptService">The system prompt service</param>
        /// <param name="toolIntegrationService">The tool integration service</param>
        /// <param name="logger">The logger</param>
        public LlmService(
            IEnumerable<ILlmProvider> providers,
            IConversationRepository conversationRepository,
            ISystemPromptService systemPromptService,
            LlmToolIntegrationService toolIntegrationService,
            ILogger<LlmService> logger)
        {
            _providers = providers;
            _conversationRepository = conversationRepository;
            _systemPromptService = systemPromptService;
            _toolIntegrationService = toolIntegrationService;
            _logger = logger;

            // Initialize providers
            InitializeProvidersAsync().ConfigureAwait(false);

            // Start model refresh timer
            StartModelRefreshTimer();
        }

        /// <summary>
        /// Initializes the LLM providers
        /// </summary>
        private async Task InitializeProvidersAsync()
        {
            foreach (var provider in _providers)
            {
                try
                {
                    await provider.InitializeAsync();
                    _logger.LogInformation("Initialized LLM provider: {ProviderName}", provider.ProviderName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing LLM provider: {ProviderName}", provider.ProviderName);
                    // Mark provider as failed
                    _providerFailures[provider.ProviderName] = DateTime.UtcNow;
                }
            }

            await SetDefaultActiveProviderAsync();
        }

        /// <summary>
        /// Sets the default active provider based on availability and API key validity
        /// </summary>
        private async Task SetDefaultActiveProviderAsync()
        {
            await _providerLock.WaitAsync();
            try
            {
                // Set the first provider with a valid API key as active, excluding recently failed providers
                var validProviders = _providers
                    .Where(p => p.HasValidApiKey && !IsProviderInFailureBackoff(p.ProviderName))
                    .ToList();

                _activeProvider = validProviders.FirstOrDefault() ??
                                 _providers.FirstOrDefault(p => p.HasValidApiKey) ??
                                 _providers.FirstOrDefault();

                if (_activeProvider != null)
                {
                    _logger.LogInformation("Active LLM provider set to: {ProviderName}", _activeProvider.ProviderName);
                }
                else
                {
                    _logger.LogWarning("No active LLM provider available");
                }
            }
            finally
            {
                _providerLock.Release();
            }
        }

        /// <summary>
        /// Checks if a provider is in failure backoff period
        /// </summary>
        /// <param name="providerName">The provider name</param>
        /// <returns>True if the provider is in backoff period</returns>
        private bool IsProviderInFailureBackoff(string providerName)
        {
            if (_providerFailures.TryGetValue(providerName, out var failureTime))
            {
                return (DateTime.UtcNow - failureTime) < _failureBackoffTime;
            }
            return false;
        }

        /// <summary>
        /// Gets a fallback provider that is not in failure backoff
        /// </summary>
        /// <returns>A fallback provider or null if none available</returns>
        private async Task<ILlmProvider?> GetFallbackProviderAsync()
        {
            await _providerLock.WaitAsync();
            try
            {
                // Find a provider that is not the active provider and not in failure backoff
                var fallbackProvider = _providers
                    .Where(p => p.ProviderName != ActiveProvider.ProviderName &&
                           p.HasValidApiKey &&
                           !IsProviderInFailureBackoff(p.ProviderName))
                    .FirstOrDefault();

                if (fallbackProvider != null)
                {
                    _logger.LogInformation("Found fallback provider: {ProviderName}", fallbackProvider.ProviderName);
                    return fallbackProvider;
                }

                _logger.LogWarning("No fallback providers available");
                return null;
            }
            finally
            {
                _providerLock.Release();
            }
        }

        /// <summary>
        /// Marks a provider as failed
        /// </summary>
        /// <param name="providerName">The provider name</param>
        private async Task MarkProviderAsFailed(string providerName)
        {
            await MarkProviderAsFailedAsync(providerName);
        }

        /// <summary>
        /// Marks a provider as failed
        /// </summary>
        /// <param name="providerName">The provider name</param>
        private async Task MarkProviderAsFailedAsync(string providerName)
        {
            await _providerLock.WaitAsync();
            try
            {
                _providerFailures[providerName] = DateTime.UtcNow;
                _logger.LogWarning("Provider {ProviderName} marked as failed for {BackoffTime}",
                    providerName, _failureBackoffTime);

                // If this was the active provider, switch to another one
                if (_activeProvider?.ProviderName == providerName)
                {
                    await SetDefaultActiveProviderAsync();
                }
            }
            finally
            {
                _providerLock.Release();
            }
        }

        /// <summary>
        /// Gets the token limit for the active model
        /// </summary>
        /// <returns>The token limit</returns>
        private int GetActiveModelTokenLimit()
        {
            if (_activeProvider == null)
            {
                return _modelTokenLimits["default"];
            }

            string modelName = _activeProvider.ModelName.ToLowerInvariant();

            // Try to find an exact match
            if (_modelTokenLimits.TryGetValue(modelName, out int limit))
            {
                return limit;
            }

            // Try to find a partial match
            foreach (var entry in _modelTokenLimits)
            {
                if (modelName.Contains(entry.Key))
                {
                    return entry.Value;
                }
            }

            // Use the provider's default model family
            string providerPrefix = _activeProvider.ProviderName.ToLowerInvariant();
            switch (providerPrefix)
            {
                case "openai":
                    return _modelTokenLimits["gpt-3.5-turbo"];
                case "anthropic":
                    return _modelTokenLimits["claude-3-haiku"];
                case "google":
                    return _modelTokenLimits["gemini-pro"];
                case "meta":
                    return _modelTokenLimits["llama-3-8b"];
                case "deepseek":
                    return _modelTokenLimits["deepseek-chat"];
                default:
                    return _modelTokenLimits["default"];
            }
        }

        /// <summary>
        /// Optimizes the conversation history to fit within the token limit
        /// </summary>
        /// <param name="messages">The messages to optimize</param>
        /// <returns>The optimized messages</returns>
        private List<LlmMessage> OptimizeConversationHistory(IEnumerable<LlmMessage> messages)
        {
            var messagesList = messages.ToList();
            int estimatedTokens = TokenCounter.EstimateTokenCount(messagesList);

            _logger.LogDebug("Conversation history has {EstimatedTokens} tokens (limit: {MaxTokens})",
                estimatedTokens, MaxHistoryTokens);

            // If we're under the limit, return the original list
            if (estimatedTokens <= MaxHistoryTokens)
            {
                return messagesList;
            }

            _logger.LogInformation("Trimming conversation history from {OriginalTokens} tokens to fit within {MaxTokens} tokens",
                estimatedTokens, MaxHistoryTokens);

            // Trim the conversation to fit within the token limit
            return TokenCounter.TrimConversationToFitTokenLimit(messagesList, MaxHistoryTokens);
        }

        /// <summary>
        /// Sets the active LLM provider
        /// </summary>
        /// <param name="providerName">The name of the provider to set as active</param>
        /// <returns>True if the provider was set, false if the provider was not found</returns>
        public Task<bool> SetActiveProviderAsync(string providerName)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            if (provider != null)
            {
                _activeProvider = provider;
                _logger.LogInformation("Active LLM provider set to: {ProviderName}", provider.ProviderName);
                return Task.FromResult(true);
            }

            _logger.LogWarning("LLM provider not found: {ProviderName}", providerName);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets a provider by name
        /// </summary>
        /// <param name="providerName">The name of the provider</param>
        /// <returns>The provider or null if not found</returns>
        public ILlmProvider? GetProvider(string providerName)
        {
            return _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sends a message to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public async Task<LlmResponse> SendMessageAsync(
            string message,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get or create the conversation
                Conversation? conversation = null;
                if (!string.IsNullOrEmpty(conversationId))
                {
                    conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                    if (conversation == null)
                    {
                        _logger.LogWarning("Conversation not found: {ConversationId}, creating new conversation", conversationId);
                        conversationId = null;
                    }
                }

                if (conversation == null)
                {
                    conversationId = await CreateConversationAsync();
                    conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                }

                // Get the system prompt if not provided
                if (string.IsNullOrEmpty(systemPrompt))
                {
                    var defaultPrompt = await _systemPromptService.GetDefaultPromptAsync();
                    systemPrompt = defaultPrompt.Content;
                }

                // Add the user message to the conversation
                conversation!.AddUserMessage(message);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Optimize conversation history to fit within token limits
                var optimizedHistory = OptimizeConversationHistory(conversation.History);

                // Send the message to the LLM with fallback mechanism
                LlmResponse response;
                try
                {
                    response = await ActiveProvider.SendMessagesAsync(
                        optimizedHistory,
                        systemPrompt,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error using primary provider {ProviderName}, attempting fallback",
                        ActiveProvider.ProviderName);

                    // Mark the provider as failed
                    await MarkProviderAsFailedAsync(ActiveProvider.ProviderName);

                    // Try with the new active provider
                    try
                    {
                        response = await ActiveProvider.SendMessagesAsync(
                            conversation.History,
                            systemPrompt,
                            cancellationToken);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Error using fallback provider {ProviderName}",
                            ActiveProvider.ProviderName);

                        // Create a graceful failure response
                        response = new LlmResponse
                        {
                            Message = new LlmMessage
                            {
                                Role = LlmRole.Assistant,
                                Content = "I'm sorry, I'm having trouble connecting to my language model providers. Please try again in a few minutes."
                            },
                            ProviderName = "System",
                            ModelName = "Fallback"
                        };
                    }
                }

                // Process any tool calls in the response
                if (response.ToolCalls.Count > 0)
                {
                    try
                    {
                        response = await _toolIntegrationService.ProcessToolCallsAsync(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing tool calls");
                        // Append error message to response
                        response.Message.Content += "\n\nNote: There was an error processing some tool calls. Some information may be incomplete.";
                    }
                }
                else
                {
                    // Check for tool calls in the message text
                    try
                    {
                        var processedContent = await _toolIntegrationService.ProcessMessageToolCallsAsync(response.Message.Content);
                        if (processedContent != response.Message.Content)
                        {
                            response.Message.Content = processedContent;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message tool calls");
                        // Don't modify the response in this case
                    }
                }

                // Add the assistant response to the conversation
                conversation.AddAssistantMessage(response.Message.Content);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Set the conversation ID in the response
                response.ConversationId = conversationId;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to LLM");
                throw;
            }
        }

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public async Task<LlmResponse> SendMessagesAsync(
            IEnumerable<LlmMessage> messages,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get or create the conversation
                Conversation? conversation = null;
                if (!string.IsNullOrEmpty(conversationId))
                {
                    conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                    if (conversation == null)
                    {
                        _logger.LogWarning("Conversation not found: {ConversationId}, creating new conversation", conversationId);
                        conversationId = null;
                    }
                }

                if (conversation == null)
                {
                    conversationId = await CreateConversationAsync();
                    conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                }

                // Get the system prompt if not provided
                if (string.IsNullOrEmpty(systemPrompt))
                {
                    var defaultPrompt = await _systemPromptService.GetDefaultPromptAsync();
                    systemPrompt = defaultPrompt.Content;
                }

                // Replace the conversation history with the provided messages
                conversation!.History = messages.ToList();
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Send the messages to the LLM
                var response = await ActiveProvider.SendMessagesAsync(
                    messages,
                    systemPrompt,
                    cancellationToken);

                // Process any tool calls in the response
                if (response.ToolCalls.Count > 0)
                {
                    response = await _toolIntegrationService.ProcessToolCallsAsync(response);
                }
                else
                {
                    // Check for tool calls in the message text
                    var processedContent = await _toolIntegrationService.ProcessMessageToolCallsAsync(response.Message.Content);
                    if (processedContent != response.Message.Content)
                    {
                        response.Message.Content = processedContent;
                    }
                }

                // Add the assistant response to the conversation
                conversation.AddAssistantMessage(response.Message.Content);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Set the conversation ID in the response
                response.ConversationId = conversationId;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending messages to LLM");
                throw;
            }
        }

        /// <summary>
        /// Gets the conversation history for a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The conversation history</returns>
        public async Task<IEnumerable<LlmMessage>> GetConversationHistoryAsync(string conversationId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                if (conversation == null)
                {
                    _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
                    return Enumerable.Empty<LlmMessage>();
                }

                return conversation.History;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history: {ConversationId}", conversationId);
                return Enumerable.Empty<LlmMessage>();
            }
        }

        /// <summary>
        /// Creates a new conversation
        /// </summary>
        /// <param name="name">Optional name for the conversation</param>
        /// <returns>The conversation ID</returns>
        public async Task<string> CreateConversationAsync(string? name = null)
        {
            try
            {
                var conversation = new Conversation();

                // Add a system message with the default prompt
                var defaultPrompt = await _systemPromptService.GetDefaultPromptAsync();
                conversation.AddSystemMessage(defaultPrompt.Content);

                return await _conversationRepository.AddConversationAsync(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation");
                throw;
            }
        }

        /// <summary>
        /// Deletes a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        public async Task DeleteConversationAsync(string conversationId)
        {
            try
            {
                await _conversationRepository.DeleteConversationAsync(conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation: {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Sends a message with conversation history to the LLM and gets a streaming response
        /// </summary>
        /// <param name="messages">The conversation history</param>
        /// <param name="onChunk">Callback for each chunk of the response</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complete LLM response</returns>
        public async Task<LlmResponse> SendMessagesStreamingAsync(
            IEnumerable<LlmMessage> messages,
            Action<string> onChunk,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ActiveProvider.SupportsStreaming)
                {
                    _logger.LogWarning("Active provider {ProviderName} does not support streaming", ActiveProvider.ProviderName);
                    return await SendMessagesAsync(messages, systemPrompt, conversationId, cancellationToken);
                }

                // Get or create the conversation
                Conversation? conversation = null;
                if (!string.IsNullOrEmpty(conversationId))
                {
                    conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                    if (conversation == null)
                    {
                        _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
                        conversation = new Conversation();
                    }
                }
                else
                {
                    conversation = new Conversation();
                }

                // Replace the conversation history with the provided messages
                conversation!.History = messages.ToList();
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Get the system prompt if not provided
                if (string.IsNullOrEmpty(systemPrompt))
                {
                    var defaultPrompt = await _systemPromptService.GetDefaultPromptAsync();
                    systemPrompt = defaultPrompt.Content;
                }

                // Optimize conversation history to fit within token limits
                var optimizedHistory = OptimizeConversationHistory(messages);

                // Send the messages to the LLM with streaming and fallback mechanism
                LlmResponse response;
                try
                {
                    response = await ActiveProvider.SendMessagesStreamingAsync(
                        optimizedHistory,
                        systemPrompt,
                        onChunk,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error using primary provider {ProviderName} for streaming, attempting fallback",
                        ActiveProvider.ProviderName);

                    // Mark the provider as failed
                    await MarkProviderAsFailedAsync(ActiveProvider.ProviderName);

                    // Try with the new active provider
                    try
                    {
                        // Send a message to the client that we're switching providers
                        onChunk?.Invoke("\n[Switching to backup provider due to connection issues...]\n");

                        response = await ActiveProvider.SendMessagesStreamingAsync(
                            messages,
                            systemPrompt,
                            onChunk,
                            cancellationToken);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Error using fallback provider {ProviderName} for streaming",
                            ActiveProvider.ProviderName);

                        // Send a final error message to the client
                        var errorMessage = "I'm sorry, I'm having trouble connecting to my language model providers. Please try again in a few minutes.";
                        onChunk?.Invoke(errorMessage);

                        // Create a graceful failure response
                        response = new LlmResponse
                        {
                            Message = new LlmMessage
                            {
                                Role = LlmRole.Assistant,
                                Content = errorMessage
                            },
                            ProviderName = "System",
                            ModelName = "Fallback"
                        };
                    }
                }

                // Process any tool calls in the response
                if (response.ToolCalls.Count > 0)
                {
                    try
                    {
                        response = await _toolIntegrationService.ProcessToolCallsAsync(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing tool calls in streaming response");
                        // Append error message to response
                        response.Message.Content += "\n\nNote: There was an error processing some tool calls. Some information may be incomplete.";
                    }
                }
                else
                {
                    // Check for tool calls in the message text
                    try
                    {
                        var processedContent = await _toolIntegrationService.ProcessMessageToolCallsAsync(response.Message.Content);
                        if (processedContent != response.Message.Content)
                        {
                            response.Message.Content = processedContent;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message tool calls in streaming response");
                        // Don't modify the response in this case
                    }
                }

                // Add the assistant response to the conversation
                conversation.AddAssistantMessage(response.Message.Content);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Set the conversation ID in the response
                response.ConversationId = conversationId;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming messages to LLM");
                throw;
            }
        }

        /// <summary>
        /// Sends a message with tool definitions to the LLM and gets a response with tool calls
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="tools">The tool definitions</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response with tool calls</returns>
        public async Task<LlmResponse> SendMessageWithToolsAsync(
            string message,
            IEnumerable<LlmTool> tools,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get or create the conversation
                Conversation? conversation = null;
                if (!string.IsNullOrEmpty(conversationId))
                {
                    conversation = await _conversationRepository.GetConversationAsync(conversationId);
                    if (conversation == null)
                    {
                        throw new ArgumentException($"Conversation with ID {conversationId} not found");
                    }
                }
                else
                {
                    conversation = new Conversation();
                    await _conversationRepository.AddConversationAsync(conversation);
                    conversationId = conversation.ConversationId;
                }

                // Get the system prompt if not provided
                if (string.IsNullOrEmpty(systemPrompt))
                {
                    var defaultPrompt = await _systemPromptService.GetDefaultPromptAsync();
                    systemPrompt = defaultPrompt.Content;
                }

                // Add the user message to the conversation
                conversation!.AddUserMessage(message);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Optimize conversation history to fit within token limits
                var optimizedHistory = OptimizeConversationHistory(conversation.History);

                // Send the message to the LLM with fallback mechanism
                LlmResponse response;
                try
                {
                    response = await ActiveProvider.SendMessagesWithToolsAsync(
                        optimizedHistory,
                        tools,
                        systemPrompt,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message with tools to {ProviderName}, trying fallback", ActiveProvider.ProviderName);
                    await MarkProviderAsFailed(ActiveProvider.ProviderName);

                    // Try to find a fallback provider
                    var fallbackProvider = await GetFallbackProviderAsync();
                    if (fallbackProvider == null)
                    {
                        _logger.LogError("No fallback provider available");
                        throw new InvalidOperationException("No LLM provider available", ex);
                    }

                    _logger.LogInformation("Using fallback provider {ProviderName}", fallbackProvider.ProviderName);
                    response = await fallbackProvider.SendMessagesWithToolsAsync(
                        optimizedHistory,
                        tools,
                        systemPrompt,
                        cancellationToken);
                }

                // Process any tool calls in the response
                if (response.ToolCalls.Count > 0)
                {
                    response = await _toolIntegrationService.ProcessToolCallsAsync(response);
                }
                else
                {
                    // Check for tool calls in the message text
                    try
                    {
                        var processedContent = await _toolIntegrationService.ProcessMessageToolCallsAsync(response.Message.Content);
                        if (processedContent != response.Message.Content)
                        {
                            response.Message.Content = processedContent;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message tool calls");
                        // Don't modify the response in this case
                    }
                }

                // Add the assistant response to the conversation
                conversation.AddAssistantMessage(response.Message.Content);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Set the conversation ID in the response
                response.ConversationId = conversationId;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with tools");
                throw;
            }
        }

        /// <summary>
        /// Sends a message with tool definitions to the LLM and gets a streaming response with tool calls
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="tools">The tool definitions</param>
        /// <param name="onChunk">Callback for each chunk of the response</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complete LLM response with tool calls</returns>
        public async Task<LlmResponse> SendMessageWithToolsStreamingAsync(
            string message,
            IEnumerable<LlmTool> tools,
            Action<string> onChunk,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get or create the conversation
                Conversation? conversation = null;
                if (!string.IsNullOrEmpty(conversationId))
                {
                    conversation = await _conversationRepository.GetConversationAsync(conversationId);
                    if (conversation == null)
                    {
                        throw new ArgumentException($"Conversation with ID {conversationId} not found");
                    }
                }
                else
                {
                    conversation = new Conversation();
                    await _conversationRepository.AddConversationAsync(conversation);
                    conversationId = conversation.ConversationId;
                }

                // Get the system prompt if not provided
                if (string.IsNullOrEmpty(systemPrompt))
                {
                    var defaultPrompt = await _systemPromptService.GetDefaultPromptAsync();
                    systemPrompt = defaultPrompt.Content;
                }

                // Add the user message to the conversation
                conversation!.AddUserMessage(message);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Optimize conversation history to fit within token limits
                var optimizedHistory = OptimizeConversationHistory(conversation.History);

                // Create a StringBuilder to collect the full response
                var fullResponse = new StringBuilder();

                // Wrap the onChunk callback to collect the full response
                Action<string> onChunkWrapper = chunk =>
                {
                    fullResponse.Append(chunk);
                    onChunk(chunk);
                };

                // Send the message to the LLM with streaming and fallback mechanism
                LlmResponse response;
                try
                {
                    response = await ActiveProvider.SendMessagesWithToolsStreamingAsync(
                        optimizedHistory,
                        tools,
                        systemPrompt,
                        onChunkWrapper,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending streaming message with tools to {ProviderName}, trying fallback", ActiveProvider.ProviderName);
                    await MarkProviderAsFailed(ActiveProvider.ProviderName);

                    // Try to find a fallback provider
                    var fallbackProvider = await GetFallbackProviderAsync();
                    if (fallbackProvider == null)
                    {
                        _logger.LogError("No fallback provider available");
                        throw new InvalidOperationException("No LLM provider available", ex);
                    }

                    _logger.LogInformation("Using fallback provider {ProviderName}", fallbackProvider.ProviderName);
                    response = await fallbackProvider.SendMessagesWithToolsStreamingAsync(
                        optimizedHistory,
                        tools,
                        systemPrompt,
                        onChunkWrapper,
                        cancellationToken);
                }

                // Process any tool calls in the response
                if (response.ToolCalls.Count > 0)
                {
                    response = await _toolIntegrationService.ProcessToolCallsAsync(response);
                }
                else
                {
                    // Check for tool calls in the message text
                    try
                    {
                        var processedContent = await _toolIntegrationService.ProcessMessageToolCallsAsync(response.Message.Content);
                        if (processedContent != response.Message.Content)
                        {
                            response.Message.Content = processedContent;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message tool calls in streaming response");
                        // Don't modify the response in this case
                    }
                }

                // Add the assistant response to the conversation
                conversation.AddAssistantMessage(response.Message.Content);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Set the conversation ID in the response
                response.ConversationId = conversationId;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending streaming message with tools");
                throw;
            }
        }

        /// <summary>
        /// Sends a message with an image to the LLM and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="imageData">The image data</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        /// <param name="conversationId">Optional conversation ID to continue a conversation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The LLM response</returns>
        public async Task<LlmResponse> SendMessageWithImageAsync(
            string message,
            byte[] imageData,
            string? systemPrompt = null,
            string? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Find a provider that supports vision
                if (!ActiveProvider.SupportsVision)
                {
                    _logger.LogWarning("Active provider {ProviderName} does not support vision, looking for alternative", ActiveProvider.ProviderName);

                    // Try to find a provider that supports vision
                    var visionProvider = _providers.FirstOrDefault(p => p.SupportsVision && p.HasValidApiKey && !IsProviderInFailureBackoff(p.ProviderName));

                    if (visionProvider != null)
                    {
                        // Temporarily set this as the active provider for this request
                        _logger.LogInformation("Using vision-capable provider {ProviderName} for image request", visionProvider.ProviderName);
                        _activeProvider = visionProvider;
                    }
                    else
                    {
                        _logger.LogError("No vision-capable providers available");
                        throw new InvalidOperationException("No vision-capable providers available");
                    }
                }

                // Get or create the conversation
                Conversation? conversation = null;
                if (!string.IsNullOrEmpty(conversationId))
                {
                    conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                    if (conversation == null)
                    {
                        _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
                        conversation = new Conversation();
                    }
                }
                else
                {
                    conversation = new Conversation();
                }

                // Add the user message to the conversation
                conversation!.AddUserMessage(message + " [Image attached]");
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Get the system prompt if not provided
                if (string.IsNullOrEmpty(systemPrompt))
                {
                    var defaultPrompt = await _systemPromptService.GetDefaultPromptAsync();
                    systemPrompt = defaultPrompt.Content;
                }

                // Send the message with image to the LLM with fallback mechanism
                LlmResponse response;
                try
                {
                    // TODO: Implement image support
                    response = await ActiveProvider.SendMessageAsync(
                        message + " [Image attached]",
                        systemPrompt,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error using provider {ProviderName} for image request, attempting fallback",
                        ActiveProvider.ProviderName);

                    // Mark the provider as failed
                    await MarkProviderAsFailedAsync(ActiveProvider.ProviderName);

                    // Try to find another vision-capable provider
                    var fallbackProvider = _providers
                        .Where(p => p.ProviderName != ActiveProvider.ProviderName &&
                               p.SupportsVision &&
                               p.HasValidApiKey &&
                               !IsProviderInFailureBackoff(p.ProviderName))
                        .FirstOrDefault();

                    if (fallbackProvider != null)
                    {
                        _logger.LogInformation("Using fallback vision provider {ProviderName}", fallbackProvider.ProviderName);
                        _activeProvider = fallbackProvider;

                        try
                        {
                            // TODO: Implement image support
                            response = await ActiveProvider.SendMessageAsync(
                                message + " [Image attached]",
                                systemPrompt,
                                cancellationToken);
                        }
                        catch (Exception fallbackEx)
                        {
                            _logger.LogError(fallbackEx, "Error using fallback provider {ProviderName} for image request",
                                ActiveProvider.ProviderName);

                            // Create a graceful failure response
                            response = new LlmResponse
                            {
                                Message = new LlmMessage
                                {
                                    Role = LlmRole.Assistant,
                                    Content = "I'm sorry, I'm having trouble processing the image. Please try again later or try a different image."
                                },
                                ProviderName = "System",
                                ModelName = "Fallback"
                            };
                        }
                    }
                    else
                    {
                        _logger.LogError("No fallback vision providers available");

                        // Create a graceful failure response
                        response = new LlmResponse
                        {
                            Message = new LlmMessage
                            {
                                Role = LlmRole.Assistant,
                                Content = "I'm sorry, I'm having trouble processing the image. No vision-capable providers are currently available."
                            },
                            ProviderName = "System",
                            ModelName = "Fallback"
                        };
                    }
                }

                // Add the assistant response to the conversation
                conversation.AddAssistantMessage(response.Message.Content);
                await _conversationRepository.UpdateConversationAsync(conversation);

                // Set the conversation ID in the response
                response.ConversationId = conversationId;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with image to LLM");
                throw;
            }
        }
    }
}
