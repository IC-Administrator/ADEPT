using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Adept.UI.Converters
{
    /// <summary>
    /// Converts a string (calendar event ID) to a brush color indicating sync status
    /// </summary>
    public class StringToSyncStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the calendar event ID is not null or empty, the lesson is synced
            if (value is string calendarEventId && !string.IsNullOrEmpty(calendarEventId))
            {
                return new SolidColorBrush(Colors.Green);
            }
            
            // Otherwise, it's not synced
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
