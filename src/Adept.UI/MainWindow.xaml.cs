using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Media;

namespace Adept.UI
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly IVoiceService _voiceService;

        public MainWindow(ILogger<MainWindow> logger, IVoiceService voiceService)
        {
            _logger = logger;
            _voiceService = voiceService;

            InitializeComponent();

            // Subscribe to voice service events
            _voiceService.StateChanged += OnVoiceServiceStateChanged;
            _voiceService.SpeechRecognized += OnSpeechRecognized;

            _logger.LogInformation("MainWindow initialized");
        }

        /// <summary>
        /// Handles the window loaded event
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Start listening for the wake word
                await _voiceService.StartListeningForWakeWordAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting voice service");
            }
        }

        /// <summary>
        /// Handles the window closing event
        /// </summary>
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Stop listening
                await _voiceService.StopListeningForWakeWordAsync();

                // Unsubscribe from events
                _voiceService.StateChanged -= OnVoiceServiceStateChanged;
                _voiceService.SpeechRecognized -= OnSpeechRecognized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping voice service");
            }
        }

        /// <summary>
        /// Handles voice service state changes
        /// </summary>
        private void OnVoiceServiceStateChanged(object? sender, VoiceServiceStateChangedEventArgs e)
        {
            // Update UI based on voice service state
            Dispatcher.Invoke(() =>
            {
                switch (e.NewState)
                {
                    case VoiceServiceState.NotListening:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Gray);
                        StatusText.Text = "Not Listening";
                        break;
                    case VoiceServiceState.ListeningForWakeWord:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Green);
                        StatusText.Text = "Listening for Wake Word";
                        break;
                    case VoiceServiceState.ListeningForSpeech:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Blue);
                        StatusText.Text = "Listening for Speech";
                        break;
                    case VoiceServiceState.ProcessingSpeech:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Orange);
                        StatusText.Text = "Processing Speech";
                        break;
                    case VoiceServiceState.Speaking:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Purple);
                        StatusText.Text = "Speaking";
                        break;
                }
            });
        }

        /// <summary>
        /// Handles speech recognition
        /// </summary>
        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            // Process the recognized speech
            _logger.LogInformation("Speech recognized: {Text}", e.Text);

            // TODO: Process the command
        }
    }
}
