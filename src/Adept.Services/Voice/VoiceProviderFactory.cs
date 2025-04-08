using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adept.Services.Voice
{
    /// <summary>
    /// Factory for creating voice providers
    /// </summary>
    public class VoiceProviderFactory : IVoiceProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<VoiceProviderFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceProviderFactory"/> class
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <param name="configurationService">The configuration service</param>
        /// <param name="logger">The logger</param>
        public VoiceProviderFactory(
            IServiceProvider serviceProvider,
            IConfigurationService configurationService,
            ILogger<VoiceProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a wake word detector
        /// </summary>
        /// <returns>The wake word detector</returns>
        public async Task<IWakeWordDetector> CreateWakeWordDetectorAsync()
        {
            try
            {
                var providerName = await _configurationService.GetConfigurationValueAsync("wake_word_detector", "simple");

                IWakeWordDetector detector = providerName.ToLowerInvariant() switch
                {
                    "vosk" => _serviceProvider.GetRequiredService<VoskWakeWordDetector>(),
                    _ => _serviceProvider.GetRequiredService<SimpleWakeWordDetector>()
                };

                await detector.InitializeAsync();
                return detector;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wake word detector");
                throw;
            }
        }

        /// <summary>
        /// Creates a speech-to-text provider
        /// </summary>
        /// <returns>The speech-to-text provider</returns>
        public async Task<ISpeechToTextProvider> CreateSpeechToTextProviderAsync()
        {
            try
            {
                var providerName = await _configurationService.GetConfigurationValueAsync("speech_to_text_provider", "simple");

                ISpeechToTextProvider provider = providerName.ToLowerInvariant() switch
                {
                    "whisper" => _serviceProvider.GetRequiredService<WhisperSpeechToTextProvider>(),
                    "google" => _serviceProvider.GetRequiredService<GoogleSpeechToTextProvider>(),
                    _ => _serviceProvider.GetRequiredService<SimpleSpeechToTextProvider>()
                };

                await provider.InitializeAsync();
                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating speech-to-text provider");
                throw;
            }
        }

        /// <summary>
        /// Creates a text-to-speech provider
        /// </summary>
        /// <returns>The text-to-speech provider</returns>
        public async Task<ITextToSpeechProvider> CreateTextToSpeechProviderAsync()
        {
            try
            {
                var providerName = await _configurationService.GetConfigurationValueAsync("text_to_speech_provider", "simple");

                ITextToSpeechProvider provider = providerName.ToLowerInvariant() switch
                {
                    "fishaudio" => _serviceProvider.GetRequiredService<FishAudioTextToSpeechProvider>(),
                    "openai" => _serviceProvider.GetRequiredService<OpenAiTextToSpeechProvider>(),
                    "google" => _serviceProvider.GetRequiredService<GoogleTextToSpeechProvider>(),
                    _ => _serviceProvider.GetRequiredService<SimpleTextToSpeechProvider>()
                };

                await provider.InitializeAsync();
                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating text-to-speech provider");
                throw;
            }
        }
    }
}
