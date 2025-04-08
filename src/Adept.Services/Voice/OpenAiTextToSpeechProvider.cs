using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Provider for text-to-speech conversion using OpenAI's TTS API
    /// </summary>
    public class OpenAiTextToSpeechProvider : ITextToSpeechProvider, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<OpenAiTextToSpeechProvider> _logger;
        private readonly WaveOutEvent _waveOut = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isInitialized;
        private bool _disposed;
        private string _apiKey = string.Empty;
        private string _voice = "alloy"; // Default voice

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "OpenAI TTS";

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAiTextToSpeechProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public OpenAiTextToSpeechProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<OpenAiTextToSpeechProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secureStorageService = secureStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                // Get the API key from secure storage
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("openai_api_key") ?? string.Empty;
                
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("OpenAI API key not found in secure storage");
                }
                else
                {
                    _isInitialized = true;
                    _logger.LogInformation("OpenAI TTS provider initialized");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing OpenAI TTS provider");
                throw;
            }
        }

        /// <summary>
        /// Sets the voice to use
        /// </summary>
        /// <param name="voice">The voice to use</param>
        public void SetVoice(string voice)
        {
            _voice = voice;
            _logger.LogInformation("Set voice to {Voice}", voice);
        }

        /// <summary>
        /// Converts text to speech and returns the audio data
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The audio data</returns>
        public async Task<byte[]> ConvertTextToSpeechAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("OpenAI API key not set");
                throw new InvalidOperationException("OpenAI API key not set");
            }

            try
            {
                // Create the HTTP client
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                // Create the request body
                var requestBody = new
                {
                    model = "tts-1",
                    input = text,
                    voice = _voice,
                    response_format = "mp3"
                };

                // Send the request
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/audio/speech", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Get the audio data
                var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                
                _logger.LogInformation("Converted text to speech: {TextLength} characters", text.Length);
                return audioData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting text to speech");
                throw;
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
                // Create a linked cancellation token source
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                
                // Convert the text to speech
                var audioData = await ConvertTextToSpeechAsync(text, _cancellationTokenSource.Token);
                
                // Play the audio
                using var audioStream = new MemoryStream(audioData);
                using var reader = new Mp3FileReader(audioStream);
                var sampleProvider = reader.ToSampleProvider();
                
                var completionSource = new TaskCompletionSource<bool>();
                
                _waveOut.Init(sampleProvider);
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
                throw;
            }
        }

        /// <summary>
        /// Cancels any ongoing speech
        /// </summary>
        public Task CancelAsync()
        {
            try
            {
                // Cancel the current speech
                _cancellationTokenSource?.Cancel();
                
                // Stop the wave out
                if (_waveOut.PlaybackState != PlaybackState.Stopped)
                {
                    _waveOut.Stop();
                }

                _logger.LogInformation("Cancelled speech");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling speech");
                throw;
            }
        }

        /// <summary>
        /// Disposes the provider
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the provider
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
                // Cancel any ongoing speech
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                // Dispose the wave out
                _waveOut.Dispose();
            }

            _disposed = true;
        }
    }
}
