using Adept.Core.Interfaces;
using Adept.UI.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using static Adept.UI.ViewModels.HomeViewModel;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// View model for the Chat tab
    /// </summary>
    public class ChatViewModel : ViewModelBase
    {
        private readonly ILlmService _llmService;
        private readonly IVoiceService _voiceService;
        private readonly ILogger<ChatViewModel> _logger;
        private string _userInput = string.Empty;
        private string _currentConversationId = string.Empty;
        private bool _isBusy;
        private bool _isStreaming = true;

        /// <summary>
        /// Gets or sets the user input
        /// </summary>
        public string UserInput
        {
            get => _userInput;
            set => SetProperty(ref _userInput, value);
        }

        /// <summary>
        /// Gets or sets whether the view model is busy
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets whether to use streaming responses
        /// </summary>
        public bool IsStreaming
        {
            get => _isStreaming;
            set => SetProperty(ref _isStreaming, value);
        }

        /// <summary>
        /// Gets the conversation messages
        /// </summary>
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

        /// <summary>
        /// Gets the send message command
        /// </summary>
        public ICommand SendMessageCommand { get; }

        /// <summary>
        /// Gets the clear conversation command
        /// </summary>
        public ICommand ClearConversationCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatViewModel"/> class
        /// </summary>
        /// <param name="llmService">The LLM service</param>
        /// <param name="voiceService">The voice service</param>
        /// <param name="logger">The logger</param>
        public ChatViewModel(ILlmService llmService, IVoiceService voiceService, ILogger<ChatViewModel> logger)
        {
            _llmService = llmService;
            _voiceService = voiceService;
            _logger = logger;

            SendMessageCommand = new RelayCommand(SendMessageAsync, CanSendMessage);
            ClearConversationCommand = new RelayCommand(ClearConversationAsync);

            // Subscribe to voice service events
            _voiceService.SpeechRecognized += OnSpeechRecognized;

            // Initialize a new conversation
            InitializeConversationAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes a new conversation
        /// </summary>
        private async Task InitializeConversationAsync()
        {
            try
            {
                IsBusy = true;
                Messages.Clear();
                _currentConversationId = await _llmService.CreateConversationAsync();

                // Add a welcome message
                Messages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = "Hello! I'm Adept, your AI teaching assistant. How can I help you today?"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing conversation");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Sends a message to the LLM
        /// </summary>
        private async void SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserInput))
            {
                return;
            }

            try
            {
                IsBusy = true;

                // Add the user message to the UI
                var userMessage = new ChatMessage
                {
                    Role = "user",
                    Content = UserInput
                };
                Messages.Add(userMessage);

                // Clear the input
                var userInput = UserInput;
                UserInput = string.Empty;

                if (IsStreaming)
                {
                    // Create a placeholder for the assistant's response
                    var assistantMessage = new ChatMessage
                    {
                        Role = "assistant",
                        Content = ""
                    };
                    Messages.Add(assistantMessage);

                    // Send the message to the LLM with streaming
                    var response = await _llmService.SendMessageAsync(
                        userInput,
                        null,
                        _currentConversationId);

                    // Update the assistant's message with the response
                    assistantMessage.Content = response.Message.Content;
                    OnPropertyChanged(nameof(Messages));
                }
                else
                {
                    // Send the message to the LLM without streaming
                    var response = await _llmService.SendMessageAsync(userInput, null, _currentConversationId);

                    // Add the assistant message to the UI
                    var assistantMessage = new ChatMessage
                    {
                        Role = "assistant",
                        Content = response.Message.Content
                    };
                    Messages.Add(assistantMessage);
                }

                // Speak the response (optional)
                // await _voiceService.SpeakAsync(response.Message.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");

                // Add an error message to the UI
                Messages.Add(new ChatMessage
                {
                    Role = "system",
                    Content = $"Error: {ex.Message}"
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Determines whether a message can be sent
        /// </summary>
        /// <returns>True if a message can be sent, false otherwise</returns>
        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(UserInput) && !IsBusy;
        }

        /// <summary>
        /// Clears the conversation
        /// </summary>
        private async void ClearConversationAsync()
        {
            try
            {
                // Delete the current conversation
                if (!string.IsNullOrEmpty(_currentConversationId))
                {
                    await _llmService.DeleteConversationAsync(_currentConversationId);
                }

                // Initialize a new conversation
                await InitializeConversationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing conversation");
            }
        }

        /// <summary>
        /// Handles speech recognition
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            // Set the recognized speech as the user input
            UserInput = e.Text;

            // Send the message
            if (CanSendMessage())
            {
                SendMessageAsync();
            }
        }
    }
}
