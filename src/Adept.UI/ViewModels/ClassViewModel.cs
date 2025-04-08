using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.UI.Commands;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// View model for the Classes tab
    /// </summary>
    public class ClassViewModel : ViewModelBase
    {
        private readonly IClassRepository _classRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<ClassViewModel> _logger;
        private bool _isBusy;
        private Class? _selectedClass;
        private Student? _selectedStudent;
        private string _searchText = string.Empty;

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
                    LoadStudentsForSelectedClassAsync().ConfigureAwait(false);
                    OnPropertyChanged(nameof(IsClassSelected));
                    OnPropertyChanged(nameof(CanEditClass));
                    OnPropertyChanged(nameof(CanDeleteClass));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected student
        /// </summary>
        public Student? SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (SetProperty(ref _selectedStudent, value))
                {
                    OnPropertyChanged(nameof(IsStudentSelected));
                    OnPropertyChanged(nameof(CanEditStudent));
                    OnPropertyChanged(nameof(CanDeleteStudent));
                }
            }
        }

        /// <summary>
        /// Gets or sets the search text
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterClassesAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets whether a class is selected
        /// </summary>
        public bool IsClassSelected => SelectedClass != null;

        /// <summary>
        /// Gets whether a student is selected
        /// </summary>
        public bool IsStudentSelected => SelectedStudent != null;

        /// <summary>
        /// Gets whether a class can be edited
        /// </summary>
        public bool CanEditClass => IsClassSelected;

        /// <summary>
        /// Gets whether a class can be deleted
        /// </summary>
        public bool CanDeleteClass => IsClassSelected;

        /// <summary>
        /// Gets whether a student can be edited
        /// </summary>
        public bool CanEditStudent => IsStudentSelected;

        /// <summary>
        /// Gets whether a student can be deleted
        /// </summary>
        public bool CanDeleteStudent => IsStudentSelected;

        /// <summary>
        /// Gets the classes
        /// </summary>
        public ObservableCollection<Class> Classes { get; } = new ObservableCollection<Class>();

        /// <summary>
        /// Gets the students for the selected class
        /// </summary>
        public ObservableCollection<Student> Students { get; } = new ObservableCollection<Student>();

        /// <summary>
        /// Gets the add class command
        /// </summary>
        public ICommand AddClassCommand { get; }

        /// <summary>
        /// Gets the edit class command
        /// </summary>
        public ICommand EditClassCommand { get; }

        /// <summary>
        /// Gets the delete class command
        /// </summary>
        public ICommand DeleteClassCommand { get; }

        /// <summary>
        /// Gets the add student command
        /// </summary>
        public ICommand AddStudentCommand { get; }

        /// <summary>
        /// Gets the edit student command
        /// </summary>
        public ICommand EditStudentCommand { get; }

        /// <summary>
        /// Gets the delete student command
        /// </summary>
        public ICommand DeleteStudentCommand { get; }

        /// <summary>
        /// Gets the refresh command
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Gets the import command
        /// </summary>
        public ICommand ImportCommand { get; }

        /// <summary>
        /// Gets the export command
        /// </summary>
        public ICommand ExportCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassViewModel"/> class
        /// </summary>
        /// <param name="classRepository">The class repository</param>
        /// <param name="studentRepository">The student repository</param>
        /// <param name="logger">The logger</param>
        public ClassViewModel(
            IClassRepository classRepository,
            IStudentRepository studentRepository,
            ILogger<ClassViewModel> logger)
        {
            _classRepository = classRepository;
            _studentRepository = studentRepository;
            _logger = logger;

            AddClassCommand = new RelayCommand(AddClassAsync);
            EditClassCommand = new RelayCommand(EditClassAsync, () => CanEditClass);
            DeleteClassCommand = new RelayCommand(DeleteClassAsync, () => CanDeleteClass);
            AddStudentCommand = new RelayCommand(AddStudentAsync, () => IsClassSelected);
            EditStudentCommand = new RelayCommand(EditStudentAsync, () => CanEditStudent);
            DeleteStudentCommand = new RelayCommand(DeleteStudentAsync, () => CanDeleteStudent);
            RefreshCommand = new RelayCommand(RefreshAsync);
            ImportCommand = new RelayCommand(ImportAsync);
            ExportCommand = new RelayCommand(ExportAsync, () => Classes.Count > 0);

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
        /// Loads students for the selected class
        /// </summary>
        private async Task LoadStudentsForSelectedClassAsync()
        {
            try
            {
                if (SelectedClass == null)
                {
                    Students.Clear();
                    return;
                }

                IsBusy = true;
                Students.Clear();

                var students = await _studentRepository.GetStudentsByClassIdAsync(SelectedClass.ClassId);
                foreach (var student in students.OrderBy(s => s.Name))
                {
                    Students.Add(student);
                }

                _logger.LogInformation("Loaded {Count} students for class {ClassName}", Students.Count, SelectedClass.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students for class {ClassId}", SelectedClass?.ClassId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Filters classes based on the search text
        /// </summary>
        private async Task FilterClassesAsync()
        {
            try
            {
                IsBusy = true;
                Classes.Clear();

                var classes = await _classRepository.GetAllClassesAsync();

                // Filter classes if search text is not empty
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    classes = classes.Where(c =>
                        c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        c.Subject.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        c.GradeLevel.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        c.AcademicYear.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    );
                }

                foreach (var cls in classes.OrderBy(c => c.Name))
                {
                    Classes.Add(cls);
                }

                _logger.LogInformation("Filtered to {Count} classes", Classes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering classes");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Adds a new class
        /// </summary>
        private async void AddClassAsync()
        {
            try
            {
                // In a real implementation, this would show a dialog to get class details
                var newClass = new Class
                {
                    Name = "New Class",
                    GradeLevel = "Year 7",
                    Subject = "Science",
                    AcademicYear = "2024-2025"
                };

                IsBusy = true;
                await _classRepository.AddClassAsync(newClass);
                Classes.Add(newClass);
                SelectedClass = newClass;

                _logger.LogInformation("Added new class: {ClassName}", newClass.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding class");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Edits the selected class
        /// </summary>
        private async void EditClassAsync()
        {
            if (SelectedClass == null)
            {
                return;
            }

            try
            {
                // In a real implementation, this would show a dialog to edit class details
                SelectedClass.Name = $"{SelectedClass.Name} (Edited)";
                SelectedClass.UpdatedAt = DateTime.UtcNow;

                IsBusy = true;
                await _classRepository.UpdateClassAsync(SelectedClass);

                // Refresh the view
                OnPropertyChanged(nameof(SelectedClass));

                _logger.LogInformation("Updated class: {ClassName}", SelectedClass.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating class {ClassId}", SelectedClass.ClassId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Deletes the selected class
        /// </summary>
        private async void DeleteClassAsync()
        {
            if (SelectedClass == null)
            {
                return;
            }

            try
            {
                // In a real implementation, this would show a confirmation dialog
                var classToDelete = SelectedClass;

                IsBusy = true;
                await _classRepository.DeleteClassAsync(classToDelete.ClassId);
                Classes.Remove(classToDelete);
                SelectedClass = null;

                _logger.LogInformation("Deleted class: {ClassName}", classToDelete.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting class {ClassId}", SelectedClass.ClassId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Adds a new student to the selected class
        /// </summary>
        private async void AddStudentAsync()
        {
            if (SelectedClass == null)
            {
                return;
            }

            try
            {
                // In a real implementation, this would show a dialog to get student details
                var newStudent = new Student
                {
                    ClassId = SelectedClass.ClassId,
                    Name = "New Student",
                    AbilityLevel = "Middle",
                    ReadingAge = "11.5"
                };

                IsBusy = true;
                await _studentRepository.AddStudentAsync(newStudent);
                Students.Add(newStudent);
                SelectedStudent = newStudent;

                _logger.LogInformation("Added new student: {StudentName} to class {ClassName}",
                    newStudent.Name, SelectedClass.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding student to class {ClassId}", SelectedClass.ClassId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Edits the selected student
        /// </summary>
        private async void EditStudentAsync()
        {
            if (SelectedStudent == null)
            {
                return;
            }

            try
            {
                // In a real implementation, this would show a dialog to edit student details
                SelectedStudent.Name = $"{SelectedStudent.Name} (Edited)";
                SelectedStudent.UpdatedAt = DateTime.UtcNow;

                IsBusy = true;
                await _studentRepository.UpdateStudentAsync(SelectedStudent);

                // Refresh the view
                OnPropertyChanged(nameof(SelectedStudent));

                _logger.LogInformation("Updated student: {StudentName}", SelectedStudent.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student {StudentId}", SelectedStudent.StudentId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Deletes the selected student
        /// </summary>
        private async void DeleteStudentAsync()
        {
            if (SelectedStudent == null)
            {
                return;
            }

            try
            {
                // In a real implementation, this would show a confirmation dialog
                var studentToDelete = SelectedStudent;

                IsBusy = true;
                await _studentRepository.DeleteStudentAsync(studentToDelete.StudentId);
                Students.Remove(studentToDelete);
                SelectedStudent = null;

                _logger.LogInformation("Deleted student: {StudentName}", studentToDelete.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student {StudentId}", SelectedStudent.StudentId);
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
        /// Imports class and student data
        /// </summary>
        private async void ImportAsync()
        {
            try
            {
                // In a real implementation, this would show a file picker and import data from Excel
                _logger.LogInformation("Import functionality not yet implemented");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data");
            }
        }

        /// <summary>
        /// Exports class and student data
        /// </summary>
        private async void ExportAsync()
        {
            try
            {
                // In a real implementation, this would show a file picker and export data to Excel
                _logger.LogInformation("Export functionality not yet implemented");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
            }
        }
    }
}
