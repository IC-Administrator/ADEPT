using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.UI.Commands;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// View model for the Lesson Planner tab
    /// </summary>
    public class LessonPlannerViewModel : ViewModelBase
    {
        private readonly IClassRepository _classRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly ILlmService _llmService;
        private readonly ICalendarSyncService _calendarSyncService;
        private readonly ILogger<LessonPlannerViewModel> _logger;
        private bool _isBusy;
        private Class? _selectedClass;
        private LessonPlan? _selectedLesson;
        private string _currentDate;
        private int _selectedTimeSlot;
        private string _lessonTitle = string.Empty;
        private string _learningObjectives = string.Empty;
        private string _lessonComponents = string.Empty;

        /// <summary>
        /// Gets or sets whether the view model is busy
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the selected class
        /// </summary>
        public Class? SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (SetProperty(ref _selectedClass, value))
                {
                    LoadLessonsForSelectedClassAsync().ConfigureAwait(false);
                    OnPropertyChanged(nameof(IsClassSelected));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected lesson
        /// </summary>
        public LessonPlan? SelectedLesson
        {
            get => _selectedLesson;
            set
            {
                if (SetProperty(ref _selectedLesson, value))
                {
                    if (_selectedLesson != null)
                    {
                        _lessonTitle = _selectedLesson.Title;
                        _learningObjectives = _selectedLesson.LearningObjectives ?? string.Empty;
                        _lessonComponents = _selectedLesson.ComponentsJson ?? string.Empty;
                        OnPropertyChanged(nameof(LessonTitle));
                        OnPropertyChanged(nameof(LearningObjectives));
                        OnPropertyChanged(nameof(LessonComponents));
                    }

                    OnPropertyChanged(nameof(IsLessonSelected));
                    OnPropertyChanged(nameof(CanEditLesson));
                    OnPropertyChanged(nameof(CanDeleteLesson));
                    OnPropertyChanged(nameof(CanGenerateLesson));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current date
        /// </summary>
        public string CurrentDate
        {
            get => _currentDate;
            set
            {
                if (SetProperty(ref _currentDate, value))
                {
                    LoadLessonsForSelectedClassAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected time slot
        /// </summary>
        public int SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set => SetProperty(ref _selectedTimeSlot, value);
        }

        /// <summary>
        /// Gets or sets the lesson title
        /// </summary>
        public string LessonTitle
        {
            get => _lessonTitle;
            set => SetProperty(ref _lessonTitle, value);
        }

        /// <summary>
        /// Gets or sets the learning objectives
        /// </summary>
        public string LearningObjectives
        {
            get => _learningObjectives;
            set => SetProperty(ref _learningObjectives, value);
        }

        /// <summary>
        /// Gets or sets the lesson components
        /// </summary>
        public string LessonComponents
        {
            get => _lessonComponents;
            set => SetProperty(ref _lessonComponents, value);
        }

        /// <summary>
        /// Gets whether a class is selected
        /// </summary>
        public bool IsClassSelected => SelectedClass != null;

        /// <summary>
        /// Gets whether a lesson is selected
        /// </summary>
        public bool IsLessonSelected => SelectedLesson != null;

        /// <summary>
        /// Gets whether a lesson can be edited
        /// </summary>
        public bool CanEditLesson => IsLessonSelected;

        /// <summary>
        /// Gets whether a lesson can be deleted
        /// </summary>
        public bool CanDeleteLesson => IsLessonSelected;

        /// <summary>
        /// Gets whether a lesson can be generated
        /// </summary>
        public bool CanGenerateLesson => IsClassSelected;

        /// <summary>
        /// Gets the classes
        /// </summary>
        public ObservableCollection<Class> Classes { get; } = new ObservableCollection<Class>();

        /// <summary>
        /// Gets the lessons for the selected class
        /// </summary>
        public ObservableCollection<LessonPlan> Lessons { get; } = new ObservableCollection<LessonPlan>();

        /// <summary>
        /// Gets the time slots
        /// </summary>
        public ObservableCollection<string> TimeSlots { get; } = new ObservableCollection<string>
        {
            "Period 1 (9:00 - 10:00)",
            "Period 2 (10:00 - 11:00)",
            "Period 3 (11:15 - 12:15)",
            "Period 4 (13:00 - 14:00)",
            "Period 5 (14:00 - 15:00)"
        };

        /// <summary>
        /// Gets the add lesson command
        /// </summary>
        public ICommand AddLessonCommand { get; }

        /// <summary>
        /// Gets the edit lesson command
        /// </summary>
        public ICommand EditLessonCommand { get; }

        /// <summary>
        /// Gets the delete lesson command
        /// </summary>
        public ICommand DeleteLessonCommand { get; }

        /// <summary>
        /// Gets the generate lesson command
        /// </summary>
        public ICommand GenerateLessonCommand { get; }

        /// <summary>
        /// Gets the save lesson command
        /// </summary>
        public ICommand SaveLessonCommand { get; }

        /// <summary>
        /// Gets the refresh command
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Gets the previous day command
        /// </summary>
        public ICommand PreviousDayCommand { get; }

        /// <summary>
        /// Gets the next day command
        /// </summary>
        public ICommand NextDayCommand { get; }

        /// <summary>
        /// Gets the add to calendar command
        /// </summary>
        public ICommand AddToCalendarCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LessonPlannerViewModel"/> class
        /// </summary>
        /// <param name="classRepository">The class repository</param>
        /// <param name="lessonRepository">The lesson repository</param>
        /// <param name="llmService">The LLM service</param>
        /// <param name="calendarSyncService">The calendar sync service</param>
        /// <param name="logger">The logger</param>
        public LessonPlannerViewModel(
            IClassRepository classRepository,
            ILessonRepository lessonRepository,
            ILlmService llmService,
            ICalendarSyncService calendarSyncService,
            ILogger<LessonPlannerViewModel> logger)
        {
            _classRepository = classRepository;
            _lessonRepository = lessonRepository;
            _llmService = llmService;
            _calendarSyncService = calendarSyncService;
            _logger = logger;
            _currentDate = DateTime.Today.ToString("yyyy-MM-dd");

            AddLessonCommand = new RelayCommand(AddLessonAsync, () => IsClassSelected);
            EditLessonCommand = new RelayCommand(EditLessonAsync, () => CanEditLesson);
            DeleteLessonCommand = new RelayCommand(DeleteLessonAsync, () => CanDeleteLesson);
            GenerateLessonCommand = new RelayCommand(GenerateLessonAsync, () => CanGenerateLesson);
            SaveLessonCommand = new RelayCommand(SaveLessonAsync, () => IsLessonSelected);
            RefreshCommand = new RelayCommand(RefreshAsync);
            PreviousDayCommand = new RelayCommand(PreviousDayAsync);
            NextDayCommand = new RelayCommand(NextDayAsync);
            AddToCalendarCommand = new RelayCommand(AddToCalendarAsync, () => IsLessonSelected);

            // Load classes
            LoadClassesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads all classes
        /// </summary>
        private async Task LoadClassesAsync()
        {
            try
            {
                IsBusy = true;
                Classes.Clear();

                var classes = await _classRepository.GetAllClassesAsync();
                foreach (var cls in classes.OrderBy(c => c.Name))
                {
                    Classes.Add(cls);
                }

                _logger.LogInformation("Loaded {Count} classes", Classes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading classes");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Loads lessons for the selected class and date
        /// </summary>
        private async Task LoadLessonsForSelectedClassAsync()
        {
            try
            {
                if (SelectedClass == null)
                {
                    Lessons.Clear();
                    return;
                }

                IsBusy = true;
                Lessons.Clear();

                var lessons = await _lessonRepository.GetLessonsByClassIdAsync(SelectedClass.ClassId);

                // Filter by date
                lessons = lessons.Where(l => l.Date == CurrentDate);

                foreach (var lesson in lessons.OrderBy(l => l.TimeSlot))
                {
                    Lessons.Add(lesson);
                }

                _logger.LogInformation("Loaded {Count} lessons for class {ClassName} on {Date}",
                    Lessons.Count, SelectedClass.Name, CurrentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lessons for class {ClassId}", SelectedClass?.ClassId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Adds a new lesson
        /// </summary>
        private async void AddLessonAsync()
        {
            if (SelectedClass == null)
            {
                return;
            }

            try
            {
                // Check if a lesson already exists for this time slot
                var existingLesson = Lessons.FirstOrDefault(l => l.TimeSlot == SelectedTimeSlot);
                if (existingLesson != null)
                {
                    _logger.LogWarning("A lesson already exists for this time slot");
                    return;
                }

                // Create a new lesson
                var newLesson = new LessonPlan
                {
                    ClassId = SelectedClass.ClassId,
                    Date = CurrentDate,
                    TimeSlot = SelectedTimeSlot,
                    Title = "New Lesson",
                    LearningObjectives = "Enter learning objectives here",
                    ComponentsJson = "[]"
                };

                IsBusy = true;
                await _lessonRepository.AddLessonAsync(newLesson);
                Lessons.Add(newLesson);
                SelectedLesson = newLesson;

                _logger.LogInformation("Added new lesson: {LessonTitle} for class {ClassName} on {Date}",
                    newLesson.Title, SelectedClass.Name, CurrentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding lesson for class {ClassId}", SelectedClass.ClassId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Edits the selected lesson
        /// </summary>
        private async void EditLessonAsync()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            try
            {
                // In a real implementation, this would show a dialog to edit lesson details
                SelectedLesson.Title = LessonTitle;
                SelectedLesson.LearningObjectives = LearningObjectives;
                SelectedLesson.ComponentsJson = LessonComponents;
                SelectedLesson.UpdatedAt = DateTime.UtcNow;

                IsBusy = true;
                await _lessonRepository.UpdateLessonAsync(SelectedLesson);

                // Refresh the view
                OnPropertyChanged(nameof(SelectedLesson));

                _logger.LogInformation("Updated lesson: {LessonTitle}", SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Deletes the selected lesson
        /// </summary>
        private async void DeleteLessonAsync()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            try
            {
                // In a real implementation, this would show a confirmation dialog
                var lessonToDelete = SelectedLesson;

                IsBusy = true;

                // Delete the calendar event if it exists
                if (!string.IsNullOrEmpty(lessonToDelete.CalendarEventId))
                {
                    await _calendarSyncService.DeleteCalendarEventAsync(lessonToDelete.LessonId);
                    _logger.LogInformation("Deleted calendar event for lesson: {LessonTitle}", lessonToDelete.Title);
                }

                // Delete the lesson
                await _lessonRepository.DeleteLessonAsync(lessonToDelete.LessonId);
                Lessons.Remove(lessonToDelete);
                SelectedLesson = null;

                _logger.LogInformation("Deleted lesson: {LessonTitle}", lessonToDelete.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson {LessonId}", SelectedLesson?.LessonId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Generates a lesson using the LLM
        /// </summary>
        private async void GenerateLessonAsync()
        {
            if (SelectedClass == null)
            {
                return;
            }

            try
            {
                IsBusy = true;

                // Create a prompt for the LLM
                var prompt = $"Create a lesson plan for a {SelectedClass.GradeLevel} {SelectedClass.Subject} class. " +
                             $"The lesson should be for {TimeSlots[SelectedTimeSlot]}. " +
                             $"Include a title, learning objectives, and a detailed lesson structure with timings, " +
                             $"activities, and resources needed.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);

                // Parse the response
                var content = response.Message.Content;

                // Extract title and learning objectives (this is a simple implementation)
                var titleMatch = System.Text.RegularExpressions.Regex.Match(content, @"Title:\s*(.+)");
                var objectivesMatch = System.Text.RegularExpressions.Regex.Match(content, @"Learning Objectives:(.*?)(?=\n\n|\z)", System.Text.RegularExpressions.RegexOptions.Singleline);

                var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "Generated Lesson";
                var objectives = objectivesMatch.Success ? objectivesMatch.Groups[1].Value.Trim() : "";

                // Create a new lesson
                var newLesson = new LessonPlan
                {
                    ClassId = SelectedClass.ClassId,
                    Date = CurrentDate,
                    TimeSlot = SelectedTimeSlot,
                    Title = title,
                    LearningObjectives = objectives,
                    ComponentsJson = content
                };

                // Add the lesson
                await _lessonRepository.AddLessonAsync(newLesson);
                Lessons.Add(newLesson);
                SelectedLesson = newLesson;

                _logger.LogInformation("Generated lesson: {LessonTitle} for class {ClassName} on {Date}",
                    newLesson.Title, SelectedClass.Name, CurrentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating lesson for class {ClassId}", SelectedClass.ClassId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Saves the current lesson
        /// </summary>
        private async void SaveLessonAsync()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            try
            {
                SelectedLesson.Title = LessonTitle;
                SelectedLesson.LearningObjectives = LearningObjectives;
                SelectedLesson.ComponentsJson = LessonComponents;
                SelectedLesson.UpdatedAt = DateTime.UtcNow;

                IsBusy = true;
                await _lessonRepository.UpdateLessonAsync(SelectedLesson);

                // Update the calendar event if it exists
                if (!string.IsNullOrEmpty(SelectedLesson.CalendarEventId))
                {
                    await _calendarSyncService.SynchronizeLessonPlanAsync(SelectedLesson.Id);
                    _logger.LogInformation("Updated calendar event for lesson: {LessonTitle}", SelectedLesson.Title);
                }

                _logger.LogInformation("Saved lesson: {LessonTitle}", SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Refreshes the data
        /// </summary>
        private async void RefreshAsync()
        {
            try
            {
                IsBusy = true;

                // Remember the selected class
                var selectedClassId = SelectedClass?.ClassId;

                // Reload classes
                await LoadClassesAsync();

                // Restore the selected class
                if (!string.IsNullOrEmpty(selectedClassId))
                {
                    SelectedClass = Classes.FirstOrDefault(c => c.ClassId == selectedClassId);
                }

                _logger.LogInformation("Refreshed data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing data");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Goes to the previous day
        /// </summary>
        private void PreviousDayAsync()
        {
            try
            {
                var date = DateTime.Parse(CurrentDate);
                date = date.AddDays(-1);
                CurrentDate = date.ToString("yyyy-MM-dd");

                _logger.LogInformation("Changed date to {Date}", CurrentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing date");
            }
        }

        /// <summary>
        /// Goes to the next day
        /// </summary>
        private void NextDayAsync()
        {
            try
            {
                var date = DateTime.Parse(CurrentDate);
                date = date.AddDays(1);
                CurrentDate = date.ToString("yyyy-MM-dd");

                _logger.LogInformation("Changed date to {Date}", CurrentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing date");
            }
        }

        /// <summary>
        /// Adds the selected lesson to the calendar
        /// </summary>
        private async void AddToCalendarAsync()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            try
            {
                IsBusy = true;

                // Synchronize the lesson with the calendar
                var success = await _calendarSyncService.SynchronizeLessonPlanAsync(SelectedLesson.Id);

                if (success)
                {
                    // Refresh the lesson to get the updated calendar event ID
                    await LoadLessonAsync(SelectedLesson.Id);
                    _logger.LogInformation("Lesson {LessonId} synchronized with calendar", SelectedLesson.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to synchronize lesson {LessonId} with calendar", SelectedLesson.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding lesson to calendar");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
