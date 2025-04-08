using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Data.Database;
using Adept.Data.Repositories;
using Adept.Services.Configuration;
using Adept.Services.Security;
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

            // Register Database
            services.AddSingleton<IDatabaseContext, SqliteDatabaseContext>();

            // Register Core Services
            services.AddSingleton<ICryptographyService, CryptographyService>();
            services.AddSingleton<ISecureStorageService, SecureStorageService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // Register Repositories
            services.AddSingleton<IClassRepository, ClassRepository>();
            // TODO: Register other repositories

            // Register Services
            // TODO: Register voice, LLM, and other services

            // Register ViewModels
            // TODO: Register ViewModels

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
