using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Service for voice recognition and synthesis
    /// </summary>
    public class VoiceService : IVoiceService, IDisposable
    {
        private readonly IVoiceProviderFactory _voiceProviderFactory;
        private readonly ILogger<VoiceService> _logger;
        private IWakeWordDetector? _wakeWordDetector;
        private ISpeechToTextProvider? _speechToTextProvider;
        private ITextToSpeechProvider? _textToSpeechProvider;
        private VoiceServiceState _state = VoiceServiceState.NotListening;
        private CancellationTokenSource? _speechCancellationTokenSource;
        private bool _isInitialized;
        private bool _disposed;

        /// <summary>
        /// Gets the current state of the voice service
        /// </summary>
        public VoiceServiceState State => _state;

        /// <summary>
        /// Event raised when the voice service state changes
        /// </summary>
        public event EventHandler<VoiceServiceStateChangedEventArgs>? StateChanged;

        /// <summary>
        /// Event raised when speech is recognized
        /// </summary>
        public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceService"/> class
        /// </summary>
        /// <param name="voiceProviderFactory">The voice provider factory</param>
        /// <param name="logger">The logger</param>
        public VoiceService(
            IVoiceProviderFactory voiceProviderFactory,
            ILogger<VoiceService> logger)
        {
            _voiceProviderFactory = voiceProviderFactory;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the service
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                // Create the providers
                _wakeWordDetector = await _voiceProviderFactory.CreateWakeWordDetectorAsync();
                _speechToTextProvider = await _voiceProviderFactory.CreateSpeechToTextProviderAsync();
                _textToSpeechProvider = await _voiceProviderFactory.CreateTextToSpeechProviderAsync();

                // Subscribe to the wake word detected event
                _wakeWordDetector.WakeWordDetected += OnWakeWordDetected;

                // Subscribe to the speech recognized event
                if (_speechToTextProvider is ISpeechToTextProvider stt && stt.SpeechRecognized != null)
                {
                    stt.SpeechRecognized += OnSpeechRecognized;
                }

                _isInitialized = true;
                _logger.LogInformation("Voice service initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing voice service");
                throw;
            }


        }

        /// <summary>
        /// Initializes the providers
        /// </summary>
        private async Task InitializeProvidersAsync()
        {
            try
            {
                await _wakeWordDetector.InitializeAsync();
                await _speechToTextProvider.InitializeAsync();
                await _textToSpeechProvider.InitializeAsync();
                _logger.LogInformation("Voice service providers initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing voice service providers");
            }
        }

        /// <summary>
        /// Starts listening for the wake word
        /// </summary>
        public async Task StartListeningForWakeWordAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (_state != VoiceServiceState.NotListening)
            {
                _logger.LogWarning("Cannot start listening for wake word while in state {State}", _state);
                return;
            }

            try
            {
                if (_wakeWordDetector == null)
                {
                    throw new InvalidOperationException("Wake word detector not initialized");
                }

                await _wakeWordDetector.StartListeningAsync();
                SetState(VoiceServiceState.ListeningForWakeWord);
                _logger.LogInformation("Started listening for wake word");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting wake word detection");
            }
        }

        /// <summary>
        /// Stops listening for the wake word
        /// </summary>
        public async Task StopListeningForWakeWordAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (_state != VoiceServiceState.ListeningForWakeWord)
            {
                _logger.LogWarning("Cannot stop listening for wake word while in state {State}", _state);
                return;
            }

            try
            {
                if (_wakeWordDetector == null)
                {
                    throw new InvalidOperationException("Wake word detector not initialized");
                }

                await _wakeWordDetector.StopListeningAsync();
                SetState(VoiceServiceState.NotListening);
                _logger.LogInformation("Stopped listening for wake word");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping wake word detection");
            }
        }

        /// <summary>
        /// Starts listening for speech
        /// </summary>
        public async Task StartListeningForSpeechAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (_state != VoiceServiceState.NotListening && _state != VoiceServiceState.ListeningForWakeWord)
            {
                _logger.LogWarning("Cannot start listening for speech while in state {State}", _state);
                return;
            }

            try
            {
                if (_wakeWordDetector == null || _speechToTextProvider == null)
                {
                    throw new InvalidOperationException("Voice providers not initialized");
                }

                // If we were listening for the wake word, stop that first
                if (_state == VoiceServiceState.ListeningForWakeWord)
                {
                    await _wakeWordDetector.StopListeningAsync();
                }

                await _speechToTextProvider.StartListeningAsync();
                SetState(VoiceServiceState.ListeningForSpeech);
                _logger.LogInformation("Started listening for speech");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting speech recognition");
                // Try to go back to listening for the wake word
                if (_state == VoiceServiceState.ListeningForWakeWord)
                {
                    await StartListeningForWakeWordAsync();
                }
                else
                {
                    SetState(VoiceServiceState.NotListening);
                }
            }
        }

        /// <summary>
        /// Stops listening for speech
        /// </summary>
        public async Task StopListeningForSpeechAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (_state != VoiceServiceState.ListeningForSpeech)
            {
                _logger.LogWarning("Cannot stop listening for speech while in state {State}", _state);
                return;
            }

            try
            {
                if (_speechToTextProvider == null)
                {
                    throw new InvalidOperationException("Speech-to-text provider not initialized");
                }

                SetState(VoiceServiceState.ProcessingSpeech);
                var (text, confidence) = await _speechToTextProvider.StopListeningAsync();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogInformation("Speech recognized: {Text} (Confidence: {Confidence})", text, confidence);
                    SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(text, confidence));
                }
                else
                {
                    _logger.LogInformation("No speech recognized");
                }

                // Go back to listening for the wake word
                await StartListeningForWakeWordAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping speech recognition");
                SetState(VoiceServiceState.NotListening);
            }
        }

        /// <summary>
        /// Speaks the specified text
        /// </summary>
        /// <param name="text">The text to speak</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                if (_textToSpeechProvider == null)
                {
                    throw new InvalidOperationException("Text-to-speech provider not initialized");
                }

                // Cancel any ongoing speech
                await CancelSpeechAsync();

                // Create a new cancellation token source
                _speechCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Set the state to speaking
                var previousState = _state;
                SetState(VoiceServiceState.Speaking);

                // Speak the text
                await _textToSpeechProvider.SpeakAsync(text, _speechCancellationTokenSource.Token);

                // If we were cancelled, don't change the state
                if (!_speechCancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Go back to the previous state if it was listening for the wake word
                    if (previousState == VoiceServiceState.ListeningForWakeWord)
                    {
                        await StartListeningForWakeWordAsync();
                    }
                    else
                    {
                        SetState(VoiceServiceState.NotListening);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Speech cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error speaking text");
                SetState(VoiceServiceState.NotListening);
            }
            finally
            {
                _speechCancellationTokenSource?.Dispose();
                _speechCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Cancels any ongoing speech
        /// </summary>
        public async Task CancelSpeechAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (_state == VoiceServiceState.Speaking && _speechCancellationTokenSource != null)
            {
                try
                {
                    if (_textToSpeechProvider == null)
                    {
                        throw new InvalidOperationException("Text-to-speech provider not initialized");
                    }

                    _speechCancellationTokenSource.Cancel();
                    await _textToSpeechProvider.CancelSpeechAsync();
                    _logger.LogInformation("Speech cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cancelling speech");
                }
            }
        }

        /// <summary>
        /// Sets the state of the voice service
        /// </summary>
        /// <param name="newState">The new state</param>
        private void SetState(VoiceServiceState newState)
        {
            if (_state == newState)
            {
                return;
            }

            var previousState = _state;
            _state = newState;
            _logger.LogInformation("Voice service state changed from {PreviousState} to {NewState}", previousState, newState);
            StateChanged?.Invoke(this, new VoiceServiceStateChangedEventArgs(previousState, newState));
        }

        /// <summary>
        /// Handles wake word detection
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private async void OnWakeWordDetected(object? sender, WakeWordDetectedEventArgs e)
        {
            _logger.LogInformation("Wake word detected: {WakeWord} (Confidence: {Confidence})", e.WakeWord, e.Confidence);
            await StartListeningForSpeechAsync();
        }

        /// <summary>
        /// Handles speech recognition
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            _logger.LogInformation("Speech recognized: {Text} (Confidence: {Confidence})", e.Text, e.Confidence);

            // Forward the event to subscribers
            SpeechRecognized?.Invoke(this, e);
        }

        /// <summary>
        /// Disposes the voice service
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the voice service
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events
                    if (_wakeWordDetector != null)
                    {
                        _wakeWordDetector.WakeWordDetected -= OnWakeWordDetected;

                        // Dispose the wake word detector if it's disposable
                        if (_wakeWordDetector is IDisposable disposableWakeWordDetector)
                        {
                            disposableWakeWordDetector.Dispose();
                        }
                    }

                    // Dispose the speech-to-text provider if it's disposable
                    if (_speechToTextProvider is IDisposable disposableSpeechToTextProvider)
                    {
                        disposableSpeechToTextProvider.Dispose();
                    }

                    // Dispose the text-to-speech provider if it's disposable
                    if (_textToSpeechProvider is IDisposable disposableTextToSpeechProvider)
                    {
                        disposableTextToSpeechProvider.Dispose();
                    }

                    // Cancel any ongoing speech
                    _speechCancellationTokenSource?.Cancel();
                    _speechCancellationTokenSource?.Dispose();
                    _speechCancellationTokenSource = null;
                }

                _disposed = true;
            }
        }
    }
}
