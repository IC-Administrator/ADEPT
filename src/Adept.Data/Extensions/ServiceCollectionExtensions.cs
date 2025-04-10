using Adept.Data.Database;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Adept.Data.Extensions
{
    /// <summary>
    /// Extension methods for service collection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds database services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
        {
            // Register database provider
            services.AddSingleton<IDatabaseProvider, SqliteDatabaseProvider>();

            return services;
        }
    }
}
