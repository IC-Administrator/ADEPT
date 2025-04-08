using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Provider for text-to-speech conversion using Google's Text-to-Speech API
    /// </summary>
    public class GoogleTextToSpeechProvider : ITextToSpeechProvider, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<GoogleTextToSpeechProvider> _logger;
        private readonly WaveOutEvent _waveOut = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isInitialized;
        private bool _disposed;
        private string _apiKey = string.Empty;
        private string _voice = "en-US-Standard-D"; // Default voice

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Google Text-to-Speech";

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleTextToSpeechProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public GoogleTextToSpeechProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<GoogleTextToSpeechProvider> logger)
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
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("google_api_key") ?? string.Empty;

                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Google API key not found in secure storage");
                }
                else
                {
                    _isInitialized = true;
                    _logger.LogInformation("Google TTS provider initialized");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Google TTS provider");
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
                _logger.LogWarning("Google API key not set");
                throw new InvalidOperationException("Google API key not set");
            }

            try
            {
                // Create the request body
                var requestBody = new
                {
                    input = new
                    {
                        text
                    },
                    voice = new
                    {
                        languageCode = "en-US",
                        name = _voice
                    },
                    audioConfig = new
                    {
                        audioEncoding = "MP3"
                    }
                };

                // Create the HTTP client
                var client = _httpClientFactory.CreateClient();

                // Send the request
                var url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={_apiKey}";
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var audioContent = responseObject.GetProperty("audioContent").GetString() ?? string.Empty;

                // Decode the base64 audio content
                var audioData = Convert.FromBase64String(audioContent);

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
        public Task CancelSpeechAsync()
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
