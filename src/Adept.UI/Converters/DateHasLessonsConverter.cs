using Adept.UI.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Adept.UI.Converters
{
    /// <summary>
    /// Converts a date to a brush color indicating if the date has lessons
    /// </summary>
    public class DateHasLessonsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date && parameter is LessonPlannerViewModel viewModel)
            {
                if (viewModel.HasLessonsOnDate(date))
                {
                    return new SolidColorBrush(Colors.LightBlue);
                }
            }
            
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
