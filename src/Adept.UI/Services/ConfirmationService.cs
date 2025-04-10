using Adept.UI.Commands;
using Adept.UI.Controls;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Adept.UI.Services
{
    /// <summary>
    /// Service for showing confirmation dialogs
    /// </summary>
    public class ConfirmationService : IConfirmationService
    {
        private readonly ILogger<ConfirmationService> _logger;
        private Grid _overlayGrid;
        private ConfirmationDialog _confirmationDialog;
        private TaskCompletionSource<bool> _tcs;

        public ConfirmationService(ILogger<ConfirmationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Shows a confirmation dialog and returns the result
        /// </summary>
        /// <param name="title">The title of the dialog</param>
        /// <param name="message">The message to display</param>
        /// <param name="confirmButtonText">Text for the confirm button</param>
        /// <param name="cancelButtonText">Text for the cancel button</param>
        /// <returns>True if confirmed, false if canceled</returns>
        public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmButtonText = "Confirm", string cancelButtonText = "Cancel")
        {
            try
            {
                _tcs = new TaskCompletionSource<bool>();

                // Create overlay grid
                _overlayGrid = new Grid
                {
                    Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
                    Opacity = 0
                };

                // Create confirmation dialog
                _confirmationDialog = new ConfirmationDialog
                {
                    Title = title,
                    Message = message,
                    ConfirmButtonText = confirmButtonText,
                    CancelButtonText = cancelButtonText,
                    ConfirmCommand = new RelayCommand(() => OnConfirm()),
                    CancelCommand = new RelayCommand(() => OnCancel())
                };

                // Add dialog to overlay
                _overlayGrid.Children.Add(_confirmationDialog);

                // Add overlay to visual tree
                var mainWindow = Application.Current.MainWindow;
                var mainGrid = mainWindow.Content as Grid;
                if (mainGrid != null)
                {
                    mainGrid.Children.Add(_overlayGrid);

                    // Animate overlay
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                    _overlayGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                }
                else
                {
                    _logger.LogError("Could not find main grid in main window");
                    return false;
                }

                // Wait for user response
                return await _tcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing confirmation dialog");
                return false;
            }
        }

        private void OnConfirm()
        {
            CloseDialog(true);
        }

        private void OnCancel()
        {
            CloseDialog(false);
        }

        private void CloseDialog(bool result)
        {
            try
            {
                // Animate overlay
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (s, e) =>
                {
                    // Remove overlay from visual tree
                    var mainWindow = Application.Current.MainWindow;
                    var mainGrid = mainWindow.Content as Grid;
                    if (mainGrid != null)
                    {
                        mainGrid.Children.Remove(_overlayGrid);
                    }

                    // Complete task
                    _tcs.SetResult(result);
                };

                _overlayGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing confirmation dialog");
                _tcs.SetResult(false);
            }
        }
    }
}
