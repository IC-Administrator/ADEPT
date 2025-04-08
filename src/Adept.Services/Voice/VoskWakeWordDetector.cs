using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Wake word detector using Vosk for offline speech recognition
    /// </summary>
    public class VoskWakeWordDetector : IWakeWordDetector, IDisposable
    {
        private readonly ILogger<VoskWakeWordDetector> _logger;
        private readonly string _wakeWord;
        private readonly float _confidenceThreshold;
        private readonly int _sampleRate;
        private readonly WaveInEvent _waveIn;
        private readonly ConcurrentQueue<byte[]> _audioQueue = new();
        private readonly CancellationTokenSource _processingCancellationTokenSource = new();
        private bool _isListening;
        private bool _isInitialized;
        private bool _disposed;
        private Task? _processingTask;

        /// <summary>
        /// Gets the wake word
        /// </summary>
        public string WakeWord => _wakeWord;

        /// <summary>
        /// Event raised when the wake word is detected
        /// </summary>
        public event EventHandler<WakeWordDetectedEventArgs>? WakeWordDetected;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoskWakeWordDetector"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public VoskWakeWordDetector(ILogger<VoskWakeWordDetector> logger)
        {
            _logger = logger;
            _wakeWord = "hey adept"; // Default wake word
            _confidenceThreshold = 0.7f; // Default confidence threshold
            _sampleRate = 16000; // 16kHz sample rate for Vosk

            // Initialize audio capture
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(_sampleRate, 1), // 16kHz, mono
                BufferMilliseconds = 100 // 100ms buffer
            };
            _waveIn.DataAvailable += OnAudioDataAvailable;
        }

        /// <summary>
        /// Initializes the detector
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                // In a real implementation, we would download and initialize the Vosk model here
                // For now, we'll just simulate initialization
                await Task.Delay(500); // Simulate model loading time

                _isInitialized = true;
                _logger.LogInformation("Wake word detector initialized with wake word: {WakeWord}", _wakeWord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing wake word detector");
                throw;
            }
        }

        /// <summary>
        /// Starts listening for the wake word
        /// </summary>
        public Task StartListeningAsync()
        {
            if (_isListening)
            {
                return Task.CompletedTask;
            }

            try
            {
                // Start audio capture
                _waveIn.StartRecording();
                _isListening = true;

                // Start processing audio in the background
                _processingTask = Task.Run(ProcessAudioQueueAsync, _processingCancellationTokenSource.Token);

                _logger.LogInformation("Started listening for wake word: {WakeWord}", _wakeWord);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting wake word detection");
                throw;
            }
        }

        /// <summary>
        /// Stops listening for the wake word
        /// </summary>
        public Task StopListeningAsync()
        {
            if (!_isListening)
            {
                return Task.CompletedTask;
            }

            try
            {
                // Stop audio capture
                _waveIn.StopRecording();
                _isListening = false;

                _logger.LogInformation("Stopped listening for wake word");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping wake word detection");
                throw;
            }
        }

        /// <summary>
        /// Handles audio data
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!_isListening || e.BytesRecorded == 0)
            {
                return;
            }

            try
            {
                // Copy the audio data to a new buffer and add it to the queue
                var buffer = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, buffer, e.BytesRecorded);
                _audioQueue.Enqueue(buffer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio data");
            }
        }

        /// <summary>
        /// Processes the audio queue
        /// </summary>
        private async Task ProcessAudioQueueAsync()
        {
            try
            {
                // In a real implementation, this would use Vosk to recognize speech
                // and detect the wake word. For now, we'll simulate detection.
                while (!_processingCancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Process audio chunks from the queue
                    while (_audioQueue.TryDequeue(out var audioChunk))
                    {
                        // Simulate wake word detection with a low probability
                        if (new Random().NextDouble() < 0.001) // 0.1% chance per audio chunk
                        {
                            var confidence = (float)new Random().NextDouble();
                            if (confidence >= _confidenceThreshold)
                            {
                                WakeWordDetected?.Invoke(this, new WakeWordDetectedEventArgs(_wakeWord, confidence));
                            }
                        }
                    }

                    // Wait a bit before checking the queue again
                    await Task.Delay(10, _processingCancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audio processing task");
            }
        }

        /// <summary>
        /// Disposes the detector
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the detector
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Stop listening if we're still listening
                if (_isListening)
                {
                    _waveIn.StopRecording();
                    _isListening = false;
                }

                // Cancel and wait for the processing task to complete
                _processingCancellationTokenSource.Cancel();
                _processingTask?.Wait(1000);

                // Dispose managed resources
                _processingCancellationTokenSource.Dispose();
                _waveIn.Dispose();
            }

            _disposed = true;
        }
    }
}
