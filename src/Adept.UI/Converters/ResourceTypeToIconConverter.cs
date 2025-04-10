using Adept.Core.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Adept.UI.Converters
{
    /// <summary>
    /// Converts a ResourceType to an icon image
    /// </summary>
    public class ResourceTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ResourceType resourceType)
            {
                string iconPath;
                
                switch (resourceType)
                {
                    case ResourceType.File:
                        iconPath = "/Adept.UI;component/Resources/Icons/file.png";
                        break;
                    case ResourceType.Link:
                        iconPath = "/Adept.UI;component/Resources/Icons/link.png";
                        break;
                    case ResourceType.Image:
                        iconPath = "/Adept.UI;component/Resources/Icons/image.png";
                        break;
                    case ResourceType.Document:
                        iconPath = "/Adept.UI;component/Resources/Icons/document.png";
                        break;
                    case ResourceType.Presentation:
                        iconPath = "/Adept.UI;component/Resources/Icons/presentation.png";
                        break;
                    case ResourceType.Spreadsheet:
                        iconPath = "/Adept.UI;component/Resources/Icons/spreadsheet.png";
                        break;
                    case ResourceType.Video:
                        iconPath = "/Adept.UI;component/Resources/Icons/video.png";
                        break;
                    case ResourceType.Audio:
                        iconPath = "/Adept.UI;component/Resources/Icons/audio.png";
                        break;
                    default:
                        iconPath = "/Adept.UI;component/Resources/Icons/other.png";
                        break;
                }
                
                try
                {
                    return new BitmapImage(new Uri(iconPath, UriKind.Relative));
                }
                catch
                {
                    // Return a default icon if the specified icon can't be loaded
                    return new BitmapImage(new Uri("/Adept.UI;component/Resources/Icons/other.png", UriKind.Relative));
                }
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
