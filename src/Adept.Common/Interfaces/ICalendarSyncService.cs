using Adept.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Delegate for handling calendar sync events
    /// </summary>
    /// <param name="changedEvents">The changed events</param>
    /// <param name="deletedEventIds">The deleted event IDs</param>
    public delegate Task CalendarSyncHandler(List<CalendarEvent> changedEvents, List<string> deletedEventIds);

    /// <summary>
    /// Interface for calendar synchronization service
    /// </summary>
    public interface ICalendarSyncService
    {
        /// <summary>
        /// Synchronizes all lesson plans with calendar events
        /// </summary>
        /// <returns>The number of synchronized events</returns>
        Task<int> SynchronizeAllLessonPlansAsync();

        /// <summary>
        /// Synchronizes a specific lesson plan with a calendar event
        /// </summary>
        /// <param name="lessonPlanId">The lesson plan ID</param>
        /// <returns>True if the synchronization was successful, false otherwise</returns>
        Task<bool> SynchronizeLessonPlanAsync(int lessonPlanId);

        /// <summary>
        /// Deletes a calendar event for a lesson plan
        /// </summary>
        /// <param name="lessonPlanId">The lesson plan ID</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        Task<bool> DeleteCalendarEventAsync(int lessonPlanId);

        /// <summary>
        /// Starts the two-way synchronization service
        /// </summary>
        Task StartTwoWaySyncAsync();

        /// <summary>
        /// Stops the two-way synchronization service
        /// </summary>
        Task StopTwoWaySyncAsync();

        /// <summary>
        /// Registers a sync handler for two-way synchronization
        /// </summary>
        /// <param name="handler">The handler to register</param>
        void RegisterSyncHandler(CalendarSyncHandler handler);

        /// <summary>
        /// Unregisters a sync handler
        /// </summary>
        /// <param name="handler">The handler to unregister</param>
        void UnregisterSyncHandler(CalendarSyncHandler handler);

        /// <summary>
        /// Triggers a manual synchronization from Google Calendar to the application
        /// </summary>
        Task SyncFromGoogleAsync();
    }
}
