namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Factory for creating voice providers
    /// </summary>
    public interface IVoiceProviderFactory
    {
        /// <summary>
        /// Creates a wake word detector
        /// </summary>
        /// <returns>The wake word detector</returns>
        Task<IWakeWordDetector> CreateWakeWordDetectorAsync();

        /// <summary>
        /// Creates a speech-to-text provider
        /// </summary>
        /// <returns>The speech-to-text provider</returns>
        Task<ISpeechToTextProvider> CreateSpeechToTextProviderAsync();

        /// <summary>
        /// Creates a text-to-speech provider
        /// </summary>
        /// <returns>The text-to-speech provider</returns>
        Task<ITextToSpeechProvider> CreateTextToSpeechProviderAsync();
    }
}
