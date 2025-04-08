using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Services.Calendar
{
    /// <summary>
    /// Mock implementation of the calendar synchronization service for testing
    /// </summary>
    public class MockCalendarSyncService : ICalendarSyncService
    {
        private readonly ILogger<MockCalendarSyncService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockCalendarSyncService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public MockCalendarSyncService(ILogger<MockCalendarSyncService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Synchronizes all lesson plans with calendar events
        /// </summary>
        /// <returns>The number of synchronized events</returns>
        public Task<int> SynchronizeAllLessonPlansAsync()
        {
            _logger.LogInformation("Mock: Synchronizing all lesson plans");
            return Task.FromResult(0);
        }

        /// <summary>
        /// Synchronizes a specific lesson plan with a calendar event
        /// </summary>
        /// <param name="lessonPlanId">The lesson plan ID</param>
        /// <returns>True if the synchronization was successful, false otherwise</returns>
        public Task<bool> SynchronizeLessonPlanAsync(int lessonPlanId)
        {
            _logger.LogInformation("Mock: Synchronizing lesson plan {LessonPlanId}", lessonPlanId);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Deletes a calendar event for a lesson plan
        /// </summary>
        /// <param name="lessonPlanId">The lesson plan ID</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public Task<bool> DeleteCalendarEventAsync(int lessonPlanId)
        {
            _logger.LogInformation("Mock: Deleting calendar event for lesson plan {LessonPlanId}", lessonPlanId);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Starts the two-way synchronization service
        /// </summary>
        public Task StartTwoWaySyncAsync()
        {
            _logger.LogInformation("Mock: Starting two-way calendar sync service");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the two-way synchronization service
        /// </summary>
        public Task StopTwoWaySyncAsync()
        {
            _logger.LogInformation("Mock: Stopping two-way calendar sync service");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Registers a sync handler
        /// </summary>
        /// <param name="handler">The handler to register</param>
        public void RegisterSyncHandler(CalendarSyncHandler handler)
        {
            _logger.LogInformation("Mock: Registering calendar sync handler");
        }

        /// <summary>
        /// Unregisters a sync handler
        /// </summary>
        /// <param name="handler">The handler to unregister</param>
        public void UnregisterSyncHandler(CalendarSyncHandler handler)
        {
            _logger.LogInformation("Mock: Unregistering calendar sync handler");
        }

        /// <summary>
        /// Triggers a manual synchronization from Google Calendar to the application
        /// </summary>
        public Task SyncFromGoogleAsync()
        {
            _logger.LogInformation("Mock: Syncing from Google Calendar");
            return Task.CompletedTask;
        }
    }
}
