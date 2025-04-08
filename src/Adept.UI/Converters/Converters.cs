using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Adept.UI.Converters
{
    /// <summary>
    /// Converts a boolean value to a Visibility value
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a Visibility value
        /// </summary>
        /// <param name="value">The boolean value</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">Optional parameter</param>
        /// <param name="culture">The culture</param>
        /// <returns>Visibility.Visible if true, Visibility.Collapsed if false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a Visibility value to a boolean value
        /// </summary>
        /// <param name="value">The Visibility value</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">Optional parameter</param>
        /// <param name="culture">The culture</param>
        /// <returns>True if Visibility.Visible, false otherwise</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }

            return false;
        }
    }

    /// <summary>
    /// Converts a role string to a background brush
    /// </summary>
    public class RoleToBackgroundConverter : IValueConverter
    {
        /// <summary>
        /// Converts a role string to a background brush
        /// </summary>
        /// <param name="value">The role string</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">Optional parameter</param>
        /// <param name="culture">The culture</param>
        /// <returns>A brush based on the role</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                return role.ToLower() switch
                {
                    "user" => new SolidColorBrush(Colors.LightBlue),
                    "assistant" => new SolidColorBrush(Colors.LightGreen),
                    "system" => new SolidColorBrush(Colors.LightGray),
                    _ => new SolidColorBrush(Colors.White)
                };
            }

            return new SolidColorBrush(Colors.White);
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
