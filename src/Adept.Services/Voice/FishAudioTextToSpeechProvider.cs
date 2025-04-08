using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using MessagePack;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
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
        private readonly IDatabaseContext _databaseContext;
        private readonly ILogger<FishAudioTextToSpeechProvider> _logger;
        private readonly WaveOutEvent _waveOut = new();
        private readonly ConcurrentDictionary<string, byte[]> _memoryCache = new();
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private string _apiKey = string.Empty;
        private string _voiceId = string.Empty;
        private float _speechSpeed = 1.0f;
        private float _speechVolume = 0.0f;
        private float _speechClarity = 0.0f;
        private float _speechEmotion = 0.0f;
        private string _ttsModel = "speech-1.6";
        private int _maxRetries = 3;
        private int _reconnectDelayMs = 1000;
        private bool _isInitialized;
        private bool _disposed;
        private bool _useDiskCache = true;
        private int _maxCacheItems = 100;
        private long _cacheHits = 0;
        private long _cacheMisses = 0;
        private long _totalCacheSize = 0;
        private DateTime _lastCacheCleanup = DateTime.MinValue;

        /// <summary>
        /// Available voice options
        /// </summary>
        public static readonly Dictionary<string, string> AvailableVoices = new()
        {
            { "default", "Default" },
            { "male-1", "Male 1" },
            { "male-2", "Male 2" },
            { "female-1", "Female 1" },
            { "female-2", "Female 2" },
            { "child-1", "Child" },
            { "elder-1", "Elder" },
            { "narrator-1", "Narrator" },
            { "assistant-1", "Assistant" }
        };

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Fish Audio";

        /// <summary>
        /// Initializes a new instance of the <see cref="FishAudioTextToSpeechProvider"/> class
        /// </summary>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="configurationService">The configuration service</param>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public FishAudioTextToSpeechProvider(
            ISecureStorageService secureStorageService,
            IConfigurationService configurationService,
            IDatabaseContext databaseContext,
            ILogger<FishAudioTextToSpeechProvider> logger)
        {
            _secureStorageService = secureStorageService;
            _configurationService = configurationService;
            _databaseContext = databaseContext;
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

                // Get configuration values
                _voiceId = await _configurationService.GetConfigurationValueAsync("fish_audio_voice_id", "default") ?? "default";
                _ttsModel = await _configurationService.GetConfigurationValueAsync("fish_audio_model", "speech-1.6") ?? "speech-1.6";
                _speechSpeed = float.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_speed", "1.0") ?? "1.0");
                _speechVolume = float.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_volume", "0.0") ?? "0.0");
                _speechClarity = float.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_clarity", "0.0") ?? "0.0");
                _speechEmotion = float.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_emotion", "0.0") ?? "0.0");
                _useDiskCache = bool.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_use_disk_cache", "true") ?? "true");
                _maxCacheItems = int.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_max_cache_items", "100") ?? "100");
                _maxRetries = int.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_max_retries", "3") ?? "3");
                _reconnectDelayMs = int.Parse(await _configurationService.GetConfigurationValueAsync("fish_audio_reconnect_delay_ms", "1000") ?? "1000");

                // Initialize the cache table if using disk cache
                if (_useDiskCache)
                {
                    await _databaseContext.ExecuteNonQueryAsync(
                        "CREATE TABLE IF NOT EXISTS TtsCache (hash TEXT PRIMARY KEY, text TEXT, audio BLOB, voice_id TEXT, created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)");

                    // Clean up old cache entries if we have too many
                    await _databaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM TtsCache WHERE rowid NOT IN (SELECT rowid FROM TtsCache ORDER BY created_at DESC LIMIT @MaxItems)",
                        new { MaxItems = _maxCacheItems });
                }

                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Fish Audio API key not found in secure storage");
                }
                else
                {
                    _isInitialized = true;
                    _logger.LogInformation("Fish Audio TTS provider initialized with voice ID: {VoiceId}, model: {Model}, speed: {Speed}, volume: {Volume}",
                        _voiceId, _ttsModel, _speechSpeed, _speechVolume);
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
        public async Task SetVoiceAsync(string voiceId)
        {
            _voiceId = voiceId;
            await _configurationService.SetConfigurationValueAsync("fish_audio_voice_id", voiceId);
            _logger.LogInformation("Set voice to {VoiceId}", voiceId);
        }

        /// <summary>
        /// Sets the TTS model to use
        /// </summary>
        /// <param name="model">The model to use (speech-1.5, speech-1.6, agent-x0)</param>
        public async Task SetModelAsync(string model)
        {
            _ttsModel = model;
            await _configurationService.SetConfigurationValueAsync("fish_audio_model", model);
            _logger.LogInformation("Set TTS model to {Model}", model);
        }

        /// <summary>
        /// Sets the speech speed
        /// </summary>
        /// <param name="speed">The speech speed (0.5-2.0)</param>
        public async Task SetSpeechSpeedAsync(float speed)
        {
            if (speed < 0.5f || speed > 2.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), "Speech speed must be between 0.5 and 2.0");
            }

            _speechSpeed = speed;
            await _configurationService.SetConfigurationValueAsync("fish_audio_speed", speed.ToString());
            _logger.LogInformation("Set speech speed to {Speed}", speed);
        }

        /// <summary>
        /// Sets the speech volume
        /// </summary>
        /// <param name="volume">The volume adjustment in dB</param>
        public async Task SetSpeechVolumeAsync(float volume)
        {
            _speechVolume = volume;
            await _configurationService.SetConfigurationValueAsync("fish_audio_volume", volume.ToString());
            _logger.LogInformation("Set speech volume to {Volume}dB", volume);
        }

        /// <summary>
        /// Sets the speech clarity
        /// </summary>
        /// <param name="clarity">The clarity value (-1.0 to 1.0)</param>
        public async Task SetSpeechClarityAsync(float clarity)
        {
            if (clarity < -1.0f || clarity > 1.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(clarity), "Speech clarity must be between -1.0 and 1.0");
            }

            _speechClarity = clarity;
            await _configurationService.SetConfigurationValueAsync("fish_audio_clarity", clarity.ToString());
            _logger.LogInformation("Set speech clarity to {Clarity}", clarity);
        }

        /// <summary>
        /// Sets the speech emotion
        /// </summary>
        /// <param name="emotion">The emotion value (-1.0 to 1.0)</param>
        public async Task SetSpeechEmotionAsync(float emotion)
        {
            if (emotion < -1.0f || emotion > 1.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(emotion), "Speech emotion must be between -1.0 and 1.0");
            }

            _speechEmotion = emotion;
            await _configurationService.SetConfigurationValueAsync("fish_audio_emotion", emotion.ToString());
            _logger.LogInformation("Set speech emotion to {Emotion}", emotion);
        }

        /// <summary>
        /// Clears the TTS cache
        /// </summary>
        public async Task ClearCacheAsync()
        {
            try
            {
                // Clear memory cache
                _memoryCache.Clear();

                // Clear disk cache if enabled
                if (_useDiskCache)
                {
                    await _databaseContext.ExecuteNonQueryAsync("DELETE FROM TtsCache");
                }

                // Reset cache statistics
                _cacheHits = 0;
                _cacheMisses = 0;
                _totalCacheSize = 0;
                _lastCacheCleanup = DateTime.Now;

                _logger.LogInformation("TTS cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing TTS cache");
                throw;
            }
        }

        /// <summary>
        /// Gets the cache statistics
        /// </summary>
        /// <returns>A dictionary with cache statistics</returns>
        public async Task<Dictionary<string, string>> GetCacheStatisticsAsync()
        {
            var stats = new Dictionary<string, string>
            {
                { "MemoryCacheCount", _memoryCache.Count.ToString() },
                { "CacheHits", _cacheHits.ToString() },
                { "CacheMisses", _cacheMisses.ToString() },
                { "HitRatio", _cacheHits + _cacheMisses > 0 ? ((_cacheHits * 100.0) / (_cacheHits + _cacheMisses)).ToString("F2") + "%" : "0%" },
                { "TotalCacheSize", _totalCacheSize.ToString() },
                { "LastCacheCleanup", _lastCacheCleanup != DateTime.MinValue ? _lastCacheCleanup.ToString() : "Never" },
                { "UseDiskCache", _useDiskCache.ToString() },
                { "MaxCacheItems", _maxCacheItems.ToString() }
            };

            // Get disk cache count and size if enabled
            if (_useDiskCache)
            {
                var diskCacheStats = await _databaseContext.QuerySingleOrDefaultAsync<DiskCacheStats>(
                    "SELECT COUNT(*) AS Count, SUM(LENGTH(audio)) AS Size FROM TtsCache");

                if (diskCacheStats != null)
                {
                    stats["DiskCacheCount"] = diskCacheStats.Count.ToString();
                    stats["DiskCacheSize"] = diskCacheStats.Size.ToString();
                }
            }

            return stats;
        }

        /// <summary>
        /// Sets the maximum number of cache items
        /// </summary>
        /// <param name="maxItems">The maximum number of cache items</param>
        public async Task SetMaxCacheItemsAsync(int maxItems)
        {
            if (maxItems < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), "Maximum cache items must be at least 1");
            }

            _maxCacheItems = maxItems;
            await _configurationService.SetConfigurationValueAsync("fish_audio_max_cache_items", maxItems.ToString());

            // Trim memory cache if needed
            if (_memoryCache.Count > _maxCacheItems)
            {
                var keysToRemove = _memoryCache.Keys.Take(_memoryCache.Count - _maxCacheItems);
                foreach (var key in keysToRemove)
                {
                    _memoryCache.TryRemove(key, out _);
                }
            }

            // Trim disk cache if needed
            if (_useDiskCache)
            {
                await _databaseContext.ExecuteNonQueryAsync(
                    "DELETE FROM TtsCache WHERE rowid NOT IN (SELECT rowid FROM TtsCache ORDER BY created_at DESC LIMIT @MaxItems)",
                    new { MaxItems = _maxCacheItems });
            }

            _logger.LogInformation("Set maximum cache items to {MaxItems}", maxItems);
        }

        /// <summary>
        /// Sets whether to use disk cache
        /// </summary>
        /// <param name="useDiskCache">Whether to use disk cache</param>
        public async Task SetUseDiskCacheAsync(bool useDiskCache)
        {
            _useDiskCache = useDiskCache;
            await _configurationService.SetConfigurationValueAsync("fish_audio_use_disk_cache", useDiskCache.ToString());
            _logger.LogInformation("Set use disk cache to {UseDiskCache}", useDiskCache);
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

            // Generate a hash for the text and voice combination for caching
            string cacheKey = GenerateCacheKey(text, _voiceId, _ttsModel, _speechSpeed, _speechVolume);

            try
            {
                // Check memory cache first
                if (_memoryCache.TryGetValue(cacheKey, out var cachedAudioData))
                {
                    _cacheHits++;
                    _cacheMisses--; // Adjust since we incremented in GenerateCacheKey
                    _logger.LogInformation("Retrieved audio from memory cache: {TextLength} characters", text.Length);
                    return cachedAudioData;
                }

                // Check disk cache if enabled
                if (_useDiskCache)
                {
                    var cachedResult = await _databaseContext.QuerySingleOrDefaultAsync<CachedTtsEntry>(
                        "SELECT audio FROM TtsCache WHERE hash = @Hash",
                        new { Hash = cacheKey });

                    if (cachedResult != null && cachedResult.audio != null)
                    {
                        // Add to memory cache for faster retrieval next time
                        _memoryCache.TryAdd(cacheKey, cachedResult.audio);

                        // Trim memory cache if it gets too large
                        if (_memoryCache.Count > _maxCacheItems)
                        {
                            var keysToRemove = _memoryCache.Keys.Take(_memoryCache.Count - _maxCacheItems);
                            foreach (var key in keysToRemove)
                            {
                                _memoryCache.TryRemove(key, out _);
                            }
                        }

                        _cacheHits++;
                        _cacheMisses--; // Adjust since we incremented in GenerateCacheKey
                        _logger.LogInformation("Retrieved audio from disk cache: {TextLength} characters", text.Length);
                        return cachedResult.audio;
                    }
                }

                // Not in cache, need to generate the audio
                byte[] audioData = await GenerateAudioFromTextAsync(text, cancellationToken);

                // Add to memory cache
                _memoryCache.TryAdd(cacheKey, audioData);

                // Add to disk cache if enabled
                if (_useDiskCache)
                {
                    await _databaseContext.ExecuteNonQueryAsync(
                        @"INSERT INTO TtsCache (hash, text, audio, voice_id, created_at)
                          VALUES (@Hash, @Text, @Audio, @VoiceId, CURRENT_TIMESTAMP)
                          ON CONFLICT(hash) DO UPDATE SET
                          audio = @Audio,
                          created_at = CURRENT_TIMESTAMP",
                        new { Hash = cacheKey, Text = text, Audio = audioData, VoiceId = _voiceId });
                }

                return audioData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting text to speech");
                throw;
            }
        }

        /// <summary>
        /// Generates a cache key for the given text and voice parameters
        /// </summary>
        private string GenerateCacheKey(string text, string voiceId, string model, float speed, float volume)
        {
            // Create a unique hash based on all parameters that affect the audio output
            string combinedInput = $"{text}|{voiceId}|{model}|{speed}|{volume}|{_speechClarity}|{_speechEmotion}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedInput));

            // Update cache statistics
            _cacheMisses++;

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Generates audio from text using the Fish Audio WebSocket API
        /// </summary>
        private async Task<byte[]> GenerateAudioFromTextAsync(string text, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    await _connectionSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        // Create a new WebSocket connection
                        _webSocket = new ClientWebSocket();
                        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                        _webSocket.Options.SetRequestHeader("model", _ttsModel);

                        // Connect to the Fish Audio WebSocket API
                        await _webSocket.ConnectAsync(new Uri("wss://api.fish.audio/v1/tts/live"), cancellationToken);

                        // Create the start event
                        var startEvent = new Dictionary<string, object>
                        {
                            ["event"] = "start",
                            ["request"] = new Dictionary<string, object>
                            {
                                ["text"] = "",  // Initial empty text
                                ["latency"] = "normal",
                                ["format"] = "wav",
                                ["temperature"] = 0.7,
                                ["top_p"] = 0.7,
                                ["prosody"] = new Dictionary<string, object>
                                {
                                    ["speed"] = _speechSpeed,
                                    ["volume"] = _speechVolume,
                                    ["clarity"] = _speechClarity,
                                    ["emotion"] = _speechEmotion
                                },
                                ["reference_id"] = _voiceId
                            }
                        };

                        // Send the start event
                        var startBytes = MessagePackSerializer.Serialize(startEvent);
                        await _webSocket.SendAsync(
                            new ArraySegment<byte>(startBytes),
                            WebSocketMessageType.Binary,
                            true,
                            cancellationToken);

                        // Send the text event
                        var textEvent = new Dictionary<string, object>
                        {
                            ["event"] = "text",
                            ["text"] = text
                        };

                        var textBytes = MessagePackSerializer.Serialize(textEvent);
                        await _webSocket.SendAsync(
                            new ArraySegment<byte>(textBytes),
                            WebSocketMessageType.Binary,
                            true,
                            cancellationToken);

                        // Send the stop event
                        var stopEvent = new Dictionary<string, object>
                        {
                            ["event"] = "stop"
                        };

                        var stopBytes = MessagePackSerializer.Serialize(stopEvent);
                        await _webSocket.SendAsync(
                            new ArraySegment<byte>(stopBytes),
                            WebSocketMessageType.Binary,
                            true,
                            cancellationToken);

                        // Receive the audio data
                        using var memoryStream = new MemoryStream();
                        var buffer = new byte[8192];

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

                            // Process the received message
                            if (result.MessageType == WebSocketMessageType.Binary)
                            {
                                try
                                {
                                    var responseObj = MessagePackSerializer.Deserialize<Dictionary<string, object>>(new ReadOnlyMemory<byte>(buffer, 0, result.Count));

                                    if (responseObj.TryGetValue("event", out var eventValue) && eventValue is string eventType)
                                    {
                                        if (eventType == "audio" && responseObj.TryGetValue("audio", out var audioValue) && audioValue is byte[] audioData)
                                        {
                                            // Write audio data directly to memory stream
                                            memoryStream.Write(audioData, 0, audioData.Length);
                                        }
                                        else if (eventType == "finish")
                                        {
                                            break;
                                        }
                                        else if (eventType == "log" && responseObj.TryGetValue("message", out var messageValue) && messageValue is string logMessage)
                                        {
                                            _logger.LogInformation("Fish Audio API: {Message}", logMessage);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing WebSocket message");
                                }
                            }


                            if (result.EndOfMessage && memoryStream.Length > 0)
                            {
                                // We have received the complete audio data
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
                        var audioResult = memoryStream.ToArray();
                        _logger.LogInformation("Generated audio from text: {TextLength} characters, {AudioLength} bytes", text.Length, audioResult.Length);
                        return audioResult;
                    }
                    finally
                    {
                        _connectionSemaphore.Release();
                        _webSocket?.Dispose();
                        _webSocket = null;
                    }
                }
                catch (Exception ex) when (retryCount < _maxRetries &&
                                         (ex is WebSocketException ||
                                          ex is IOException ||
                                          ex is TimeoutException))
                {
                    retryCount++;
                    _logger.LogWarning(ex, "Error in WebSocket connection, retrying ({RetryCount}/{MaxRetries})...", retryCount, _maxRetries);

                    // Dispose the current WebSocket
                    _webSocket?.Dispose();
                    _webSocket = null;

                    // Wait before retrying
                    await Task.Delay(_reconnectDelayMs * retryCount, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating audio from text");
                    throw;
                }
            }
        }

        /// <summary>
        /// Class for caching TTS entries
        /// </summary>
        private class CachedTtsEntry
        {
            public byte[]? audio { get; set; }
        }

        /// <summary>
        /// Class for disk cache statistics
        /// </summary>
        private class DiskCacheStats
        {
            public long Count { get; set; }
            public long Size { get; set; }
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

                // Stop any existing playback
                if (_waveOut.PlaybackState != PlaybackState.Stopped)
                {
                    _waveOut.Stop();
                }

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
        /// Gets available voices from the configuration
        /// </summary>
        /// <returns>A dictionary of voice IDs and names</returns>
        public async Task<Dictionary<string, string>> GetAvailableVoicesAsync()
        {
            try
            {
                // In a real implementation, this would query the Fish Audio API for available voices
                // For now, we'll return a hardcoded list of common voices
                var voices = new Dictionary<string, string>
                {
                    { "default", "Default Voice" },
                    { "male-1", "Male Voice 1" },
                    { "female-1", "Female Voice 1" },
                    { "male-2", "Male Voice 2" },
                    { "female-2", "Female Voice 2" }
                };

                // Add any custom voices from the database
                var customVoices = await _databaseContext.QueryAsync<(string id, string name)>(
                    "SELECT DISTINCT voice_id as id, voice_id as name FROM TtsCache WHERE voice_id NOT IN ('default', 'male-1', 'female-1', 'male-2', 'female-2')");

                foreach (var voice in customVoices)
                {
                    if (!voices.ContainsKey(voice.id))
                    {
                        voices.Add(voice.id, $"Custom: {voice.name}");
                    }
                }

                return voices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available voices");
                throw;
            }
        }

        /// <summary>
        /// Gets available TTS models
        /// </summary>
        /// <returns>A dictionary of model IDs and names</returns>
        public Dictionary<string, string> GetAvailableModels()
        {
            return new Dictionary<string, string>
            {
                { "speech-1.5", "Fish Audio Speech 1.5" },
                { "speech-1.6", "Fish Audio Speech 1.6" },
                { "agent-x0", "Fish Audio Agent X0" }
            };
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

                // Dispose the semaphore
                _connectionSemaphore.Dispose();
            }

            _disposed = true;
        }
    }
}
