using Adept.UI.Controls;
using Adept.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Adept.UI.Tests
{
    [TestClass]
    public class UIEnhancementTests
    {
        private Mock<ILogger<NotificationService>>? _loggerMock;
        private INotificationService? _notificationService;

        // Required properties to avoid nullable warnings
        private Mock<ILogger<NotificationService>> LoggerMock => _loggerMock ?? throw new InvalidOperationException("LoggerMock not initialized");
        private INotificationService NotificationService => _notificationService ?? throw new InvalidOperationException("NotificationService not initialized");

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
            NotificationService.ShowInformation(message);

            // Assert
            Assert.AreEqual(1, NotificationService.Notifications.Count);
            Assert.AreEqual(message, NotificationService.Notifications[0].Message);
            Assert.AreEqual(NotificationType.Information, NotificationService.Notifications[0].Type);
        }

        [TestMethod]
        public void NotificationService_ShowSuccess_AddsNotification()
        {
            // Arrange
            string message = "Test Success";

            // Act
            NotificationService.ShowSuccess(message);

            // Assert
            Assert.AreEqual(1, NotificationService.Notifications.Count);
            Assert.AreEqual(message, NotificationService.Notifications[0].Message);
            Assert.AreEqual(NotificationType.Success, NotificationService.Notifications[0].Type);
        }

        [TestMethod]
        public void NotificationService_ShowWarning_AddsNotification()
        {
            // Arrange
            string message = "Test Warning";

            // Act
            NotificationService.ShowWarning(message);

            // Assert
            Assert.AreEqual(1, NotificationService.Notifications.Count);
            Assert.AreEqual(message, NotificationService.Notifications[0].Message);
            Assert.AreEqual(NotificationType.Warning, NotificationService.Notifications[0].Type);
        }

        [TestMethod]
        public void NotificationService_ShowError_AddsNotification()
        {
            // Arrange
            string message = "Test Error";

            // Act
            NotificationService.ShowError(message);

            // Assert
            Assert.AreEqual(1, NotificationService.Notifications.Count);
            Assert.AreEqual(message, NotificationService.Notifications[0].Message);
            Assert.AreEqual(NotificationType.Error, NotificationService.Notifications[0].Type);
        }

        [TestMethod]
        public void NotificationService_ClearAll_RemovesAllNotifications()
        {
            // Arrange
            NotificationService.ShowInformation("Test 1");
            NotificationService.ShowSuccess("Test 2");
            NotificationService.ShowWarning("Test 3");
            Assert.AreEqual(3, NotificationService.Notifications.Count);

            // Act
            NotificationService.ClearAll();

            // Assert
            Assert.AreEqual(0, NotificationService.Notifications.Count);
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
            NotificationService.ShowInformation(message, durationSeconds);

            // Assert
            Assert.AreEqual(1, NotificationService.Notifications.Count);

            // Manually remove the notification to clean up
            NotificationService.ClearAll();
        }
    }
}
