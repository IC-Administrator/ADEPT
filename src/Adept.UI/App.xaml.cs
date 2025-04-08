using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Data.Database;
using Adept.Data.Repositories;
using Adept.Services.Configuration;
using Adept.Services.Database;
using Adept.Services.Llm;
using Adept.Services.Llm.Providers;
using Adept.Services.Mcp;
using Adept.Services.Security;
using Adept.Services.Voice;
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
        private ServiceProvider _serviceProvider;
        private IConfiguration _configuration;

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

            // Start the application
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

            // Register MCP Services
            services.AddSingleton<IMcpServerManager, McpServerManager>();

            // Register Database Initialization
            services.AddSingleton<DatabaseBackupInitializer>();

            // Register ViewModels
            services.AddSingleton<ViewModels.HomeViewModel>();
            services.AddSingleton<ViewModels.ChatViewModel>();
            services.AddSingleton<ViewModels.ClassViewModel>();
            services.AddSingleton<ViewModels.LessonPlannerViewModel>();
            services.AddSingleton<ViewModels.ConfigurationViewModel>();
            services.AddSingleton<ViewModels.SystemStatusViewModel>();
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
    }
}
