using System;
using System.Globalization;
using System.Windows.Data;

namespace Adept.UI.Converters
{
    /// <summary>
    /// Converts a boolean value to a string
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a string
        /// </summary>
        /// <param name="value">The boolean value</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">The parameter (format: "TrueString|FalseString")</param>
        /// <param name="culture">The culture</param>
        /// <returns>The string value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string format)
            {
                var parts = format.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }

            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Converts a string value to a boolean
        /// </summary>
        /// <param name="value">The string value</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">The parameter</param>
        /// <param name="culture">The culture</param>
        /// <returns>The boolean value</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
