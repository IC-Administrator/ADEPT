namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Service for voice recognition and synthesis
    /// </summary>
    public interface IVoiceService
    {
        /// <summary>
        /// Gets the current state of the voice service
        /// </summary>
        VoiceServiceState State { get; }

        /// <summary>
        /// Event raised when the voice service state changes
        /// </summary>
        event EventHandler<VoiceServiceStateChangedEventArgs>? StateChanged;

        /// <summary>
        /// Event raised when speech is recognized
        /// </summary>
        event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;

        /// <summary>
        /// Initializes the voice service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Starts listening for the wake word
        /// </summary>
        Task StartListeningForWakeWordAsync();

        /// <summary>
        /// Stops listening for the wake word
        /// </summary>
        Task StopListeningForWakeWordAsync();

        /// <summary>
        /// Starts listening for speech
        /// </summary>
        Task StartListeningForSpeechAsync();

        /// <summary>
        /// Stops listening for speech
        /// </summary>
        Task StopListeningForSpeechAsync();

        /// <summary>
        /// Speaks the specified text
        /// </summary>
        /// <param name="text">The text to speak</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SpeakAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels any ongoing speech
        /// </summary>
        Task CancelSpeechAsync();
    }

    /// <summary>
    /// Voice service state
    /// </summary>
    public enum VoiceServiceState
    {
        /// <summary>
        /// The service is not listening
        /// </summary>
        NotListening,

        /// <summary>
        /// The service is listening for the wake word
        /// </summary>
        ListeningForWakeWord,

        /// <summary>
        /// The service is listening for speech
        /// </summary>
        ListeningForSpeech,

        /// <summary>
        /// The service is processing speech
        /// </summary>
        ProcessingSpeech,

        /// <summary>
        /// The service is speaking
        /// </summary>
        Speaking
    }

    /// <summary>
    /// Event arguments for voice service state changes
    /// </summary>
    public class VoiceServiceStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The previous state
        /// </summary>
        public VoiceServiceState PreviousState { get; }

        /// <summary>
        /// The new state
        /// </summary>
        public VoiceServiceState NewState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceServiceStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        public VoiceServiceStateChangedEventArgs(VoiceServiceState previousState, VoiceServiceState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Event arguments for speech recognition
    /// </summary>
    public class SpeechRecognizedEventArgs : EventArgs
    {
        /// <summary>
        /// The recognized text
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The confidence level (0.0 to 1.0)
        /// </summary>
        public float Confidence { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognizedEventArgs"/> class
        /// </summary>
        /// <param name="text">The recognized text</param>
        /// <param name="confidence">The confidence level</param>
        public SpeechRecognizedEventArgs(string text, float confidence)
        {
            Text = text;
            Confidence = confidence;
        }
    }
}
