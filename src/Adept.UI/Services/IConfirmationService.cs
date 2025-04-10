using System.Threading.Tasks;

namespace Adept.UI.Services
{
    /// <summary>
    /// Interface for confirmation dialog service
    /// </summary>
    public interface IConfirmationService
    {
        /// <summary>
        /// Shows a confirmation dialog and returns the result
        /// </summary>
        /// <param name="title">The title of the dialog</param>
        /// <param name="message">The message to display</param>
        /// <param name="confirmButtonText">Text for the confirm button</param>
        /// <param name="cancelButtonText">Text for the cancel button</param>
        /// <returns>True if confirmed, false if canceled</returns>
        Task<bool> ShowConfirmationAsync(string title, string message, string confirmButtonText = "Confirm", string cancelButtonText = "Cancel");
    }
}
