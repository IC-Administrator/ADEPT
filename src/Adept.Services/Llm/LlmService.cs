using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;

namespace Adept.Services.Llm
{
    /// <summary>
    /// Service for interacting with Large Language Models
    /// </summary>
    public class LlmService : ILlmService
    {
        private readonly IEnumerable<ILlmProvider> _providers;
        private readonly IConversationRepository _conversationRepository;
        private readonly ISystemPromptService _systemPromptService;
        private readonly ILogger<LlmService> _logger;
        private ILlmProvider? _activeProvider;

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
        /// <param name="logger">The logger</param>
        public LlmService(
            IEnumerable<ILlmProvider> providers,
            IConversationRepository conversationRepository,
            ISystemPromptService systemPromptService,
            ILogger<LlmService> logger)
        {
            _providers = providers;
            _conversationRepository = conversationRepository;
            _systemPromptService = systemPromptService;
            _logger = logger;

            // Initialize providers
            InitializeProvidersAsync().ConfigureAwait(false);
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
                }
            }

            // Set the first provider with a valid API key as active
            _activeProvider = _providers.FirstOrDefault(p => p.HasValidApiKey) ?? _providers.FirstOrDefault();
            
            if (_activeProvider != null)
            {
                _logger.LogInformation("Active LLM provider set to: {ProviderName}", _activeProvider.ProviderName);
            }
            else
            {
                _logger.LogWarning("No active LLM provider available");
            }
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

                // Send the message to the LLM
                var response = await ActiveProvider.SendMessagesAsync(
                    conversation.History,
                    systemPrompt,
                    cancellationToken);

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
    }
}
