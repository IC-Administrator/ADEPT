using Adept.Core.Interfaces;
using Adept.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Media;

namespace Adept.UI
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly IVoiceService _voiceService;

        /// <summary>
        /// Gets the main view model
        /// </summary>
        public MainViewModel MainViewModel { get; }

        /// <summary>
        /// Gets the home view model
        /// </summary>
        public HomeViewModel HomeViewModel { get; }

        /// <summary>
        /// Gets or sets the selected tab index
        /// </summary>
        public int SelectedTabIndex { get; set; }

        public MainWindow(ILogger<MainWindow> logger, IVoiceService voiceService, MainViewModel mainViewModel, HomeViewModel homeViewModel)
        {
            _logger = logger;
            _voiceService = voiceService;
            MainViewModel = mainViewModel;
            HomeViewModel = homeViewModel;

            InitializeComponent();

            // Set the data context
            DataContext = this;

            // Set the data context for the tabs
            ChatTab.DataContext = MainViewModel;
            ClassesTab.DataContext = MainViewModel;
            LessonPlannerTab.DataContext = MainViewModel;
            ConfigurationTab.DataContext = MainViewModel;
            SystemStatusTab.DataContext = MainViewModel;

            // Subscribe to voice service events
            _voiceService.StateChanged += OnVoiceServiceStateChanged;

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
