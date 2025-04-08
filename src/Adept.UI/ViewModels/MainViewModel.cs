using Microsoft.Extensions.Logging;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// Main view model for the application
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogger<MainViewModel> _logger;

        /// <summary>
        /// Gets the chat view model
        /// </summary>
        public ChatViewModel ChatViewModel { get; }

        /// <summary>
        /// Gets the class view model
        /// </summary>
        public ClassViewModel ClassViewModel { get; }

        /// <summary>
        /// Gets the lesson planner view model
        /// </summary>
        public LessonPlannerViewModel LessonPlannerViewModel { get; }

        /// <summary>
        /// Gets the configuration view model
        /// </summary>
        public ConfigurationViewModel ConfigurationViewModel { get; }

        /// <summary>
        /// Gets the system status view model
        /// </summary>
        public SystemStatusViewModel SystemStatusViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class
        /// </summary>
        /// <param name="chatViewModel">The chat view model</param>
        /// <param name="classViewModel">The class view model</param>
        /// <param name="lessonPlannerViewModel">The lesson planner view model</param>
        /// <param name="configurationViewModel">The configuration view model</param>
        /// <param name="systemStatusViewModel">The system status view model</param>
        /// <param name="logger">The logger</param>
        public MainViewModel(
            ChatViewModel chatViewModel,
            ClassViewModel classViewModel,
            LessonPlannerViewModel lessonPlannerViewModel,
            ConfigurationViewModel configurationViewModel,
            SystemStatusViewModel systemStatusViewModel,
            ILogger<MainViewModel> logger)
        {
            ChatViewModel = chatViewModel;
            ClassViewModel = classViewModel;
            LessonPlannerViewModel = lessonPlannerViewModel;
            ConfigurationViewModel = configurationViewModel;
            SystemStatusViewModel = systemStatusViewModel;
            _logger = logger;

            _logger.LogInformation("MainViewModel initialized");
        }
    }
}
