using System.Threading.Tasks;

namespace Adept.Common.Interfaces
{
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
    }
}
