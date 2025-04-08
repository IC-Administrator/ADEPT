namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Detector for wake words
    /// </summary>
    public interface IWakeWordDetector
    {
        /// <summary>
        /// Gets the wake word
        /// </summary>
        string WakeWord { get; }

        /// <summary>
        /// Event raised when the wake word is detected
        /// </summary>
        event EventHandler<WakeWordDetectedEventArgs>? WakeWordDetected;

        /// <summary>
        /// Initializes the detector
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Starts listening for the wake word
        /// </summary>
        Task StartListeningAsync();

        /// <summary>
        /// Stops listening for the wake word
        /// </summary>
        Task StopListeningAsync();
    }

    /// <summary>
    /// Event arguments for wake word detection
    /// </summary>
    public class WakeWordDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// The detected wake word
        /// </summary>
        public string WakeWord { get; }

        /// <summary>
        /// The confidence level (0.0 to 1.0)
        /// </summary>
        public float Confidence { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WakeWordDetectedEventArgs"/> class
        /// </summary>
        /// <param name="wakeWord">The detected wake word</param>
        /// <param name="confidence">The confidence level</param>
        public WakeWordDetectedEventArgs(string wakeWord, float confidence)
        {
            WakeWord = wakeWord;
            Confidence = confidence;
        }
    }
}
