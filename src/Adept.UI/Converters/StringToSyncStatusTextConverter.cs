using System;
using System.Globalization;
using System.Windows.Data;

namespace Adept.UI.Converters
{
    /// <summary>
    /// Converts a string (calendar event ID) to a text description of sync status
    /// </summary>
    public class StringToSyncStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the calendar event ID is not null or empty, the lesson is synced
            if (value is string calendarEventId && !string.IsNullOrEmpty(calendarEventId))
            {
                return "Synchronized with Google Calendar";
            }
            
            // Otherwise, it's not synced
            return "Not synchronized with Google Calendar";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
