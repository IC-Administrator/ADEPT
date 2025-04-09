using Adept.UI.Commands;
using Adept.UI.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Adept.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the NotificationsView
    /// </summary>
    public class NotificationsViewModel : ViewModelBase
    {
        private readonly ILogger<NotificationsViewModel> _logger;
        private readonly INotificationService _notificationService;
        private bool _hasNotifications;

        /// <summary>
        /// Gets the notifications
        /// </summary>
        public ObservableCollection<Notification> Notifications => _notificationService.Notifications;

        /// <summary>
        /// Gets or sets whether there are notifications
        /// </summary>
        public bool HasNotifications
        {
            get => _hasNotifications;
            private set => SetProperty(ref _hasNotifications, value);
        }

        /// <summary>
        /// Gets the command to clear all notifications
        /// </summary>
        public ICommand ClearAllCommand { get; }

        /// <summary>
        /// Gets the command to remove a notification
        /// </summary>
        public ICommand RemoveNotificationCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsViewModel"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="notificationService">The notification service</param>
        public NotificationsViewModel(ILogger<NotificationsViewModel> logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;

            // Initialize commands
            ClearAllCommand = new RelayCommand(ClearAll);
            RemoveNotificationCommand = new RelayCommand<Notification>(RemoveNotification);

            // Subscribe to notifications collection changes
            Notifications.CollectionChanged += OnNotificationsCollectionChanged;

            // Initialize properties
            HasNotifications = Notifications.Count > 0;

            _logger.LogInformation("NotificationsViewModel initialized");
        }

        /// <summary>
        /// Handles the notifications collection changed event
        /// </summary>
        private void OnNotificationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasNotifications = Notifications.Count > 0;
        }

        /// <summary>
        /// Clears all notifications
        /// </summary>
        private void ClearAll()
        {
            _logger.LogInformation("Clearing all notifications");
            _notificationService.ClearAll();
        }

        /// <summary>
        /// Removes a notification
        /// </summary>
        private void RemoveNotification(Notification? notification)
        {
            if (notification == null)
            {
                return;
            }

            _logger.LogInformation("Removing notification: {Id}", notification.Id);
            Notifications.Remove(notification);
        }
    }
}
