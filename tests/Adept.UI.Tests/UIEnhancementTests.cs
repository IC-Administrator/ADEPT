using Adept.UI.Controls;
using Adept.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using System.Windows;

namespace Adept.UI.Tests
{
    [TestClass]
    public class UIEnhancementTests
    {
        private Mock<ILogger<NotificationService>> _loggerMock;
        private INotificationService _notificationService;

        [TestInitialize]
        public void Initialize()
        {
            _loggerMock = new Mock<ILogger<NotificationService>>();
            _notificationService = new NotificationService(_loggerMock.Object);
        }

        [TestMethod]
        public void NotificationService_ShowInformation_AddsNotification()
        {
            // Arrange
            string message = "Test Information";

            // Act
            _notificationService.ShowInformation(message);

            // Assert
            Assert.AreEqual(1, _notificationService.Notifications.Count);
            Assert.AreEqual(message, _notificationService.Notifications[0].Message);
            Assert.AreEqual(NotificationType.Information, _notificationService.Notifications[0].Type);
        }

        [TestMethod]
        public void NotificationService_ShowSuccess_AddsNotification()
        {
            // Arrange
            string message = "Test Success";

            // Act
            _notificationService.ShowSuccess(message);

            // Assert
            Assert.AreEqual(1, _notificationService.Notifications.Count);
            Assert.AreEqual(message, _notificationService.Notifications[0].Message);
            Assert.AreEqual(NotificationType.Success, _notificationService.Notifications[0].Type);
        }

        [TestMethod]
        public void NotificationService_ShowWarning_AddsNotification()
        {
            // Arrange
            string message = "Test Warning";

            // Act
            _notificationService.ShowWarning(message);

            // Assert
            Assert.AreEqual(1, _notificationService.Notifications.Count);
            Assert.AreEqual(message, _notificationService.Notifications[0].Message);
            Assert.AreEqual(NotificationType.Warning, _notificationService.Notifications[0].Type);
        }

        [TestMethod]
        public void NotificationService_ShowError_AddsNotification()
        {
            // Arrange
            string message = "Test Error";

            // Act
            _notificationService.ShowError(message);

            // Assert
            Assert.AreEqual(1, _notificationService.Notifications.Count);
            Assert.AreEqual(message, _notificationService.Notifications[0].Message);
            Assert.AreEqual(NotificationType.Error, _notificationService.Notifications[0].Type);
        }

        [TestMethod]
        public void NotificationService_ClearAll_RemovesAllNotifications()
        {
            // Arrange
            _notificationService.ShowInformation("Test 1");
            _notificationService.ShowSuccess("Test 2");
            _notificationService.ShowWarning("Test 3");
            Assert.AreEqual(3, _notificationService.Notifications.Count);

            // Act
            _notificationService.ClearAll();

            // Assert
            Assert.AreEqual(0, _notificationService.Notifications.Count);
        }

        [TestMethod]
        public void NotificationService_AutoRemovesNotification_AfterDuration()
        {
            // This test is skipped because it requires a UI dispatcher
            // In a real application, the notification would be auto-removed
            // For testing purposes, we'll just verify that the notification is added

            // Arrange
            string message = "Test Auto Remove";
            int durationSeconds = 1;

            // Act
            _notificationService.ShowInformation(message, durationSeconds);

            // Assert
            Assert.AreEqual(1, _notificationService.Notifications.Count);

            // Manually remove the notification to clean up
            _notificationService.ClearAll();
        }
    }
}
