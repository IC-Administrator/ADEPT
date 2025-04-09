using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace Adept.UI.Services
{
    /// <summary>
    /// Notification type enumeration
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Information notification
        /// </summary>
        Information,

        /// <summary>
        /// Success notification
        /// </summary>
        Success,

        /// <summary>
        /// Warning notification
        /// </summary>
        Warning,

        /// <summary>
        /// Error notification
        /// </summary>
        Error
    }

    /// <summary>
    /// Notification model
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Gets or sets the notification ID
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the notification message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification type
        /// </summary>
        public NotificationType Type { get; set; } = NotificationType.Information;

        /// <summary>
        /// Gets or sets the notification timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets whether the notification is visible
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets the notification duration in seconds
        /// </summary>
        public int DurationSeconds { get; set; } = 5;
    }

    /// <summary>
    /// Interface for notification service
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gets the active notifications
        /// </summary>
        ObservableCollection<Notification> Notifications { get; }

        /// <summary>
        /// Shows an information notification
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="durationSeconds">The notification duration in seconds</param>
        void ShowInformation(string message, int durationSeconds = 5);

        /// <summary>
        /// Shows a success notification
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="durationSeconds">The notification duration in seconds</param>
        void ShowSuccess(string message, int durationSeconds = 5);

        /// <summary>
        /// Shows a warning notification
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="durationSeconds">The notification duration in seconds</param>
        void ShowWarning(string message, int durationSeconds = 5);

        /// <summary>
        /// Shows an error notification
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="durationSeconds">The notification duration in seconds</param>
        void ShowError(string message, int durationSeconds = 5);

        /// <summary>
        /// Clears all notifications
        /// </summary>
        void ClearAll();
    }

    /// <summary>
    /// Implementation of notification service
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// Gets the active notifications
        /// </summary>
        public ObservableCollection<Notification> Notifications { get; } = new ObservableCollection<Notification>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// Shows an information notification
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="durationSeconds">The notification duration in seconds</param>
        public void ShowInformation(string message, int durationSeconds = 5)
        {
            ShowNotification(message, NotificationType.Information, durationSeconds);
        }

        /// <summary>
        /// Shows a success notification
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="durationSeconds">The notification duration in seconds</param>
        public void ShowSuccess(string message, int durationSeconds = 5)
        {
            ShowNotification(message, NotificationType.Success, durationSeconds);
        }

        /// <summary>
        /// Shows a warning notification
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="durationSeconds">The notification duration in seconds</param>
        public void ShowWarning(string message, int durationSeconds = 5)
        {
            ShowNotification(message, NotificationType.Warning, durationSeconds);
        }

        /// <summary>
        /// Shows an error notification
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="durationSeconds">The notification duration in seconds</param>
        public void ShowError(string message, int durationSeconds = 5)
        {
            ShowNotification(message, NotificationType.Error, durationSeconds);
        }

        /// <summary>
        /// Clears all notifications
        /// </summary>
        public void ClearAll()
        {
            _dispatcher.Invoke(() =>
            {
                Notifications.Clear();
            });
        }

        private void ShowNotification(string message, NotificationType type, int durationSeconds)
        {
            _logger.LogInformation("Showing notification: {Message} ({Type})", message, type);

            var notification = new Notification
            {
                Message = message,
                Type = type,
                DurationSeconds = durationSeconds
            };

            _dispatcher.Invoke(() =>
            {
                Notifications.Add(notification);
            });

            // Auto-remove notification after duration
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(durationSeconds));

                _dispatcher.Invoke(() =>
                {
                    if (Notifications.Contains(notification))
                    {
                        Notifications.Remove(notification);
                    }
                });
            });
        }
    }
}
