using Adept.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Adept.UI.Controls
{
    /// <summary>
    /// Interaction logic for WeekCalendarControl.xaml
    /// </summary>
    public partial class WeekCalendarControl : UserControl
    {
        private LessonPlan _draggedLesson;

        public WeekCalendarControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the MouseDown event on a lesson item to start drag operation
        /// </summary>
        private void Lesson_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                // Get the lesson from the Tag property
                if (element.Tag is LessonPlan lesson)
                {
                    _draggedLesson = lesson;
                    DragDrop.DoDragDrop(element, lesson, DragDropEffects.Move);
                }
            }
        }

        /// <summary>
        /// Handles the MouseMove event on a cell to enable drag and drop
        /// </summary>
        private void Cell_MouseMove(object sender, MouseEventArgs e)
        {
            // This method is needed to enable drag and drop
        }

        /// <summary>
        /// Handles the Drop event on a cell to move a lesson
        /// </summary>
        private void Cell_Drop(object sender, DragEventArgs e)
        {
            if (_draggedLesson != null && sender is FrameworkElement element && element.Tag is string cellTag)
            {
                // Parse the cell tag to get row and column
                var parts = cellTag.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int column))
                {
                    // Convert row and column to day and period
                    var day = GetDayFromColumn(column);
                    var period = GetPeriodFromRow(row);

                    // Call the command to move the lesson
                    if (DataContext is ViewModels.LessonPlannerViewModel viewModel)
                    {
                        viewModel.MoveLessonCommand.Execute(new { Lesson = _draggedLesson, Day = day, Period = period });
                    }
                }

                _draggedLesson = null;
            }
        }

        /// <summary>
        /// Gets the day of the week from a column index
        /// </summary>
        private string GetDayFromColumn(int column)
        {
            switch (column)
            {
                case 1: return "Monday";
                case 2: return "Tuesday";
                case 3: return "Wednesday";
                case 4: return "Thursday";
                case 5: return "Friday";
                default: return "Monday";
            }
        }

        /// <summary>
        /// Gets the period from a row index
        /// </summary>
        private string GetPeriodFromRow(int row)
        {
            switch (row)
            {
                case 0: return "Period 1";
                case 1: return "Period 2";
                case 2: return "Period 3";
                case 3: return "Period 4";
                case 4: return "Period 5";
                default: return "Period 1";
            }
        }
    }
}
