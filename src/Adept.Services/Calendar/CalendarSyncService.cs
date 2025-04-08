using Adept.Common.Interfaces;
using Adept.Common.Models;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Services.Calendar
{
    /// <summary>
    /// Service for synchronizing lesson plans with calendar events
    /// </summary>
    public class CalendarSyncService : ICalendarSyncService
    {
        private readonly ICalendarService _calendarService;
        private readonly ILessonPlanService _lessonPlanService;
        private readonly IClassService _classService;
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<CalendarSyncService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarSyncService"/> class
        /// </summary>
        /// <param name="calendarService">The calendar service</param>
        /// <param name="lessonPlanService">The lesson plan service</param>
        /// <param name="classService">The class service</param>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public CalendarSyncService(
            ICalendarService calendarService,
            ILessonPlanService lessonPlanService,
            IClassService classService,
            ISecureStorageService secureStorageService,
            ILogger<CalendarSyncService> logger)
        {
            _calendarService = calendarService;
            _lessonPlanService = lessonPlanService;
            _classService = classService;
            _secureStorageService = secureStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Synchronizes all lesson plans with calendar events
        /// </summary>
        /// <returns>The number of synchronized events</returns>
        public async Task<int> SynchronizeAllLessonPlansAsync()
        {
            try
            {
                // Check if the calendar service is authenticated
                var isAuthenticated = await _calendarService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    _logger.LogWarning("Calendar service is not authenticated. Skipping synchronization.");
                    return 0;
                }

                // Get all lesson plans
                var lessonPlans = await _lessonPlanService.GetAllLessonPlansAsync();
                if (lessonPlans == null || !lessonPlans.Any())
                {
                    _logger.LogInformation("No lesson plans found to synchronize.");
                    return 0;
                }

                // Get all classes
                var classes = await _classService.GetAllClassesAsync();
                if (classes == null || !classes.Any())
                {
                    _logger.LogWarning("No classes found. Skipping synchronization.");
                    return 0;
                }

                // Create a dictionary of classes for quick lookup
                var classDictionary = classes.ToDictionary(c => c.Id);

                // Synchronize each lesson plan
                int syncCount = 0;
                foreach (var lessonPlan in lessonPlans)
                {
                    try
                    {
                        // Skip lesson plans without a class ID
                        if (lessonPlan.ClassId == 0 || !classDictionary.TryGetValue(lessonPlan.ClassId, out var classInfo))
                        {
                            _logger.LogWarning("Lesson plan {LessonPlanId} has no valid class ID. Skipping.", lessonPlan.Id);
                            continue;
                        }

                        // Calculate the start and end times
                        if (!TryGetLessonDateTime(lessonPlan, classInfo, out var startDateTime, out var endDateTime))
                        {
                            _logger.LogWarning("Could not determine start/end time for lesson plan {LessonPlanId}. Skipping.", lessonPlan.Id);
                            continue;
                        }

                        // Format the event summary and description
                        var summary = $"{classInfo.Subject} - {lessonPlan.Title}";
                        var description = FormatLessonDescription(lessonPlan);

                        // Get the settings from secure storage
                        var colorId = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_color_id");
                        var useDefaultRemindersStr = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_use_default_reminders");
                        var reminderMinutesStr = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_reminder_minutes");
                        var reminderMethod = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_reminder_method");
                        var visibility = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_visibility");
                        var attendeesJson = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_attendees");

                        // Parse the settings
                        bool useDefaultReminders = true;
                        if (!string.IsNullOrEmpty(useDefaultRemindersStr) && bool.TryParse(useDefaultRemindersStr, out var parsedUseDefaultReminders))
                        {
                            useDefaultReminders = parsedUseDefaultReminders;
                        }

                        int reminderMinutes = 30;
                        if (!string.IsNullOrEmpty(reminderMinutesStr) && int.TryParse(reminderMinutesStr, out var parsedReminderMinutes))
                        {
                            reminderMinutes = parsedReminderMinutes;
                        }

                        if (string.IsNullOrEmpty(reminderMethod))
                        {
                            reminderMethod = "popup";
                        }

                        // Create reminders if not using default
                        CalendarReminders? reminders = null;
                        if (!useDefaultReminders)
                        {
                            reminders = new CalendarReminders
                            {
                                UseDefault = false,
                                Overrides = new List<CalendarReminder>
                                {
                                    new CalendarReminder
                                    {
                                        Method = reminderMethod,
                                        Minutes = reminderMinutes
                                    }
                                }
                            };
                        }

                        // Parse attendees
                        List<CalendarAttendee>? attendees = null;
                        if (!string.IsNullOrEmpty(attendeesJson))
                        {
                            attendees = System.Text.Json.JsonSerializer.Deserialize<List<CalendarAttendee>>(attendeesJson);
                        }

                        // If the lesson plan already has a calendar event ID, update it
                        if (!string.IsNullOrEmpty(lessonPlan.CalendarEventId))
                        {
                            var success = await _calendarService.UpdateEventAsync(
                                lessonPlan.CalendarEventId,
                                summary,
                                description,
                                classInfo.Location,
                                startDateTime,
                                endDateTime,
                                "Europe/London",
                                colorId,
                                reminders,
                                attendees,
                                null,
                                visibility);

                            if (success)
                            {
                                _logger.LogInformation("Updated calendar event for lesson plan {LessonPlanId}", lessonPlan.Id);
                                syncCount++;
                            }
                            else
                            {
                                _logger.LogWarning("Failed to update calendar event for lesson plan {LessonPlanId}", lessonPlan.Id);
                            }
                        }
                        // Otherwise, create a new event
                        else
                        {
                            var eventId = await _calendarService.CreateEventAsync(
                                summary,
                                description,
                                classInfo.Location,
                                startDateTime,
                                endDateTime,
                                "Europe/London",
                                colorId,
                                reminders,
                                attendees,
                                null,
                                visibility);

                            if (!string.IsNullOrEmpty(eventId))
                            {
                                // Update the lesson plan with the event ID
                                lessonPlan.CalendarEventId = eventId;
                                await _lessonPlanService.UpdateLessonPlanAsync(lessonPlan);

                                _logger.LogInformation("Created calendar event for lesson plan {LessonPlanId}", lessonPlan.Id);
                                syncCount++;
                            }
                            else
                            {
                                _logger.LogWarning("Failed to create calendar event for lesson plan {LessonPlanId}", lessonPlan.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error synchronizing lesson plan {LessonPlanId}", lessonPlan.Id);
                    }
                }

                _logger.LogInformation("Synchronized {Count} lesson plans with calendar events", syncCount);
                return syncCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing lesson plans with calendar events");
                return 0;
            }
        }

        /// <summary>
        /// Synchronizes a specific lesson plan with a calendar event
        /// </summary>
        /// <param name="lessonPlanId">The lesson plan ID</param>
        /// <returns>True if the synchronization was successful, false otherwise</returns>
        public async Task<bool> SynchronizeLessonPlanAsync(int lessonPlanId)
        {
            try
            {
                // Check if the calendar service is authenticated
                var isAuthenticated = await _calendarService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    _logger.LogWarning("Calendar service is not authenticated. Skipping synchronization.");
                    return false;
                }

                // Get the lesson plan
                var lessonPlan = await _lessonPlanService.GetLessonPlanByIdAsync(lessonPlanId);
                if (lessonPlan == null)
                {
                    _logger.LogWarning("Lesson plan {LessonPlanId} not found.", lessonPlanId);
                    return false;
                }

                // Get the class
                var classInfo = await _classService.GetClassByIdAsync(lessonPlan.ClassId);
                if (classInfo == null)
                {
                    _logger.LogWarning("Class {ClassId} not found for lesson plan {LessonPlanId}.", lessonPlan.ClassId, lessonPlanId);
                    return false;
                }

                // Calculate the start and end times
                if (!TryGetLessonDateTime(lessonPlan, classInfo, out var startDateTime, out var endDateTime))
                {
                    _logger.LogWarning("Could not determine start/end time for lesson plan {LessonPlanId}.", lessonPlanId);
                    return false;
                }

                // Format the event summary and description
                var summary = $"{classInfo.Subject} - {lessonPlan.Title}";
                var description = FormatLessonDescription(lessonPlan);

                // Get the settings from secure storage
                var colorId = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_color_id");
                var useDefaultRemindersStr = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_use_default_reminders");
                var reminderMinutesStr = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_reminder_minutes");
                var reminderMethod = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_reminder_method");
                var visibility = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_visibility");
                var attendeesJson = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_attendees");

                // Parse the settings
                bool useDefaultReminders = true;
                if (!string.IsNullOrEmpty(useDefaultRemindersStr) && bool.TryParse(useDefaultRemindersStr, out var parsedUseDefaultReminders))
                {
                    useDefaultReminders = parsedUseDefaultReminders;
                }

                int reminderMinutes = 30;
                if (!string.IsNullOrEmpty(reminderMinutesStr) && int.TryParse(reminderMinutesStr, out var parsedReminderMinutes))
                {
                    reminderMinutes = parsedReminderMinutes;
                }

                if (string.IsNullOrEmpty(reminderMethod))
                {
                    reminderMethod = "popup";
                }

                // Create reminders if not using default
                CalendarReminders? reminders = null;
                if (!useDefaultReminders)
                {
                    reminders = new CalendarReminders
                    {
                        UseDefault = false,
                        Overrides = new List<CalendarReminder>
                        {
                            new CalendarReminder
                            {
                                Method = reminderMethod,
                                Minutes = reminderMinutes
                            }
                        }
                    };
                }

                // Parse attendees
                List<CalendarAttendee>? attendees = null;
                if (!string.IsNullOrEmpty(attendeesJson))
                {
                    attendees = System.Text.Json.JsonSerializer.Deserialize<List<CalendarAttendee>>(attendeesJson);
                }

                // If the lesson plan already has a calendar event ID, update it
                if (!string.IsNullOrEmpty(lessonPlan.CalendarEventId))
                {
                    var success = await _calendarService.UpdateEventAsync(
                        lessonPlan.CalendarEventId,
                        summary,
                        description,
                        classInfo.Location,
                        startDateTime,
                        endDateTime,
                        "Europe/London",
                        colorId,
                        reminders,
                        attendees,
                        null,
                        visibility);

                    if (success)
                    {
                        _logger.LogInformation("Updated calendar event for lesson plan {LessonPlanId}", lessonPlanId);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update calendar event for lesson plan {LessonPlanId}", lessonPlanId);
                        return false;
                    }
                }
                // Otherwise, create a new event
                else
                {
                    var eventId = await _calendarService.CreateEventAsync(
                        summary,
                        description,
                        classInfo.Location,
                        startDateTime,
                        endDateTime,
                        "Europe/London",
                        colorId,
                        reminders,
                        attendees,
                        null,
                        visibility);

                    if (!string.IsNullOrEmpty(eventId))
                    {
                        // Update the lesson plan with the event ID
                        lessonPlan.CalendarEventId = eventId;
                        await _lessonPlanService.UpdateLessonPlanAsync(lessonPlan);

                        _logger.LogInformation("Created calendar event for lesson plan {LessonPlanId}", lessonPlanId);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create calendar event for lesson plan {LessonPlanId}", lessonPlanId);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing lesson plan {LessonPlanId}", lessonPlanId);
                return false;
            }
        }

        /// <summary>
        /// Deletes a calendar event for a lesson plan
        /// </summary>
        /// <param name="lessonPlanId">The lesson plan ID</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public async Task<bool> DeleteCalendarEventAsync(int lessonPlanId)
        {
            try
            {
                // Check if the calendar service is authenticated
                var isAuthenticated = await _calendarService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    _logger.LogWarning("Calendar service is not authenticated. Skipping deletion.");
                    return false;
                }

                // Get the lesson plan
                var lessonPlan = await _lessonPlanService.GetLessonPlanByIdAsync(lessonPlanId);
                if (lessonPlan == null)
                {
                    _logger.LogWarning("Lesson plan {LessonPlanId} not found.", lessonPlanId);
                    return false;
                }

                // If the lesson plan has a calendar event ID, delete it
                if (!string.IsNullOrEmpty(lessonPlan.CalendarEventId))
                {
                    var success = await _calendarService.DeleteEventAsync(lessonPlan.CalendarEventId);
                    if (success)
                    {
                        // Update the lesson plan to remove the event ID
                        lessonPlan.CalendarEventId = null;
                        await _lessonPlanService.UpdateLessonPlanAsync(lessonPlan);

                        _logger.LogInformation("Deleted calendar event for lesson plan {LessonPlanId}", lessonPlanId);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete calendar event for lesson plan {LessonPlanId}", lessonPlanId);
                        return false;
                    }
                }
                else
                {
                    _logger.LogInformation("Lesson plan {LessonPlanId} has no calendar event ID.", lessonPlanId);
                    return true; // Nothing to delete
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar event for lesson plan {LessonPlanId}", lessonPlanId);
                return false;
            }
        }

        /// <summary>
        /// Tries to get the start and end date/time for a lesson
        /// </summary>
        /// <param name="lessonPlan">The lesson plan</param>
        /// <param name="classInfo">The class information</param>
        /// <param name="startDateTime">The start date/time</param>
        /// <param name="endDateTime">The end date/time</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool TryGetLessonDateTime(LessonPlan lessonPlan, Class classInfo, out DateTime startDateTime, out DateTime endDateTime)
        {
            startDateTime = DateTime.MinValue;
            endDateTime = DateTime.MinValue;

            try
            {
                // Parse the lesson date
                if (!DateTime.TryParse(lessonPlan.Date, out var lessonDate))
                {
                    _logger.LogWarning("Invalid date format for lesson plan {LessonPlanId}: {Date}", lessonPlan.Id, lessonPlan.Date);
                    return false;
                }

                // Parse the class start time
                if (!TimeSpan.TryParse(classInfo.StartTime, out var startTime))
                {
                    _logger.LogWarning("Invalid start time format for class {ClassId}: {StartTime}", classInfo.Id, classInfo.StartTime);
                    return false;
                }

                // Calculate the start and end date/time
                startDateTime = lessonDate.Add(startTime);
                endDateTime = startDateTime.AddMinutes(classInfo.DurationMinutes);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating date/time for lesson plan {LessonPlanId}", lessonPlan.Id);
                return false;
            }
        }

        /// <summary>
        /// Formats the lesson description for a calendar event
        /// </summary>
        /// <param name="lessonPlan">The lesson plan</param>
        /// <returns>The formatted description</returns>
        private string FormatLessonDescription(LessonPlan lessonPlan)
        {
            var description = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(lessonPlan.LearningObjectives))
            {
                description.AppendLine("Learning Objectives:");
                description.AppendLine(lessonPlan.LearningObjectives);
                description.AppendLine();
            }

            if (!string.IsNullOrEmpty(lessonPlan.Description))
            {
                description.AppendLine("Description:");
                description.AppendLine(lessonPlan.Description);
                description.AppendLine();
            }

            if (!string.IsNullOrEmpty(lessonPlan.LessonComponents))
            {
                description.AppendLine("Lesson Components:");
                description.AppendLine(lessonPlan.LessonComponents);
            }

            return description.ToString();
        }
    }
}
