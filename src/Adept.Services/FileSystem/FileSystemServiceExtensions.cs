using Adept.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Adept.Services.FileSystem
{
    /// <summary>
    /// Extension methods for registering file system services
    /// </summary>
    public static class FileSystemServiceExtensions
    {
        /// <summary>
        /// Adds file system services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddFileSystemServices(this IServiceCollection services)
        {
            // Register the services
            services.AddSingleton<IFileSystemService, ScratchpadService>();
            services.AddSingleton<MarkdownProcessor>();
            services.AddSingleton<FileOrganizer>();
            services.AddSingleton<FileSearchService>();

            return services;
        }
    }
}
