using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.TestUtilities.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

// Use alias to avoid ambiguity with Moq.MockFactory
using TestMockFactory = Adept.TestUtilities.Helpers.MockFactory;

namespace Adept.TestUtilities.Fixtures
{
    /// <summary>
    /// A fixture for service tests that provides mock dependencies
    /// </summary>
    public class ServiceFixture : IAsyncLifetime, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }
        public IServiceCollection Services { get; }

        // Common mocks
        public Mock<ILogger<object>> MockLogger { get; }
        public Mock<IDatabaseContext> MockDatabaseContext { get; }
        public Mock<IFileSystemService> MockFileSystemService { get; }
        public Mock<ILlmProvider> MockLlmProvider { get; }
        public Mock<ISystemPromptRepository> MockSystemPromptRepository { get; }
        public Mock<ILessonResourceRepository> MockLessonResourceRepository { get; }
        public Mock<ILessonTemplateRepository> MockLessonTemplateRepository { get; }

        public ServiceFixture()
        {
            // Create mocks
            MockLogger = new Mock<ILogger<object>>();
            MockDatabaseContext = TestMockFactory.CreateMockDatabaseContext();
            MockFileSystemService = TestMockFactory.CreateMockFileSystemService();
            MockLlmProvider = TestMockFactory.CreateMockLlmProvider();
            MockSystemPromptRepository = TestMockFactory.CreateMockSystemPromptRepository();
            MockLessonResourceRepository = TestMockFactory.CreateMockLessonResourceRepository();
            MockLessonTemplateRepository = TestMockFactory.CreateMockLessonTemplateRepository();

            // Configure services
            Services = new ServiceCollection();
            ConfigureServices(Services);
            ServiceProvider = Services.BuildServiceProvider();
        }

        /// <summary>
        /// Configure the service collection with mock dependencies
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Register generic logger
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton(MockLogger.Object);

            // Register mock repositories
            services.AddSingleton(MockDatabaseContext.Object);
            services.AddSingleton(MockFileSystemService.Object);
            services.AddSingleton(MockLlmProvider.Object);
            services.AddSingleton(MockSystemPromptRepository.Object);
            services.AddSingleton(MockLessonResourceRepository.Object);
            services.AddSingleton(MockLessonTemplateRepository.Object);

            // Register mock repositories as their interfaces
            services.AddSingleton<ISystemPromptRepository>(MockSystemPromptRepository.Object);
            services.AddSingleton<ILessonResourceRepository>(MockLessonResourceRepository.Object);
            services.AddSingleton<ILessonTemplateRepository>(MockLessonTemplateRepository.Object);
        }

        /// <summary>
        /// Get a service from the service provider
        /// </summary>
        /// <typeparam name="T">The type of service to get</typeparam>
        /// <returns>The service instance</returns>
        public T GetService<T>() where T : class
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Create a mock logger for the specified type
        /// </summary>
        /// <typeparam name="T">The type that the logger is for</typeparam>
        /// <returns>A mock logger</returns>
        public Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return TestMockFactory.CreateMockLogger<T>();
        }

        /// <summary>
        /// Initialize the fixture
        /// </summary>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup the fixture
        /// </summary>
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public virtual void Dispose()
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
