using System.Windows;
using System.Windows.Controls;
using Adept.UI.Services;

namespace Adept.UI.Controls
{
    /// <summary>
    /// Interaction logic for NotificationControl.xaml
    /// </summary>
    public partial class NotificationControl : UserControl
    {
        private readonly INotificationService _notificationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationControl"/> class
        /// </summary>
        /// <param name="notificationService">The notification service</param>
        public NotificationControl(INotificationService notificationService)
        {
            _notificationService = notificationService;
            InitializeComponent();
            DataContext = _notificationService;
        }

        /// <summary>
        /// Handles the close button click event
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Notification notification)
            {
                _notificationService.Notifications.Remove(notification);
            }
        }
    }
}
