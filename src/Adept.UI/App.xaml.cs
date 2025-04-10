using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Data.Database;
using Adept.Data.Repositories;
using Adept.Services.Calendar;
using Adept.Services.Configuration;
using Adept.Services.Database;
using Adept.Services.Extensions;
using Adept.Services.FileSystem;
using Adept.Services.Llm;
using Adept.Services.Llm.Providers;
using Adept.Services.Mcp;
using Adept.Services.OAuth;
using Adept.Services.Security;
using Adept.Services.Voice;
using Adept.UI.Controls;
using Adept.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;

namespace Adept.UI
{
    public partial class App : Application
    {
        // These fields are initialized in the OnStartup method before they're used
        private ServiceProvider _serviceProvider = null!;
        private IConfiguration _configuration = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            // Initialize database backup and maintenance
            var databaseInitializer = _serviceProvider.GetRequiredService<DatabaseBackupInitializer>();
            databaseInitializer.InitializeAsync().ConfigureAwait(false);

            // Check for test mode
            bool testMode = e.Args.Length > 0 && e.Args[0] == "--test";

            if (testMode)
            {
                RunTestMode();
                Shutdown();
                return;
            }

            // Start the application normally
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(configure =>
            {
                configure.AddSerilog(dispose: true);
            });

            // Add configuration
            services.AddSingleton(_configuration);

            // Add HTTP clients
            services.AddHttpClient();

            // Register Database Services
            services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();
            services.AddSingleton<IDatabaseIntegrityService, DatabaseIntegrityService>();
            services.AddSingleton<IDatabaseContext>(provider => new SqliteDatabaseContext(
                provider.GetRequiredService<IConfiguration>(),
                provider.GetRequiredService<ILogger<SqliteDatabaseContext>>(),
                provider.GetRequiredService<IDatabaseBackupService>()));

            // Register Core Services
            services.AddSingleton<ICryptographyService, CryptographyService>();
            services.AddSingleton<ISecureStorageService, SecureStorageService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // Register Repositories
            services.AddSingleton<IClassRepository, ClassRepository>();
            services.AddSingleton<IStudentRepository, StudentRepository>();
            services.AddSingleton<ILessonRepository, LessonRepository>();
            services.AddSingleton<ISystemPromptService, SystemPromptRepository>();
            services.AddSingleton<IConversationRepository, ConversationRepository>();

            // Register Voice Services
            services.AddSingleton<SimpleWakeWordDetector>();
            services.AddSingleton<VoskWakeWordDetector>();
            services.AddSingleton<SimpleSpeechToTextProvider>();
            services.AddSingleton<WhisperSpeechToTextProvider>();
            services.AddSingleton<GoogleSpeechToTextProvider>();
            services.AddSingleton<SimpleTextToSpeechProvider>();
            services.AddSingleton<FishAudioTextToSpeechProvider>();
            services.AddSingleton<OpenAiTextToSpeechProvider>();
            services.AddSingleton<GoogleTextToSpeechProvider>();
            services.AddSingleton<IVoiceProviderFactory, VoiceProviderFactory>();
            services.AddSingleton<IVoiceService, VoiceService>();

            // Register LLM Services
            services.AddHttpClient();
            services.AddSingleton<ILlmProvider, SimpleLlmProvider>();
            services.AddSingleton<ILlmProvider, OpenAiProvider>();
            services.AddSingleton<ILlmProvider, AnthropicProvider>();
            services.AddSingleton<ILlmProvider, GoogleProvider>();
            services.AddSingleton<ILlmProvider, MetaProvider>();
            services.AddSingleton<ILlmProvider, OpenRouterProvider>();
            services.AddSingleton<ILlmProvider, DeepSeekProvider>();
            services.AddSingleton<LlmToolIntegrationService>();
            services.AddSingleton<ILlmService, LlmService>();

            // Register Calendar Services
            services.AddCalendarServices();

            // Register File System Services
            services.AddFileSystemServices();

            // Register mock services for testing
            services.AddSingleton<ICalendarSyncService, MockCalendarSyncService>();
            services.AddSingleton<ILessonPlanService, MockLessonPlanService>();
            services.AddSingleton<IClassService, MockClassService>();

            // Register MCP Services
            services.AddSingleton<IMcpServerManager, McpServerManager>();
            services.AddSingleton<IMcpToolProvider, FileSystemToolProvider>();
            services.AddSingleton<IMcpToolProvider, CalendarToolProvider>();
            services.AddSingleton<IMcpToolProvider, WebSearchToolProvider>();
            services.AddSingleton<IMcpToolProvider, ExcelToolProvider>();
            services.AddSingleton<IMcpToolProvider, FetchToolProvider>();
            services.AddSingleton<IMcpToolProvider, SequentialThinkingToolProvider>();
            services.AddSingleton<IMcpToolProvider, PuppeteerToolProvider>();

            // Register Database Initialization
            services.AddSingleton<DatabaseBackupInitializer>();

            // Register UI Services
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IConfirmationService, ConfirmationService>();
            services.AddSingleton<NotificationControl>();

            // Register ViewModels
            services.AddSingleton<ViewModels.HomeViewModel>();
            services.AddSingleton<ViewModels.ChatViewModel>();
            services.AddSingleton<ViewModels.ClassViewModel>();
            services.AddSingleton<ViewModels.LessonPlannerViewModel>();
            services.AddSingleton<ViewModels.ConfigurationViewModel>();
            services.AddSingleton<ViewModels.SystemStatusViewModel>();
            services.AddSingleton<ViewModels.CalendarSettingsViewModel>();
            services.AddSingleton<ViewModels.NotificationsViewModel>();
            services.AddSingleton<ViewModels.MainViewModel>();

            // Register MainWindow
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup
            Log.CloseAndFlush();
            _serviceProvider.Dispose();

            base.OnExit(e);
        }

        /// <summary>
        /// Runs the application in test mode to verify functionality
        /// </summary>
        private void RunTestMode()
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Running in test mode");

            // Test the LessonPlannerViewModel
            var lessonPlannerViewModel = _serviceProvider.GetRequiredService<ViewModels.LessonPlannerViewModel>();
            logger.LogInformation("Testing LessonPlannerViewModel");

            // Test loading classes
            lessonPlannerViewModel.LoadClassesAsync().Wait();
            logger.LogInformation("Loaded {Count} classes", lessonPlannerViewModel.Classes.Count);

            // Test resource management
            if (lessonPlannerViewModel.Classes.Count > 0)
            {
                lessonPlannerViewModel.SelectedClass = lessonPlannerViewModel.Classes[0];
                logger.LogInformation("Selected class: {ClassName}", lessonPlannerViewModel.SelectedClass.Name);

                // Test loading lessons
                lessonPlannerViewModel.LoadLessonsForSelectedClassAsync().Wait();
                logger.LogInformation("Loaded {Count} lessons", lessonPlannerViewModel.Lessons.Count);

                // Test resource management
                if (lessonPlannerViewModel.Lessons.Count > 0)
                {
                    lessonPlannerViewModel.SelectedLesson = lessonPlannerViewModel.Lessons[0];
                    logger.LogInformation("Selected lesson: {LessonTitle}", lessonPlannerViewModel.SelectedLesson.Title);

                    // Test loading resources
                    lessonPlannerViewModel.LoadResourcesAsync().Wait();
                    logger.LogInformation("Resources count: {Count}", lessonPlannerViewModel.Resources.Count);

                    // Test resource details
                    foreach (var resource in lessonPlannerViewModel.Resources)
                    {
                        logger.LogInformation("Resource: {ResourceName}, Type: {ResourceType}", resource.Name, resource.Type);
                    }
                }
                else
                {
                    logger.LogInformation("No lessons available for testing resource management");
                }
            }
            else
            {
                logger.LogInformation("No classes available for testing");
            }

            logger.LogInformation("Test mode completed successfully");
        }
    }
}
