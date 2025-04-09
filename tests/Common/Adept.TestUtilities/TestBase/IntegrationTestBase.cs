using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Adept.TestUtilities.TestBase
{
    /// <summary>
    /// Base class for integration tests that provides common setup and teardown functionality
    /// </summary>
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IServiceCollection Services;
        
        protected IntegrationTestBase()
        {
            Services = new ServiceCollection();
            ConfigureServices(Services);
            ServiceProvider = Services.BuildServiceProvider();
        }

        /// <summary>
        /// Configure the service collection for the test
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        protected abstract void ConfigureServices(IServiceCollection services);

        /// <summary>
        /// Get a service from the service provider
        /// </summary>
        /// <typeparam name="T">The type of service to get</typeparam>
        /// <returns>The service instance</returns>
        protected T GetService<T>() where T : class
        {
            return ServiceProvider.GetRequiredService<T>();
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
