namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Provider for text-to-speech conversion
    /// </summary>
    public interface ITextToSpeechProvider
    {
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Converts text to speech and returns the audio data
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The audio data</returns>
        Task<byte[]> ConvertTextToSpeechAsync(string text, CancellationToken cancellationToken = default);

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
}
