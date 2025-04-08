using Adept.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Interface for database backup and recovery operations
    /// </summary>
    public interface IDatabaseBackupService
    {
        /// <summary>
        /// Creates a backup of the database
        /// </summary>
        /// <param name="backupName">Optional backup name (default: timestamp)</param>
        /// <returns>The path to the created backup file</returns>
        Task<string> CreateBackupAsync(string? backupName = null);

        /// <summary>
        /// Restores the database from a backup
        /// </summary>
        /// <param name="backupPath">The path to the backup file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RestoreFromBackupAsync(string backupPath);

        /// <summary>
        /// Gets a list of available backups
        /// </summary>
        /// <returns>List of backup file information</returns>
        Task<IEnumerable<DatabaseBackupInfo>> GetAvailableBackupsAsync();

        /// <summary>
        /// Creates a backup before applying migrations
        /// </summary>
        /// <returns>The path to the created backup file</returns>
        Task<string> CreateMigrationBackupAsync();

        /// <summary>
        /// Schedules periodic backups
        /// </summary>
        /// <param name="intervalHours">Interval in hours between backups</param>
        void SchedulePeriodicBackups(int intervalHours);

        /// <summary>
        /// Verifies the integrity of a backup file
        /// </summary>
        /// <param name="backupPath">The path to the backup file</param>
        /// <returns>True if the backup is valid, false otherwise</returns>
        Task<bool> VerifyBackupIntegrityAsync(string backupPath);
    }
}
