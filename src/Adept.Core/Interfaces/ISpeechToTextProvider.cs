namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Provider for speech-to-text conversion
    /// </summary>
    public interface ISpeechToTextProvider
    {
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Event raised when speech is recognized
        /// </summary>
        event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;

        /// <summary>
        /// Initializes the provider
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Starts listening for speech
        /// </summary>
        Task StartListeningAsync();

        /// <summary>
        /// Stops listening for speech and returns the recognized text
        /// </summary>
        /// <returns>The recognized text and confidence level</returns>
        Task<(string Text, float Confidence)> StopListeningAsync();

        /// <summary>
        /// Cancels any ongoing speech recognition
        /// </summary>
        Task CancelAsync();

        /// <summary>
        /// Converts audio data to text
        /// </summary>
        /// <param name="audioData">The audio data</param>
        /// <returns>The recognized text and confidence level</returns>
        Task<(string Text, float Confidence)> ConvertSpeechToTextAsync(byte[] audioData);
    }
}
