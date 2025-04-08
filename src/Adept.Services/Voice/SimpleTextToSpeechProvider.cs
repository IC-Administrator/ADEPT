using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Speech.Synthesis;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Simple text-to-speech provider using System.Speech
    /// </summary>
    public class SimpleTextToSpeechProvider : ITextToSpeechProvider, IDisposable
    {
        private readonly ILogger<SimpleTextToSpeechProvider> _logger;
        private SpeechSynthesizer? _synthesizer;
        private WaveOutEvent? _waveOut;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Simple TTS Provider";

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTextToSpeechProvider"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public SimpleTextToSpeechProvider(ILogger<SimpleTextToSpeechProvider> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public Task InitializeAsync()
        {
            try
            {
                // Initialize speech synthesizer
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.SetOutputToDefaultAudioDevice();
                
                // Initialize wave out
                _waveOut = new WaveOutEvent();
                
                _logger.LogInformation("Text-to-speech provider initialized");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing text-to-speech provider");
                throw;
            }
        }

        /// <summary>
        /// Converts text to speech and returns the audio data
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The audio data</returns>
        public Task<byte[]> ConvertTextToSpeechAsync(string text, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_synthesizer == null)
                {
                    throw new InvalidOperationException("Text-to-speech provider not initialized");
                }

                using var stream = new MemoryStream();
                _synthesizer.SetOutputToWaveStream(stream);
                _synthesizer.Speak(text);
                _synthesizer.SetOutputToDefaultAudioDevice();

                return Task.FromResult(stream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting text to speech: {Text}", text);
                return Task.FromResult(Array.Empty<byte>());
            }
        }

        /// <summary>
        /// Speaks the specified text
        /// </summary>
        /// <param name="text">The text to speak</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            try
            {
                // Cancel any ongoing speech
                await CancelSpeechAsync();

                // Create a new cancellation token source
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Convert text to speech
                var audioData = await ConvertTextToSpeechAsync(text, _cancellationTokenSource.Token);
                if (audioData.Length == 0)
                {
                    return;
                }

                // Play the audio
                using var audioStream = new MemoryStream(audioData);
                using var reader = new WaveFileReader(audioStream);
                var sampleProvider = reader.ToSampleProvider();
                
                var completionSource = new TaskCompletionSource<bool>();
                
                _waveOut!.Init(sampleProvider);
                _waveOut.PlaybackStopped += (s, e) => completionSource.TrySetResult(true);
                _waveOut.Play();

                // Wait for playback to complete or cancellation
                await using var registration = _cancellationTokenSource.Token.Register(() => completionSource.TrySetCanceled());
                await completionSource.Task;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Speech cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error speaking text: {Text}", text);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Cancels any ongoing speech
        /// </summary>
        public Task CancelSpeechAsync()
        {
            try
            {
                if (_waveOut?.PlaybackState == PlaybackState.Playing)
                {
                    _waveOut.Stop();
                }

                _cancellationTokenSource?.Cancel();
                _logger.LogInformation("Speech cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling speech");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the text-to-speech provider
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the text-to-speech provider
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;

                    _waveOut?.Stop();
                    _waveOut?.Dispose();
                    _waveOut = null;

                    _synthesizer?.Dispose();
                    _synthesizer = null;
                }

                _disposed = true;
            }
        }
    }
}
