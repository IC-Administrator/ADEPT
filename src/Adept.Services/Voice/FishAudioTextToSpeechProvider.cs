using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Provider for text-to-speech conversion using Fish Audio's WebSocket API
    /// </summary>
    public class FishAudioTextToSpeechProvider : ITextToSpeechProvider, IDisposable
    {
        private readonly ISecureStorageService _secureStorageService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<FishAudioTextToSpeechProvider> _logger;
        private readonly WaveOutEvent _waveOut = new();
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private string _apiKey = string.Empty;
        private string _voiceId = string.Empty;
        private bool _isInitialized;
        private bool _disposed;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Fish Audio";

        /// <summary>
        /// Initializes a new instance of the <see cref="FishAudioTextToSpeechProvider"/> class
        /// </summary>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="configurationService">The configuration service</param>
        /// <param name="logger">The logger</param>
        public FishAudioTextToSpeechProvider(
            ISecureStorageService secureStorageService,
            IConfigurationService configurationService,
            ILogger<FishAudioTextToSpeechProvider> logger)
        {
            _secureStorageService = secureStorageService;
            _configurationService = configurationService;
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
                _apiKey = await _secureStorageService.RetrieveSecureValueAsync("fish_audio_api_key") ?? string.Empty;
                
                // Get the voice ID from configuration
                _voiceId = await _configurationService.GetConfigurationValueAsync("fish_audio_voice_id", "default") ?? "default";
                
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Fish Audio API key not found in secure storage");
                }
                else
                {
                    _isInitialized = true;
                    _logger.LogInformation("Fish Audio TTS provider initialized with voice ID: {VoiceId}", _voiceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Fish Audio TTS provider");
                throw;
            }
        }

        /// <summary>
        /// Sets the voice to use
        /// </summary>
        /// <param name="voiceId">The voice ID to use</param>
        public void SetVoice(string voiceId)
        {
            _voiceId = voiceId;
            _logger.LogInformation("Set voice to {VoiceId}", voiceId);
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
                _logger.LogWarning("Fish Audio API key not set");
                throw new InvalidOperationException("Fish Audio API key not set");
            }

            try
            {
                // Create a new WebSocket connection
                _webSocket = new ClientWebSocket();
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                
                // Connect to the Fish Audio WebSocket API
                await _webSocket.ConnectAsync(new Uri("wss://api.fish.audio/v1/tts/stream"), cancellationToken);
                
                // Create the request
                var request = new
                {
                    text = text,
                    voice_id = _voiceId,
                    output_format = "wav"
                };
                
                // Send the request
                var requestJson = JsonSerializer.Serialize(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(requestBytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
                
                // Receive the audio data
                using var memoryStream = new MemoryStream();
                var buffer = new byte[4096];
                
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            string.Empty,
                            cancellationToken);
                        break;
                    }
                    
                    // Write the received data to the memory stream
                    memoryStream.Write(buffer, 0, result.Count);
                    
                    if (result.EndOfMessage)
                    {
                        break;
                    }
                }
                
                // Close the WebSocket connection
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        string.Empty,
                        cancellationToken);
                }
                
                // Return the audio data
                var audioData = memoryStream.ToArray();
                _logger.LogInformation("Converted text to speech: {TextLength} characters, {AudioLength} bytes", text.Length, audioData.Length);
                return audioData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting text to speech");
                throw;
            }
            finally
            {
                _webSocket?.Dispose();
                _webSocket = null;
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
                using var reader = new WaveFileReader(audioStream);
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
                
                // Dispose the WebSocket
                _webSocket?.Dispose();
            }

            _disposed = true;
        }
    }
}
