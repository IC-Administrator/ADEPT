using Adept.Common.Models;
using System.Threading.Tasks;

namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Interface for database integrity checks and maintenance
    /// </summary>
    public interface IDatabaseIntegrityService
    {
        /// <summary>
        /// Checks the integrity of the database
        /// </summary>
        /// <returns>Integrity check results</returns>
        Task<DatabaseIntegrityResult> CheckIntegrityAsync();

        /// <summary>
        /// Performs database maintenance operations
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> PerformMaintenanceAsync();

        /// <summary>
        /// Attempts to repair database issues
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> AttemptRepairAsync();
    }
}
