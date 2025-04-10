using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Adept.UI.Converters
{
    /// <summary>
    /// Converts a class ID to a color for visual differentiation
    /// </summary>
    public class ClassIdToColorConverter : IValueConverter
    {
        // Dictionary to store class ID to color mappings for consistency
        private static readonly Dictionary<Guid, SolidColorBrush> _colorMap = new Dictionary<Guid, SolidColorBrush>();
        
        // Predefined colors for classes
        private static readonly List<SolidColorBrush> _predefinedColors = new List<SolidColorBrush>
        {
            new SolidColorBrush(Colors.LightBlue),
            new SolidColorBrush(Colors.LightGreen),
            new SolidColorBrush(Colors.LightPink),
            new SolidColorBrush(Colors.LightYellow),
            new SolidColorBrush(Colors.LightCoral),
            new SolidColorBrush(Colors.LightSkyBlue),
            new SolidColorBrush(Colors.LightSalmon),
            new SolidColorBrush(Colors.LightSeaGreen),
            new SolidColorBrush(Colors.LightSteelBlue),
            new SolidColorBrush(Colors.LightGoldenrodYellow)
        };
        
        private static int _colorIndex = 0;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Guid classId)
            {
                // If we already have a color for this class ID, return it
                if (_colorMap.TryGetValue(classId, out var brush))
                {
                    return brush;
                }
                
                // Otherwise, assign a new color
                var newBrush = _predefinedColors[_colorIndex % _predefinedColors.Count];
                _colorIndex++;
                
                _colorMap[classId] = newBrush;
                return newBrush;
            }
            
            // Default color if not a valid class ID
            return new SolidColorBrush(Colors.LightGray);
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
