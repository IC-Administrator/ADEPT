using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Provider for speech-to-text conversion using OpenAI's Whisper API
    /// </summary>
    public class WhisperSpeechToTextProvider : ISpeechToTextProvider, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<WhisperSpeechToTextProvider> _logger;
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
        public string ProviderName => "OpenAI Whisper";

        /// <summary>
        /// Event raised when speech is recognized
        /// </summary>
        public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperSpeechToTextProvider"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public WhisperSpeechToTextProvider(
            IHttpClientFactory httpClientFactory,
            ISecureStorageService secureStorageService,
            ILogger<WhisperSpeechToTextProvider> logger)
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
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("openai_api_key") ?? string.Empty;

                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("OpenAI API key not found in secure storage");
                }
                else
                {
                    _isInitialized = true;
                    _logger.LogInformation("Whisper speech-to-text provider initialized");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Whisper speech-to-text provider");
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
                _logger.LogWarning("OpenAI API key not set");
                return ("OpenAI API key not set", 0);
            }

            try
            {
                // Create a temporary WAV file
                var tempFile = Path.GetTempFileName() + ".wav";
                await File.WriteAllBytesAsync(tempFile, audioData);

                // Create the HTTP client
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                // Create the form content
                using var formContent = new MultipartFormDataContent();
                using var fileContent = new StreamContent(File.OpenRead(tempFile));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                formContent.Add(fileContent, "file", "audio.wav");
                formContent.Add(new StringContent("whisper-1"), "model");
                formContent.Add(new StringContent("en"), "language");

                // Send the request
                var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", formContent);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var text = responseObject.GetProperty("text").GetString() ?? string.Empty;

                // Clean up the temporary file
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore errors when deleting the temp file
                }

                _logger.LogInformation("Converted speech to text: {Text}", text);
                return (text, 1.0f); // Whisper doesn't provide confidence scores
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
