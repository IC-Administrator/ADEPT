using Adept.Common.Interfaces;
using Adept.Services.Calendar;
using Adept.Services.OAuth;
using Microsoft.Extensions.DependencyInjection;

namespace Adept.Services.Extensions
{
    /// <summary>
    /// Extension methods for registering calendar services
    /// </summary>
    public static class CalendarServiceExtensions
    {
        /// <summary>
        /// Adds calendar services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddCalendarServices(this IServiceCollection services)
        {
            // Register the OAuth service
            services.AddHttpClient();
            services.AddSingleton<IOAuthService, GoogleOAuthService>();

            // Register the calendar service
            services.AddSingleton<ICalendarService, GoogleCalendarService>();

            // Register the calendar sync service
            services.AddSingleton<ICalendarSyncService, CalendarSyncService>();

            return services;
        }
    }
}
