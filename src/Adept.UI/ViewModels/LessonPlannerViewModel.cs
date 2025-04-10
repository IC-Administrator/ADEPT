using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.UI.Commands;
using Adept.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Linq;

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
        private readonly IConfirmationService _confirmationService;
        private readonly ILogger<LessonPlannerViewModel> _logger;
        private bool _isBusy;
        private Class? _selectedClass;
        private LessonPlan? _selectedLesson;
        private DateTime _currentDate = DateTime.Today;
        private int _selectedTimeSlot;
        private string _lessonTitle = string.Empty;
        private string _learningObjectives = string.Empty;
        private string _lessonComponents = string.Empty;
        private ObservableCollection<QuestionAnswer> _retrievalQuestions = new ObservableCollection<QuestionAnswer>();
        private QuestionAnswer _challengeQuestion = new QuestionAnswer();
        private string _bigQuestion = string.Empty;
        private string _starterActivity = string.Empty;
        private string _mainActivity = string.Empty;
        private string _plenaryActivity = string.Empty;
        private bool _isGeneratingRetrievalQuestions;
        private bool _isGeneratingChallengeQuestion;
        private bool _isGeneratingBigQuestion;
        private bool _isGeneratingActivities;
        private string _retrievalQuestionsStatus = string.Empty;
        private string _challengeQuestionStatus = string.Empty;
        private string _bigQuestionStatus = string.Empty;
        private string _activitiesStatus = string.Empty;
        private QuestionAnswer _selectedRetrievalQuestion;
        private bool _isWeekViewActive;
        private string _weekRangeText = string.Empty;
        private ObservableCollection<LessonPlan> _allLessons = new ObservableCollection<LessonPlan>();
        private ObservableCollection<LessonPlan> _mondayPeriod1Lessons = new ObservableCollection<LessonPlan>();
        private ObservableCollection<LessonPlan> _tuesdayPeriod1Lessons = new ObservableCollection<LessonPlan>();
        // Add similar fields for all other days and periods
        private ObservableCollection<LessonResource> _resources = new ObservableCollection<LessonResource>();
        private LessonResource _selectedResource;
        private bool _isAddingLink;
        private bool _isPreviewingResource;
        private string _newResourceName = string.Empty;
        private string _newResourceUrl = string.Empty;
        private string _previewResourceName = string.Empty;
        private object _previewContent;

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

                        // Load resources for the selected lesson
                        LoadResourcesAsync().ConfigureAwait(false);
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
        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                if (SetProperty(ref _currentDate, value))
                {
                    LoadLessonsForSelectedClassAsync().ConfigureAwait(false);
                    OnPropertyChanged(nameof(HasLessons));
                }
            }
        }

        /// <summary>
        /// Gets whether the current date has lessons
        /// </summary>
        public bool HasLessons => Lessons.Count > 0;

        /// <summary>
        /// Checks if a date has lessons
        /// </summary>
        /// <param name="date">The date to check</param>
        /// <returns>True if the date has lessons, false otherwise</returns>
        public bool HasLessonsOnDate(DateTime date)
        {
            if (SelectedClass == null)
            {
                return false;
            }

            var dateString = date.ToString("yyyy-MM-dd");
            return Lessons.Any(l => l.Date == dateString);
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
        /// Gets or sets the lesson components JSON
        /// </summary>
        public string LessonComponents
        {
            get => _lessonComponents;
            set
            {
                if (SetProperty(ref _lessonComponents, value))
                {
                    // Try to parse the components JSON
                    try
                    {
                        var components = JsonSerializer.Deserialize<LessonComponents>(value);
                        if (components != null)
                        {
                            // Update the component properties
                            RetrievalQuestions.Clear();
                            foreach (var question in components.RetrievalQuestions)
                            {
                                RetrievalQuestions.Add(question);
                            }

                            ChallengeQuestion = components.ChallengeQuestion ?? new QuestionAnswer();
                            BigQuestion = components.BigQuestion ?? string.Empty;
                            StarterActivity = components.StarterActivity ?? string.Empty;
                            MainActivity = components.MainActivity ?? string.Empty;
                            PlenaryActivity = components.PlenaryActivity ?? string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing lesson components JSON");
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the retrieval questions
        /// </summary>
        public ObservableCollection<QuestionAnswer> RetrievalQuestions
        {
            get => _retrievalQuestions;
            set => SetProperty(ref _retrievalQuestions, value);
        }

        /// <summary>
        /// Gets or sets the selected retrieval question
        /// </summary>
        public QuestionAnswer SelectedRetrievalQuestion
        {
            get => _selectedRetrievalQuestion;
            set => SetProperty(ref _selectedRetrievalQuestion, value);
        }

        /// <summary>
        /// Gets or sets the challenge question
        /// </summary>
        public QuestionAnswer ChallengeQuestion
        {
            get => _challengeQuestion;
            set => SetProperty(ref _challengeQuestion, value);
        }

        /// <summary>
        /// Gets or sets the big question
        /// </summary>
        public string BigQuestion
        {
            get => _bigQuestion;
            set => SetProperty(ref _bigQuestion, value);
        }

        /// <summary>
        /// Gets or sets the starter activity
        /// </summary>
        public string StarterActivity
        {
            get => _starterActivity;
            set => SetProperty(ref _starterActivity, value);
        }

        /// <summary>
        /// Gets or sets the main activity
        /// </summary>
        public string MainActivity
        {
            get => _mainActivity;
            set => SetProperty(ref _mainActivity, value);
        }

        /// <summary>
        /// Gets or sets the plenary activity
        /// </summary>
        public string PlenaryActivity
        {
            get => _plenaryActivity;
            set => SetProperty(ref _plenaryActivity, value);
        }

        /// <summary>
        /// Gets or sets whether retrieval questions are being generated
        /// </summary>
        public bool IsGeneratingRetrievalQuestions
        {
            get => _isGeneratingRetrievalQuestions;
            set => SetProperty(ref _isGeneratingRetrievalQuestions, value);
        }

        /// <summary>
        /// Gets or sets whether a challenge question is being generated
        /// </summary>
        public bool IsGeneratingChallengeQuestion
        {
            get => _isGeneratingChallengeQuestion;
            set => SetProperty(ref _isGeneratingChallengeQuestion, value);
        }

        /// <summary>
        /// Gets or sets whether a big question is being generated
        /// </summary>
        public bool IsGeneratingBigQuestion
        {
            get => _isGeneratingBigQuestion;
            set => SetProperty(ref _isGeneratingBigQuestion, value);
        }

        /// <summary>
        /// Gets or sets whether activities are being generated
        /// </summary>
        public bool IsGeneratingActivities
        {
            get => _isGeneratingActivities;
            set => SetProperty(ref _isGeneratingActivities, value);
        }

        /// <summary>
        /// Gets or sets the retrieval questions status
        /// </summary>
        public string RetrievalQuestionsStatus
        {
            get => _retrievalQuestionsStatus;
            set => SetProperty(ref _retrievalQuestionsStatus, value);
        }

        /// <summary>
        /// Gets or sets the challenge question status
        /// </summary>
        public string ChallengeQuestionStatus
        {
            get => _challengeQuestionStatus;
            set => SetProperty(ref _challengeQuestionStatus, value);
        }

        /// <summary>
        /// Gets or sets the big question status
        /// </summary>
        public string BigQuestionStatus
        {
            get => _bigQuestionStatus;
            set => SetProperty(ref _bigQuestionStatus, value);
        }

        /// <summary>
        /// Gets or sets the activities status
        /// </summary>
        public string ActivitiesStatus
        {
            get => _activitiesStatus;
            set => SetProperty(ref _activitiesStatus, value);
        }

        /// <summary>
        /// Gets or sets whether the week view is active
        /// </summary>
        public bool IsWeekViewActive
        {
            get => _isWeekViewActive;
            set
            {
                if (SetProperty(ref _isWeekViewActive, value))
                {
                    if (value)
                    {
                        LoadWeekLessonsAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        LoadLessonsForSelectedClassAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the week range text
        /// </summary>
        public string WeekRangeText
        {
            get => _weekRangeText;
            set => SetProperty(ref _weekRangeText, value);
        }

        /// <summary>
        /// Gets or sets all lessons for the week
        /// </summary>
        public ObservableCollection<LessonPlan> AllLessons
        {
            get => _allLessons;
            set => SetProperty(ref _allLessons, value);
        }

        /// <summary>
        /// Gets or sets the Monday Period 1 lessons
        /// </summary>
        public ObservableCollection<LessonPlan> MondayPeriod1Lessons
        {
            get => _mondayPeriod1Lessons;
            set => SetProperty(ref _mondayPeriod1Lessons, value);
        }

        /// <summary>
        /// Gets or sets the Tuesday Period 1 lessons
        /// </summary>
        public ObservableCollection<LessonPlan> TuesdayPeriod1Lessons
        {
            get => _tuesdayPeriod1Lessons;
            set => SetProperty(ref _tuesdayPeriod1Lessons, value);
        }

        // Add similar properties for all other days and periods

        /// <summary>
        /// Gets or sets the resources for the selected lesson
        /// </summary>
        public ObservableCollection<LessonResource> Resources
        {
            get => _resources;
            set => SetProperty(ref _resources, value);
        }

        /// <summary>
        /// Gets or sets the selected resource
        /// </summary>
        public LessonResource SelectedResource
        {
            get => _selectedResource;
            set => SetProperty(ref _selectedResource, value);
        }

        /// <summary>
        /// Gets or sets whether a link is being added
        /// </summary>
        public bool IsAddingLink
        {
            get => _isAddingLink;
            set => SetProperty(ref _isAddingLink, value);
        }

        /// <summary>
        /// Gets or sets whether a resource is being previewed
        /// </summary>
        public bool IsPreviewingResource
        {
            get => _isPreviewingResource;
            set => SetProperty(ref _isPreviewingResource, value);
        }

        /// <summary>
        /// Gets or sets the name of the new resource
        /// </summary>
        public string NewResourceName
        {
            get => _newResourceName;
            set => SetProperty(ref _newResourceName, value);
        }

        /// <summary>
        /// Gets or sets the URL of the new resource
        /// </summary>
        public string NewResourceUrl
        {
            get => _newResourceUrl;
            set => SetProperty(ref _newResourceUrl, value);
        }

        /// <summary>
        /// Gets or sets the name of the resource being previewed
        /// </summary>
        public string PreviewResourceName
        {
            get => _previewResourceName;
            set => SetProperty(ref _previewResourceName, value);
        }

        /// <summary>
        /// Gets or sets the content of the resource being previewed
        /// </summary>
        public object PreviewContent
        {
            get => _previewContent;
            set => SetProperty(ref _previewContent, value);
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
        /// Gets the add retrieval question command
        /// </summary>
        public ICommand AddRetrievalQuestionCommand { get; }

        /// <summary>
        /// Gets the remove retrieval question command
        /// </summary>
        public ICommand RemoveRetrievalQuestionCommand { get; }

        /// <summary>
        /// Gets the generate retrieval questions command
        /// </summary>
        public ICommand GenerateRetrievalQuestionsCommand { get; }

        /// <summary>
        /// Gets the generate challenge question command
        /// </summary>
        public ICommand GenerateChallengeQuestionCommand { get; }

        /// <summary>
        /// Gets the generate big question command
        /// </summary>
        public ICommand GenerateBigQuestionCommand { get; }

        /// <summary>
        /// Gets the generate starter activity command
        /// </summary>
        public ICommand GenerateStarterActivityCommand { get; }

        /// <summary>
        /// Gets the generate main activity command
        /// </summary>
        public ICommand GenerateMainActivityCommand { get; }

        /// <summary>
        /// Gets the generate plenary activity command
        /// </summary>
        public ICommand GeneratePlenaryActivityCommand { get; }

        /// <summary>
        /// Gets the generate all activities command
        /// </summary>
        public ICommand GenerateAllActivitiesCommand { get; }

        /// <summary>
        /// Gets the toggle week view command
        /// </summary>
        public ICommand ToggleWeekViewCommand { get; }

        /// <summary>
        /// Gets the previous week command
        /// </summary>
        public ICommand PreviousWeekCommand { get; }

        /// <summary>
        /// Gets the next week command
        /// </summary>
        public ICommand NextWeekCommand { get; }

        /// <summary>
        /// Gets the move lesson command
        /// </summary>
        public ICommand MoveLessonCommand { get; }

        /// <summary>
        /// Gets the add file resource command
        /// </summary>
        public ICommand AddFileResourceCommand { get; }

        /// <summary>
        /// Gets the add link resource command
        /// </summary>
        public ICommand AddLinkResourceCommand { get; }

        /// <summary>
        /// Gets the remove resource command
        /// </summary>
        public ICommand RemoveResourceCommand { get; }

        /// <summary>
        /// Gets the preview resource command
        /// </summary>
        public ICommand PreviewResourceCommand { get; }

        /// <summary>
        /// Gets the cancel add link command
        /// </summary>
        public ICommand CancelAddLinkCommand { get; }

        /// <summary>
        /// Gets the confirm add link command
        /// </summary>
        public ICommand ConfirmAddLinkCommand { get; }

        /// <summary>
        /// Gets the close preview command
        /// </summary>
        public ICommand ClosePreviewCommand { get; }

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
            IConfirmationService confirmationService,
            ILogger<LessonPlannerViewModel> logger)
        {
            _classRepository = classRepository;
            _lessonRepository = lessonRepository;
            _llmService = llmService;
            _calendarSyncService = calendarSyncService;
            _confirmationService = confirmationService;
            _logger = logger;
            _currentDate = DateTime.Today;

            AddLessonCommand = new RelayCommand(AddLessonAsync, () => IsClassSelected);
            EditLessonCommand = new RelayCommand(EditLessonAsync, () => CanEditLesson);
            DeleteLessonCommand = new RelayCommand(DeleteLessonAsync, () => CanDeleteLesson);
            GenerateLessonCommand = new RelayCommand(GenerateLessonAsync, () => CanGenerateLesson);
            SaveLessonCommand = new RelayCommand(SaveLessonAsync, () => IsLessonSelected);
            RefreshCommand = new RelayCommand(RefreshAsync);
            PreviousDayCommand = new RelayCommand(PreviousDayAsync);
            NextDayCommand = new RelayCommand(NextDayAsync);
            AddToCalendarCommand = new RelayCommand(AddToCalendarAsync, () => IsLessonSelected);

            // Component editing commands
            AddRetrievalQuestionCommand = new RelayCommand(AddRetrievalQuestion);
            RemoveRetrievalQuestionCommand = new RelayCommand<QuestionAnswer>(RemoveRetrievalQuestion);
            GenerateRetrievalQuestionsCommand = new RelayCommand(GenerateRetrievalQuestionsAsync, () => IsLessonSelected && !IsGeneratingRetrievalQuestions);
            GenerateChallengeQuestionCommand = new RelayCommand(GenerateChallengeQuestionAsync, () => IsLessonSelected && !IsGeneratingChallengeQuestion);
            GenerateBigQuestionCommand = new RelayCommand(GenerateBigQuestionAsync, () => IsLessonSelected && !IsGeneratingBigQuestion);
            GenerateStarterActivityCommand = new RelayCommand(GenerateStarterActivityAsync, () => IsLessonSelected && !IsGeneratingActivities);
            GenerateMainActivityCommand = new RelayCommand(GenerateMainActivityAsync, () => IsLessonSelected && !IsGeneratingActivities);
            GeneratePlenaryActivityCommand = new RelayCommand(GeneratePlenaryActivityAsync, () => IsLessonSelected && !IsGeneratingActivities);
            GenerateAllActivitiesCommand = new RelayCommand(GenerateAllActivitiesAsync, () => IsLessonSelected && !IsGeneratingActivities);

            // Week view commands
            ToggleWeekViewCommand = new RelayCommand(ToggleWeekView);
            PreviousWeekCommand = new RelayCommand(PreviousWeekAsync);
            NextWeekCommand = new RelayCommand(NextWeekAsync);
            MoveLessonCommand = new RelayCommand<object>(MoveLesson);

            // Resource management commands
            AddFileResourceCommand = new RelayCommand(AddFileResource, () => IsLessonSelected);
            AddLinkResourceCommand = new RelayCommand(AddLinkResource, () => IsLessonSelected);
            RemoveResourceCommand = new RelayCommand<LessonResource>(RemoveResource, r => r != null);
            PreviewResourceCommand = new RelayCommand<LessonResource>(PreviewResource, r => r != null);
            CancelAddLinkCommand = new RelayCommand(CancelAddLink);
            ConfirmAddLinkCommand = new RelayCommand(ConfirmAddLink, () => !string.IsNullOrEmpty(NewResourceName) && !string.IsNullOrEmpty(NewResourceUrl));
            ClosePreviewCommand = new RelayCommand(ClosePreview);

            // Load classes
            LoadClassesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads all classes
        /// </summary>
        public async Task LoadClassesAsync()
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
        public async Task LoadLessonsForSelectedClassAsync()
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
                var dateString = CurrentDate.ToString("yyyy-MM-dd");
                lessons = lessons.Where(l => l.Date == dateString);

                foreach (var lesson in lessons.OrderBy(l => l.TimeSlot))
                {
                    Lessons.Add(lesson);
                }

                OnPropertyChanged(nameof(HasLessons));

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
                    Date = CurrentDate.ToString("yyyy-MM-dd"),
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
                // Show a confirmation dialog
                var lessonToDelete = SelectedLesson;
                var title = "Delete Lesson";
                var message = $"Are you sure you want to delete the lesson '{lessonToDelete.Title}'? This action cannot be undone.";

                var confirmed = await _confirmationService.ShowConfirmationAsync(title, message, "Delete", "Cancel");
                if (!confirmed)
                {
                    return;
                }

                IsBusy = true;

                // Delete the calendar event if it exists
                if (!string.IsNullOrEmpty(lessonToDelete.CalendarEventId))
                {
                    await _calendarSyncService.DeleteCalendarEventAsync(int.Parse(lessonToDelete.LessonId));
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
                    Date = CurrentDate.ToString("yyyy-MM-dd"),
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

                // Update components from structured editor
                var components = new LessonComponents
                {
                    RetrievalQuestions = RetrievalQuestions.ToList(),
                    ChallengeQuestion = ChallengeQuestion,
                    BigQuestion = BigQuestion,
                    StarterActivity = StarterActivity,
                    MainActivity = MainActivity,
                    PlenaryActivity = PlenaryActivity
                };

                SelectedLesson.ComponentsJson = JsonSerializer.Serialize(components);
                SelectedLesson.UpdatedAt = DateTime.UtcNow;

                IsBusy = true;
                await _lessonRepository.UpdateLessonAsync(SelectedLesson);

                // Update the calendar event if it exists
                if (!string.IsNullOrEmpty(SelectedLesson.CalendarEventId))
                {
                    await _calendarSyncService.SynchronizeLessonPlanAsync(int.Parse(SelectedLesson.LessonId));
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
                CurrentDate = CurrentDate.AddDays(-1);

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
                CurrentDate = CurrentDate.AddDays(1);

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
                var success = await _calendarSyncService.SynchronizeLessonPlanAsync(int.Parse(SelectedLesson.LessonId));

                if (success)
                {
                    // Refresh the lesson to get the updated calendar event ID
                    var updatedLesson = await _lessonRepository.GetLessonByIdAsync(SelectedLesson.LessonId);
                    if (updatedLesson != null)
                    {
                        SelectedLesson = updatedLesson;
                    }
                    _logger.LogInformation("Lesson {LessonId} synchronized with calendar", SelectedLesson.LessonId);
                }
                else
                {
                    _logger.LogWarning("Failed to synchronize lesson {LessonId} with calendar", SelectedLesson.LessonId);
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

        #region Component Editing Methods

        /// <summary>
        /// Adds a new retrieval question
        /// </summary>
        private void AddRetrievalQuestion()
        {
            RetrievalQuestions.Add(new QuestionAnswer
            {
                Question = "New question",
                Answer = "Answer"
            });
        }

        /// <summary>
        /// Removes a retrieval question
        /// </summary>
        private void RemoveRetrievalQuestion(QuestionAnswer question)
        {
            if (question != null)
            {
                RetrievalQuestions.Remove(question);
            }
        }

        /// <summary>
        /// Generates retrieval questions using the LLM
        /// </summary>
        private async void GenerateRetrievalQuestionsAsync()
        {
            if (SelectedLesson == null || SelectedClass == null)
            {
                return;
            }

            try
            {
                IsGeneratingRetrievalQuestions = true;
                RetrievalQuestionsStatus = "Generating retrieval questions...";

                // Create a prompt for the LLM
                var prompt = $"Generate 5 retrieval questions with answers for a {SelectedClass.GradeLevel} {SelectedClass.Subject} class. " +
                             $"The lesson is titled '{SelectedLesson.Title}' with learning objectives: {SelectedLesson.LearningObjectives}. " +
                             $"Format each question and answer as a JSON object with 'question' and 'answer' properties.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);

                // Parse the response
                var content = response.Message.Content;

                // Extract questions and answers using regex
                var questionMatches = System.Text.RegularExpressions.Regex.Matches(content, @"\{\s*""question""\s*:\s*""([^""]+)""\s*,\s*""answer""\s*:\s*""([^""]+)""\s*\}");

                if (questionMatches.Count > 0)
                {
                    // Clear existing questions if we found new ones
                    RetrievalQuestions.Clear();

                    foreach (System.Text.RegularExpressions.Match match in questionMatches)
                    {
                        if (match.Groups.Count >= 3)
                        {
                            var question = match.Groups[1].Value;
                            var answer = match.Groups[2].Value;

                            RetrievalQuestions.Add(new QuestionAnswer
                            {
                                Question = question,
                                Answer = answer
                            });
                        }
                    }

                    RetrievalQuestionsStatus = $"Generated {RetrievalQuestions.Count} retrieval questions.";
                }
                else
                {
                    // Try a different approach with line-by-line parsing
                    var lines = content.Split('\n');
                    var currentQuestion = "";
                    var currentAnswer = "";

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Q:") || line.StartsWith("Question:"))
                        {
                            // Save previous Q&A if we have them
                            if (!string.IsNullOrEmpty(currentQuestion) && !string.IsNullOrEmpty(currentAnswer))
                            {
                                RetrievalQuestions.Add(new QuestionAnswer
                                {
                                    Question = currentQuestion,
                                    Answer = currentAnswer
                                });
                            }

                            // Start new question
                            currentQuestion = line.Substring(line.IndexOf(':') + 1).Trim();
                            currentAnswer = "";
                        }
                        else if (line.StartsWith("A:") || line.StartsWith("Answer:"))
                        {
                            currentAnswer = line.Substring(line.IndexOf(':') + 1).Trim();
                        }
                    }

                    // Add the last Q&A if we have them
                    if (!string.IsNullOrEmpty(currentQuestion) && !string.IsNullOrEmpty(currentAnswer))
                    {
                        RetrievalQuestions.Add(new QuestionAnswer
                        {
                            Question = currentQuestion,
                            Answer = currentAnswer
                        });
                    }

                    if (RetrievalQuestions.Count > 0)
                    {
                        RetrievalQuestionsStatus = $"Generated {RetrievalQuestions.Count} retrieval questions.";
                    }
                    else
                    {
                        // If all else fails, create some basic questions from the content
                        var sentences = content.Split('.', '?', '!');
                        for (int i = 0; i < Math.Min(5, sentences.Length); i++)
                        {
                            var sentence = sentences[i].Trim();
                            if (sentence.Length > 10)
                            {
                                RetrievalQuestions.Add(new QuestionAnswer
                                {
                                    Question = $"What is {sentence.Substring(0, sentence.Length / 2)}...?",
                                    Answer = sentence
                                });
                            }
                        }

                        RetrievalQuestionsStatus = $"Created {RetrievalQuestions.Count} basic questions from content.";
                    }
                }

                _logger.LogInformation("Generated {Count} retrieval questions for lesson: {LessonTitle}",
                    RetrievalQuestions.Count, SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                RetrievalQuestionsStatus = "Error generating retrieval questions.";
                _logger.LogError(ex, "Error generating retrieval questions for lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsGeneratingRetrievalQuestions = false;
            }
        }

        /// <summary>
        /// Generates a challenge question using the LLM
        /// </summary>
        private async void GenerateChallengeQuestionAsync()
        {
            if (SelectedLesson == null || SelectedClass == null)
            {
                return;
            }

            try
            {
                IsGeneratingChallengeQuestion = true;
                ChallengeQuestionStatus = "Generating challenge question...";

                // Create a prompt for the LLM
                var prompt = $"Generate a challenging question with a detailed answer for a {SelectedClass.GradeLevel} {SelectedClass.Subject} class. " +
                             $"The lesson is titled '{SelectedLesson.Title}' with learning objectives: {SelectedLesson.LearningObjectives}. " +
                             $"The question should be thought-provoking and require deeper understanding. " +
                             $"Format as a JSON object with 'question' and 'answer' properties.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);

                // Parse the response
                var content = response.Message.Content;

                // Extract question and answer using regex
                var match = System.Text.RegularExpressions.Regex.Match(content, @"\{\s*""question""\s*:\s*""([^""]+)""\s*,\s*""answer""\s*:\s*""([^""]+)""\s*\}");

                if (match.Success && match.Groups.Count >= 3)
                {
                    var question = match.Groups[1].Value;
                    var answer = match.Groups[2].Value;

                    ChallengeQuestion = new QuestionAnswer
                    {
                        Question = question,
                        Answer = answer
                    };

                    ChallengeQuestionStatus = "Generated challenge question.";
                }
                else
                {
                    // Try a different approach with line-by-line parsing
                    var lines = content.Split('\n');
                    var questionLine = lines.FirstOrDefault(l => l.StartsWith("Q:") || l.StartsWith("Question:"));
                    var answerLine = lines.FirstOrDefault(l => l.StartsWith("A:") || l.StartsWith("Answer:"));

                    if (!string.IsNullOrEmpty(questionLine) && !string.IsNullOrEmpty(answerLine))
                    {
                        var question = questionLine.Substring(questionLine.IndexOf(':') + 1).Trim();
                        var answer = answerLine.Substring(answerLine.IndexOf(':') + 1).Trim();

                        ChallengeQuestion = new QuestionAnswer
                        {
                            Question = question,
                            Answer = answer
                        };

                        ChallengeQuestionStatus = "Generated challenge question.";
                    }
                    else
                    {
                        // If all else fails, use the first part of the content as the question and the rest as the answer
                        var contentParts = content.Split(new string[] { "\n\n" }, 2, StringSplitOptions.None);
                        var question = contentParts[0].Trim();
                        var answer = contentParts.Length > 1 ? contentParts[1].Trim() : "";

                        ChallengeQuestion = new QuestionAnswer
                        {
                            Question = question,
                            Answer = answer
                        };

                        ChallengeQuestionStatus = "Created challenge question from content.";
                    }
                }

                _logger.LogInformation("Generated challenge question for lesson: {LessonTitle}", SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                ChallengeQuestionStatus = "Error generating challenge question.";
                _logger.LogError(ex, "Error generating challenge question for lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsGeneratingChallengeQuestion = false;
            }
        }

        /// <summary>
        /// Generates a big question using the LLM
        /// </summary>
        private async void GenerateBigQuestionAsync()
        {
            if (SelectedLesson == null || SelectedClass == null)
            {
                return;
            }

            try
            {
                IsGeneratingBigQuestion = true;
                BigQuestionStatus = "Generating big question...";

                // Create a prompt for the LLM
                var prompt = $"Generate a 'big question' for a {SelectedClass.GradeLevel} {SelectedClass.Subject} class. " +
                             $"The lesson is titled '{SelectedLesson.Title}' with learning objectives: {SelectedLesson.LearningObjectives}. " +
                             $"The big question should be open-ended, thought-provoking, and connect to broader themes or real-world applications.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);

                // Parse the response
                BigQuestion = response.Message.Content.Trim();

                _logger.LogInformation("Generated big question for lesson: {LessonTitle}", SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                BigQuestionStatus = "Error generating big question.";
                _logger.LogError(ex, "Error generating big question for lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsGeneratingBigQuestion = false;
            }
        }

        /// <summary>
        /// Generates a starter activity using the LLM
        /// </summary>
        private async void GenerateStarterActivityAsync()
        {
            if (SelectedLesson == null || SelectedClass == null)
            {
                return;
            }

            try
            {
                IsGeneratingActivities = true;
                ActivitiesStatus = "Generating starter activity...";

                // Create a prompt for the LLM
                var prompt = $"Generate a starter activity for a {SelectedClass.GradeLevel} {SelectedClass.Subject} class. " +
                             $"The lesson is titled '{SelectedLesson.Title}' with learning objectives: {SelectedLesson.LearningObjectives}. " +
                             $"The starter activity should engage students and prepare them for the main lesson content.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);

                // Parse the response
                StarterActivity = response.Message.Content.Trim();

                ActivitiesStatus = "Generated starter activity.";
                _logger.LogInformation("Generated starter activity for lesson: {LessonTitle}", SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                ActivitiesStatus = "Error generating starter activity.";
                _logger.LogError(ex, "Error generating starter activity for lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsGeneratingActivities = false;
            }
        }

        /// <summary>
        /// Generates a main activity using the LLM
        /// </summary>
        private async void GenerateMainActivityAsync()
        {
            if (SelectedLesson == null || SelectedClass == null)
            {
                return;
            }

            try
            {
                IsGeneratingActivities = true;
                ActivitiesStatus = "Generating main activity...";

                // Create a prompt for the LLM
                var prompt = $"Generate a main activity for a {SelectedClass.GradeLevel} {SelectedClass.Subject} class. " +
                             $"The lesson is titled '{SelectedLesson.Title}' with learning objectives: {SelectedLesson.LearningObjectives}. " +
                             $"The main activity should be the core learning experience that helps students achieve the learning objectives.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);

                // Parse the response
                MainActivity = response.Message.Content.Trim();

                ActivitiesStatus = "Generated main activity.";
                _logger.LogInformation("Generated main activity for lesson: {LessonTitle}", SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                ActivitiesStatus = "Error generating main activity.";
                _logger.LogError(ex, "Error generating main activity for lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsGeneratingActivities = false;
            }
        }

        /// <summary>
        /// Generates a plenary activity using the LLM
        /// </summary>
        private async void GeneratePlenaryActivityAsync()
        {
            if (SelectedLesson == null || SelectedClass == null)
            {
                return;
            }

            try
            {
                IsGeneratingActivities = true;
                ActivitiesStatus = "Generating plenary activity...";

                // Create a prompt for the LLM
                var prompt = $"Generate a plenary activity for a {SelectedClass.GradeLevel} {SelectedClass.Subject} class. " +
                             $"The lesson is titled '{SelectedLesson.Title}' with learning objectives: {SelectedLesson.LearningObjectives}. " +
                             $"The plenary activity should consolidate learning and check understanding at the end of the lesson.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);

                // Parse the response
                PlenaryActivity = response.Message.Content.Trim();

                ActivitiesStatus = "Generated plenary activity.";
                _logger.LogInformation("Generated plenary activity for lesson: {LessonTitle}", SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                ActivitiesStatus = "Error generating plenary activity.";
                _logger.LogError(ex, "Error generating plenary activity for lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsGeneratingActivities = false;
            }
        }

        /// <summary>
        /// Generates all activities using the LLM
        /// </summary>
        private async void GenerateAllActivitiesAsync()
        {
            if (SelectedLesson == null || SelectedClass == null)
            {
                return;
            }

            try
            {
                IsGeneratingActivities = true;
                ActivitiesStatus = "Generating all activities...";

                // Create a prompt for the LLM
                var prompt = $"Generate a complete set of activities for a {SelectedClass.GradeLevel} {SelectedClass.Subject} class. " +
                             $"The lesson is titled '{SelectedLesson.Title}' with learning objectives: {SelectedLesson.LearningObjectives}. " +
                             $"Include a starter activity, main activity, and plenary activity. " +
                             $"Format your response with clear headings for each activity type.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);

                // Parse the response
                var content = response.Message.Content;

                // Extract activities using regex or simple text parsing
                var starterMatch = System.Text.RegularExpressions.Regex.Match(content, @"(?:Starter Activity|Starter|Introduction):(.*?)(?=Main Activity|Main|Development|Plenary Activity|Plenary|Conclusion|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
                var mainMatch = System.Text.RegularExpressions.Regex.Match(content, @"(?:Main Activity|Main|Development):(.*?)(?=Plenary Activity|Plenary|Conclusion|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
                var plenaryMatch = System.Text.RegularExpressions.Regex.Match(content, @"(?:Plenary Activity|Plenary|Conclusion):(.*?)$", System.Text.RegularExpressions.RegexOptions.Singleline);

                if (starterMatch.Success)
                {
                    StarterActivity = starterMatch.Groups[1].Value.Trim();
                }

                if (mainMatch.Success)
                {
                    MainActivity = mainMatch.Groups[1].Value.Trim();
                }

                if (plenaryMatch.Success)
                {
                    PlenaryActivity = plenaryMatch.Groups[1].Value.Trim();
                }

                ActivitiesStatus = "Generated all activities.";
                _logger.LogInformation("Generated all activities for lesson: {LessonTitle}", SelectedLesson.Title);
            }
            catch (Exception ex)
            {
                ActivitiesStatus = "Error generating activities.";
                _logger.LogError(ex, "Error generating activities for lesson {LessonId}", SelectedLesson.LessonId);
            }
            finally
            {
                IsGeneratingActivities = false;
            }
        }

        #endregion

        #region Week View Methods

        /// <summary>
        /// Toggles between week view and day view
        /// </summary>
        private void ToggleWeekView()
        {
            IsWeekViewActive = !IsWeekViewActive;
        }

        /// <summary>
        /// Loads lessons for the current week
        /// </summary>
        private async Task LoadWeekLessonsAsync()
        {
            if (SelectedClass == null)
            {
                return;
            }

            try
            {
                IsBusy = true;
                AllLessons.Clear();
                ClearWeekLessons();

                // Calculate the start and end of the week
                var startOfWeek = GetStartOfWeek(CurrentDate);
                var endOfWeek = startOfWeek.AddDays(4); // Monday to Friday

                // Update the week range text
                WeekRangeText = $"{startOfWeek:MMM d} - {endOfWeek:MMM d, yyyy}";

                // Load all lessons for the class
                var lessons = await _lessonRepository.GetLessonsByClassIdAsync(SelectedClass.ClassId);

                // Filter by week
                var weekLessons = lessons.Where(l =>
                {
                    if (DateTime.TryParse(l.Date, out var lessonDate))
                    {
                        return lessonDate >= startOfWeek && lessonDate <= endOfWeek;
                    }
                    return false;
                }).ToList();

                foreach (var lesson in weekLessons)
                {
                    AllLessons.Add(lesson);

                    // Add to the appropriate day and period collection
                    if (DateTime.TryParse(lesson.Date, out var lessonDate))
                    {
                        var dayOfWeek = lessonDate.DayOfWeek;
                        var period = lesson.TimeSlot;

                        // Add to the appropriate collection based on day and period
                        AddLessonToWeekCell(lesson, dayOfWeek, period);
                    }
                }

                _logger.LogInformation("Loaded {Count} lessons for week {WeekRange}",
                    AllLessons.Count, WeekRangeText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lessons for week");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Clears all week lesson collections
        /// </summary>
        private void ClearWeekLessons()
        {
            MondayPeriod1Lessons.Clear();
            TuesdayPeriod1Lessons.Clear();
            // Clear all other day/period collections
        }

        /// <summary>
        /// Adds a lesson to the appropriate week cell collection
        /// </summary>
        private void AddLessonToWeekCell(LessonPlan lesson, DayOfWeek dayOfWeek, int period)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    if (period == 0) MondayPeriod1Lessons.Add(lesson);
                    // Add cases for other periods
                    break;
                case DayOfWeek.Tuesday:
                    if (period == 0) TuesdayPeriod1Lessons.Add(lesson);
                    // Add cases for other periods
                    break;
                // Add cases for other days
            }
        }

        /// <summary>
        /// Gets the start of the week (Monday) for a given date
        /// </summary>
        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Moves to the previous week
        /// </summary>
        private async void PreviousWeekAsync()
        {
            CurrentDate = CurrentDate.AddDays(-7);
            await LoadWeekLessonsAsync();
        }

        /// <summary>
        /// Moves to the next week
        /// </summary>
        private async void NextWeekAsync()
        {
            CurrentDate = CurrentDate.AddDays(7);
            await LoadWeekLessonsAsync();
        }

        /// <summary>
        /// Moves a lesson to a new day and period
        /// </summary>
        private void MoveLesson(object parameter)
        {
            if (parameter is dynamic)
            {
                dynamic data = parameter;
                LessonPlan lesson = data.Lesson;
                string day = data.Day;
                string period = data.Period;

                if (lesson != null && !string.IsNullOrEmpty(day) && !string.IsNullOrEmpty(period))
                {
                    // Calculate the new date based on the day of the week
                    var startOfWeek = GetStartOfWeek(CurrentDate);
                    var newDate = startOfWeek;

                    switch (day)
                    {
                        case "Monday": newDate = startOfWeek; break;
                        case "Tuesday": newDate = startOfWeek.AddDays(1); break;
                        case "Wednesday": newDate = startOfWeek.AddDays(2); break;
                        case "Thursday": newDate = startOfWeek.AddDays(3); break;
                        case "Friday": newDate = startOfWeek.AddDays(4); break;
                    }

                    // Update the lesson
                    lesson.Date = newDate.ToString("yyyy-MM-dd");
                    lesson.TimeSlot = int.Parse(period);
                    lesson.UpdatedAt = DateTime.UtcNow;

                    // Save the updated lesson
                    _lessonRepository.UpdateLessonAsync(lesson).ConfigureAwait(false);

                    // Refresh the week view
                    LoadWeekLessonsAsync().ConfigureAwait(false);

                    _logger.LogInformation("Moved lesson {LessonId} to {Day} {Period}",
                        lesson.LessonId, day, period);
                }
            }
        }

        #endregion

        #region Resource Management Methods

        /// <summary>
        /// Loads resources for the selected lesson
        /// </summary>
        public async Task LoadResourcesAsync()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            try
            {
                Resources.Clear();

                // In a real implementation, this would load resources from a repository
                // For now, we'll just create some sample resources
                var resources = new List<LessonResource>
                {
                    new LessonResource
                    {
                        LessonId = Guid.Parse(SelectedLesson.LessonId),
                        Name = "Lesson Slides",
                        Type = ResourceType.Presentation,
                        Path = "C:\\Temp\\slides.pptx"
                    },
                    new LessonResource
                    {
                        LessonId = Guid.Parse(SelectedLesson.LessonId),
                        Name = "Worksheet",
                        Type = ResourceType.Document,
                        Path = "C:\\Temp\\worksheet.docx"
                    },
                    new LessonResource
                    {
                        LessonId = Guid.Parse(SelectedLesson.LessonId),
                        Name = "Reference Website",
                        Type = ResourceType.Link,
                        Path = "https://www.example.com"
                    }
                };

                foreach (var resource in resources)
                {
                    Resources.Add(resource);
                }

                _logger.LogInformation("Loaded {Count} resources for lesson {LessonId}",
                    Resources.Count, SelectedLesson.LessonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading resources for lesson {LessonId}", SelectedLesson.LessonId);
            }
        }

        /// <summary>
        /// Adds a file resource to the selected lesson
        /// </summary>
        private void AddFileResource()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            try
            {
                // Show file dialog
                var dialog = new OpenFileDialog
                {
                    Title = "Select a file to add as a resource",
                    Filter = "All Files (*.*)|*.*|Documents (*.docx;*.pdf)|*.docx;*.pdf|Images (*.jpg;*.png)|*.jpg;*.png|Videos (*.mp4;*.avi)|*.mp4;*.avi",
                    FilterIndex = 1,
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    var filePath = dialog.FileName;
                    var fileName = Path.GetFileName(filePath);
                    var fileExtension = Path.GetExtension(filePath).ToLower();

                    // Determine resource type based on file extension
                    var resourceType = GetResourceTypeFromExtension(fileExtension);

                    // Create the resource
                    var resource = new LessonResource
                    {
                        LessonId = Guid.Parse(SelectedLesson.LessonId),
                        Name = fileName,
                        Type = resourceType,
                        Path = filePath
                    };

                    // Add to the collection
                    Resources.Add(resource);

                    _logger.LogInformation("Added file resource {ResourceName} to lesson {LessonId}",
                        resource.Name, SelectedLesson.LessonId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding file resource to lesson {LessonId}", SelectedLesson.LessonId);
            }
        }

        /// <summary>
        /// Determines the resource type based on file extension
        /// </summary>
        private ResourceType GetResourceTypeFromExtension(string extension)
        {
            switch (extension)
            {
                case ".docx":
                case ".doc":
                case ".pdf":
                case ".txt":
                    return ResourceType.Document;
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    return ResourceType.Image;
                case ".pptx":
                case ".ppt":
                    return ResourceType.Presentation;
                case ".xlsx":
                case ".xls":
                case ".csv":
                    return ResourceType.Spreadsheet;
                case ".mp4":
                case ".avi":
                case ".mov":
                case ".wmv":
                    return ResourceType.Video;
                case ".mp3":
                case ".wav":
                case ".ogg":
                    return ResourceType.Audio;
                default:
                    return ResourceType.File;
            }
        }

        /// <summary>
        /// Initiates adding a link resource
        /// </summary>
        private void AddLinkResource()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            // Reset the dialog fields
            NewResourceName = "";
            NewResourceUrl = "https://";

            // Show the dialog
            IsAddingLink = true;
        }

        /// <summary>
        /// Cancels adding a link resource
        /// </summary>
        private void CancelAddLink()
        {
            IsAddingLink = false;
        }

        /// <summary>
        /// Confirms adding a link resource
        /// </summary>
        private void ConfirmAddLink()
        {
            if (SelectedLesson == null || string.IsNullOrEmpty(NewResourceName) || string.IsNullOrEmpty(NewResourceUrl))
            {
                return;
            }

            try
            {
                // Create the resource
                var resource = new LessonResource
                {
                    LessonId = Guid.Parse(SelectedLesson.LessonId),
                    Name = NewResourceName,
                    Type = ResourceType.Link,
                    Path = NewResourceUrl
                };

                // Add to the collection
                Resources.Add(resource);

                // Hide the dialog
                IsAddingLink = false;

                _logger.LogInformation("Added link resource {ResourceName} to lesson {LessonId}",
                    resource.Name, SelectedLesson.LessonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding link resource to lesson {LessonId}", SelectedLesson.LessonId);
            }
        }

        /// <summary>
        /// Removes a resource from the selected lesson
        /// </summary>
        private void RemoveResource(LessonResource resource)
        {
            if (resource == null)
            {
                return;
            }

            try
            {
                // Remove from the collection
                Resources.Remove(resource);

                _logger.LogInformation("Removed resource {ResourceName} from lesson {LessonId}",
                    resource.Name, resource.LessonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing resource {ResourceId} from lesson {LessonId}",
                    resource.ResourceId, resource.LessonId);
            }
        }

        /// <summary>
        /// Previews a resource
        /// </summary>
        private void PreviewResource(LessonResource resource)
        {
            if (resource == null)
            {
                return;
            }

            try
            {
                PreviewResourceName = resource.Name;
                PreviewContent = CreatePreviewContent(resource);
                IsPreviewingResource = true;

                _logger.LogInformation("Previewing resource {ResourceName}", resource.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing resource {ResourceId}", resource.ResourceId);
            }
        }

        /// <summary>
        /// Creates preview content for a resource
        /// </summary>
        private object CreatePreviewContent(LessonResource resource)
        {
            switch (resource.Type)
            {
                case ResourceType.Image:
                    try
                    {
                        return new Image
                        {
                            Source = new BitmapImage(new Uri(resource.Path)),
                            Stretch = System.Windows.Media.Stretch.Uniform,
                            MaxHeight = 500,
                            MaxWidth = 700
                        };
                    }
                    catch
                    {
                        return new TextBlock { Text = "Unable to load image." };
                    }
                case ResourceType.Link:
                    var linkText = new TextBlock
                    {
                        Text = $"Link: {resource.Path}",
                        TextWrapping = TextWrapping.Wrap
                    };
                    var button = new Button
                    {
                        Content = "Open in Browser",
                        Margin = new Thickness(0, 10, 0, 0)
                    };
                    button.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = resource.Path,
                        UseShellExecute = true
                    });
                    var panel = new StackPanel();
                    panel.Children.Add(linkText);
                    panel.Children.Add(button);
                    return panel;
                default:
                    return new TextBlock
                    {
                        Text = $"File: {resource.Path}\n\nUse the system default application to open this file.",
                        TextWrapping = TextWrapping.Wrap
                    };
            }
        }

        /// <summary>
        /// Closes the resource preview
        /// </summary>
        private void ClosePreview()
        {
            IsPreviewingResource = false;
            PreviewContent = null;
        }

        #endregion
    }
}
