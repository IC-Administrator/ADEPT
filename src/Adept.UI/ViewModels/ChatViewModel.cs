using Adept.Core.Interfaces;
using Adept.UI.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private bool _isVoiceInputEnabled;
        private bool _isVoiceOutputEnabled;
        private VoiceServiceState _voiceServiceState = VoiceServiceState.NotListening;

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
        /// Gets or sets whether voice input is enabled
        /// </summary>
        public bool IsVoiceInputEnabled
        {
            get => _isVoiceInputEnabled;
            set => SetProperty(ref _isVoiceInputEnabled, value);
        }

        /// <summary>
        /// Gets or sets whether voice output is enabled
        /// </summary>
        public bool IsVoiceOutputEnabled
        {
            get => _isVoiceOutputEnabled;
            set => SetProperty(ref _isVoiceOutputEnabled, value);
        }

        /// <summary>
        /// Gets the current voice service state
        /// </summary>
        public VoiceServiceState VoiceServiceState
        {
            get => _voiceServiceState;
            private set => SetProperty(ref _voiceServiceState, value);
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
        /// Gets the toggle streaming command
        /// </summary>
        public ICommand ToggleStreamingCommand { get; }

        /// <summary>
        /// Gets the toggle voice input command
        /// </summary>
        public ICommand ToggleVoiceInputCommand { get; }

        /// <summary>
        /// Gets the toggle voice output command
        /// </summary>
        public ICommand ToggleVoiceOutputCommand { get; }

        /// <summary>
        /// Gets the start voice input command
        /// </summary>
        public ICommand StartVoiceInputCommand { get; }

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
            ToggleStreamingCommand = new RelayCommand(ToggleStreaming);
            ToggleVoiceInputCommand = new RelayCommand(ToggleVoiceInput);
            ToggleVoiceOutputCommand = new RelayCommand(ToggleVoiceOutput);
            StartVoiceInputCommand = new RelayCommand(StartVoiceInputAsync, CanStartVoiceInput);

            // Subscribe to voice service events
            _voiceService.SpeechRecognized += OnSpeechRecognized;
            _voiceService.StateChanged += OnVoiceServiceStateChanged;

            // Initialize voice service state
            VoiceServiceState = _voiceService.State;

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

                LlmResponse response;
                ChatMessage assistantMessage;

                if (IsStreaming)
                {
                    // Create a placeholder for the assistant's response
                    assistantMessage = new ChatMessage
                    {
                        Role = "assistant",
                        Content = ""
                    };
                    Messages.Add(assistantMessage);

                    // Convert the conversation history to LlmMessages
                    var history = new List<LlmMessage>();
                    foreach (var message in Messages.Take(Messages.Count - 1)) // Exclude the empty assistant message
                    {
                        LlmRole role = message.Role.ToLowerInvariant() switch
                        {
                            "user" => LlmRole.User,
                            "assistant" => LlmRole.Assistant,
                            "system" => LlmRole.System,
                            _ => LlmRole.User
                        };
                        history.Add(new LlmMessage(role, message.Content));
                    }

                    // Send the message to the LLM with streaming
                    response = await _llmService.SendMessagesStreamingAsync(
                        history,
                        chunk =>
                        {
                            // Update the UI with each chunk
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                assistantMessage.Content += chunk;
                                OnPropertyChanged(nameof(Messages));
                            });
                        },
                        null,
                        _currentConversationId);

                    // Ensure the final content is set correctly
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        assistantMessage.Content = response.Message.Content;
                        OnPropertyChanged(nameof(Messages));
                    });
                }
                else
                {
                    // Send the message to the LLM without streaming
                    response = await _llmService.SendMessageAsync(userInput, null, _currentConversationId);

                    // Add the assistant message to the UI
                    assistantMessage = new ChatMessage
                    {
                        Role = "assistant",
                        Content = response.Message.Content
                    };
                    Messages.Add(assistantMessage);
                }

                // Speak the response if voice output is enabled
                if (IsVoiceOutputEnabled)
                {
                    await _voiceService.SpeakAsync(assistantMessage.Content);
                }
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

        /// <summary>
        /// Toggles streaming mode on or off
        /// </summary>
        private void ToggleStreaming()
        {
            IsStreaming = !IsStreaming;
            _logger.LogInformation($"Streaming mode {(IsStreaming ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Toggles voice input on or off
        /// </summary>
        private void ToggleVoiceInput()
        {
            IsVoiceInputEnabled = !IsVoiceInputEnabled;
            _logger.LogInformation($"Voice input {(IsVoiceInputEnabled ? "enabled" : "disabled")}");

            if (IsVoiceInputEnabled)
            {
                // Start listening for wake word
                _voiceService.StartListeningForWakeWordAsync().ConfigureAwait(false);
            }
            else
            {
                // Stop listening
                _voiceService.StopListeningForWakeWordAsync().ConfigureAwait(false);
                _voiceService.StopListeningForSpeechAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Toggles voice output on or off
        /// </summary>
        private void ToggleVoiceOutput()
        {
            IsVoiceOutputEnabled = !IsVoiceOutputEnabled;
            _logger.LogInformation($"Voice output {(IsVoiceOutputEnabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Starts voice input
        /// </summary>
        private async void StartVoiceInputAsync()
        {
            try
            {
                // Start listening for speech
                await _voiceService.StartListeningForSpeechAsync();
                _logger.LogInformation("Started listening for speech");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting voice input");
            }
        }

        /// <summary>
        /// Determines whether voice input can be started
        /// </summary>
        private bool CanStartVoiceInput()
        {
            return IsVoiceInputEnabled &&
                   VoiceServiceState != VoiceServiceState.ListeningForSpeech &&
                   VoiceServiceState != VoiceServiceState.ProcessingSpeech;
        }

        /// <summary>
        /// Handles voice service state changes
        /// </summary>
        private void OnVoiceServiceStateChanged(object? sender, VoiceServiceStateChangedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                VoiceServiceState = e.NewState;
                _logger.LogInformation("Voice service state changed to {State}", e.NewState);

                // Update command availability
                (StartVoiceInputCommand as RelayCommand)?.RaiseCanExecuteChanged();
            });
        }
    }
}
