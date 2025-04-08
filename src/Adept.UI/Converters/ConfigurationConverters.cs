using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Adept.UI.Converters
{
    /// <summary>
    /// Converts a string to a visibility value based on equality
    /// </summary>
    public class StringEqualityToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a string to a visibility value based on equality
        /// </summary>
        /// <param name="value">The string value</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">The comparison string</param>
        /// <param name="culture">The culture</param>
        /// <returns>Visibility.Visible if equal, Visibility.Collapsed if not</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string comparisonValue)
            {
                return stringValue.Equals(comparisonValue, StringComparison.OrdinalIgnoreCase) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean value to a status string
    /// </summary>
    public class BooleanToStatusConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a status string
        /// </summary>
        /// <param name="value">The boolean value</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">Optional parameter</param>
        /// <param name="culture">The culture</param>
        /// <returns>"Running" if true, "Stopped" if false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Running" : "Stopped";
            }

            return "Unknown";
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean value to a color
    /// </summary>
    public class BooleanToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a color
        /// </summary>
        /// <param name="value">The boolean value</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">Optional parameter</param>
        /// <param name="culture">The culture</param>
        /// <returns>Green if true, Red if false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }

            return new SolidColorBrush(Colors.Gray);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
