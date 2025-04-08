using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Provider for speech-to-text conversion using Google's Speech-to-Text API
    /// </summary>
    public class GoogleSpeechToTextProvider : ISpeechToTextProvider, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<GoogleSpeechToTextProvider> _logger;
        private readonly WaveInEvent _waveIn;
        private readonly MemoryStream _audioStream = new();
        private readonly WaveFileWriter _waveWriter;
        private bool _isListening;
        private bool _isInitialized;
        private bool _disposed;
        private string _apiKey = string.Empty;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Google Speech-to-Text";

        /// <summary>
        /// Event raised when speech is recognized
        /// </summary>
        public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleSpeechToTextProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public GoogleSpeechToTextProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<GoogleSpeechToTextProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secureStorageService = secureStorageService;
            _logger = logger;

            // Initialize audio capture
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1), // 16kHz, mono
                BufferMilliseconds = 100 // 100ms buffer
            };
            _waveIn.DataAvailable += OnAudioDataAvailable;

            // Initialize wave writer
            _waveWriter = new WaveFileWriter(_audioStream, _waveIn.WaveFormat);
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
                    _logger.LogInformation("Google speech-to-text provider initialized");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Google speech-to-text provider");
                throw;
            }
        }

        /// <summary>
        /// Starts listening for speech
        /// </summary>
        public Task StartListeningAsync()
        {
            if (_isListening)
            {
                return Task.CompletedTask;
            }

            try
            {
                // Reset the audio stream
                _audioStream.SetLength(0);

                // Start audio capture
                _waveIn.StartRecording();
                _isListening = true;

                _logger.LogInformation("Started listening for speech");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting speech recognition");
                throw;
            }
        }

        /// <summary>
        /// Stops listening for speech and returns the recognized text
        /// </summary>
        /// <returns>The recognized text and confidence level</returns>
        public async Task<(string Text, float Confidence)> StopListeningAsync()
        {
            if (!_isListening)
            {
                return (string.Empty, 0);
            }

            try
            {
                // Stop audio capture
                _waveIn.StopRecording();
                _isListening = false;

                // Flush the wave writer
                _waveWriter.Flush();

                // Get the audio data
                var audioData = _audioStream.ToArray();

                if (audioData.Length == 0)
                {
                    _logger.LogWarning("No audio data captured");
                    return (string.Empty, 0);
                }

                // Convert the audio to text
                var result = await ConvertSpeechToTextAsync(audioData);

                _logger.LogInformation("Stopped listening for speech, recognized: {Text}", result.Text);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping speech recognition");
                throw;
            }
        }

        /// <summary>
        /// Cancels any ongoing speech recognition
        /// </summary>
        public Task CancelAsync()
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

                // Reset the audio stream
                _audioStream.SetLength(0);

                _logger.LogInformation("Cancelled speech recognition");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling speech recognition");
                throw;
            }
        }

        /// <summary>
        /// Converts audio data to text
        /// </summary>
        /// <param name="audioData">The audio data</param>
        /// <returns>The recognized text and confidence level</returns>
        public async Task<(string Text, float Confidence)> ConvertSpeechToTextAsync(byte[] audioData)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Google API key not set");
                return ("Google API key not set", 0);
            }

            try
            {
                // Convert the audio data to base64
                var base64Audio = Convert.ToBase64String(audioData);

                // Create the request body
                var requestBody = new
                {
                    config = new
                    {
                        encoding = "LINEAR16",
                        sampleRateHertz = 16000,
                        languageCode = "en-US",
                        model = "default",
                        enableAutomaticPunctuation = true
                    },
                    audio = new
                    {
                        content = base64Audio
                    }
                };

                // Create the HTTP client
                var client = _httpClientFactory.CreateClient();

                // Send the request
                var url = $"https://speech.googleapis.com/v1/speech:recognize?key={_apiKey}";
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Extract the text and confidence
                var text = string.Empty;
                var confidence = 0.0f;

                if (responseObject.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("alternatives", out var alternatives) && alternatives.GetArrayLength() > 0)
                    {
                        var firstAlternative = alternatives[0];
                        text = firstAlternative.GetProperty("transcript").GetString() ?? string.Empty;

                        if (firstAlternative.TryGetProperty("confidence", out var confidenceElement))
                        {
                            confidence = confidenceElement.GetSingle();
                        }
                    }
                }

                _logger.LogInformation("Converted speech to text: {Text} (Confidence: {Confidence})", text, confidence);
                return (text, confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting speech to text");
                return ("Error: " + ex.Message, 0);
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
                // Write the audio data to the wave writer
                _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio data");
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
                // Stop listening if we're still listening
                if (_isListening)
                {
                    _waveIn.StopRecording();
                    _isListening = false;
                }

                // Dispose managed resources
                _waveWriter.Dispose();
                _audioStream.Dispose();
                _waveIn.Dispose();
            }

            _disposed = true;
        }
    }
}
