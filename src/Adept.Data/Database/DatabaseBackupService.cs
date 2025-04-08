using Adept.Common.Interfaces;
using Adept.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Data.Database
{
    /// <summary>
    /// Service for database backup and recovery operations
    /// </summary>
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly string _connectionString;
        private readonly string _databasePath;
        private readonly string _backupDirectory;
        private readonly int _maxBackupCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseBackupService"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        public DatabaseBackupService(
            IDatabaseContext databaseContext,
            IConfiguration configuration,
            ILogger<DatabaseBackupService> logger)
        {
            _databaseContext = databaseContext;
            _configuration = configuration;
            _logger = logger;

            // Get database path from connection string
            _connectionString = configuration["Database:ConnectionString"] ?? "Data Source=data/adept.db";
            _databasePath = _connectionString.Replace("Data Source=", "");

            // Set backup directory
            _backupDirectory = configuration["Database:BackupDirectory"] ?? "data/backups";
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }

            // Set max backup count (default: 10)
            if (!int.TryParse(configuration["Database:MaxBackupCount"], out _maxBackupCount))
            {
                _maxBackupCount = 10;
            }
        }

        /// <summary>
        /// Creates a backup of the database
        /// </summary>
        /// <param name="backupName">Optional backup name (default: timestamp)</param>
        /// <returns>The path to the created backup file</returns>
        public async Task<string> CreateBackupAsync(string? backupName = null)
        {
            try
            {
                // Ensure database is in a consistent state by flushing any pending changes
                await _databaseContext.ExecuteNonQueryAsync("PRAGMA wal_checkpoint(FULL)");

                // Generate backup filename
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = string.IsNullOrEmpty(backupName)
                    ? $"adept_{timestamp}.db"
                    : $"adept_{backupName}_{timestamp}.db";

                string backupPath = Path.Combine(_backupDirectory, fileName);

                // Copy the database file
                File.Copy(_databasePath, backupPath, true);

                _logger.LogInformation("Database backup created at {BackupPath}", backupPath);

                // Clean up old backups if we have too many
                await CleanupOldBackupsAsync();

                return backupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database backup");
                throw;
            }
        }

        /// <summary>
        /// Restores the database from a backup
        /// </summary>
        /// <param name="backupPath">The path to the backup file</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RestoreFromBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger.LogError("Backup file not found: {BackupPath}", backupPath);
                    return false;
                }

                // Create a backup of the current database before restoring
                string currentBackupPath = await CreateBackupAsync("pre_restore");

                // Close all database connections
                // This is a simplification - in a real app, you'd need to ensure all connections are closed
                await _databaseContext.ExecuteNonQueryAsync("PRAGMA optimize");

                try
                {
                    // Copy the backup file to the database location
                    File.Copy(backupPath, _databasePath, true);
                    _logger.LogInformation("Database restored from backup: {BackupPath}", backupPath);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error restoring database from backup");
                    
                    // Try to restore the pre-restore backup if the restore failed
                    try
                    {
                        File.Copy(currentBackupPath, _databasePath, true);
                        _logger.LogInformation("Reverted to pre-restore state after failed restore");
                    }
                    catch (Exception revertEx)
                    {
                        _logger.LogError(revertEx, "Failed to revert to pre-restore state");
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in restore process");
                return false;
            }
        }

        /// <summary>
        /// Gets a list of available backups
        /// </summary>
        /// <returns>List of backup file information</returns>
        public async Task<IEnumerable<DatabaseBackupInfo>> GetAvailableBackupsAsync()
        {
            try
            {
                // Use Task.Run to make this truly async
                return await Task.Run(() =>
                {
                    var backupFiles = Directory.GetFiles(_backupDirectory, "adept_*.db")
                        .Select(path => new FileInfo(path))
                        .OrderByDescending(file => file.CreationTime)
                        .Select(file => {
                            var info = new DatabaseBackupInfo
                            {
                                FilePath = file.FullName,
                                FileName = file.Name,
                                CreatedAt = file.CreationTime,
                                SizeInBytes = file.Length,
                                FormattedSize = FormatFileSize(file.Length)
                            };
                            return info;
                        })
                        .ToList(); // Materialize the query

                    return backupFiles.AsEnumerable();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available backups");
                return Enumerable.Empty<DatabaseBackupInfo>();
            }
        }

        /// <summary>
        /// Creates a backup before applying migrations
        /// </summary>
        /// <returns>The path to the created backup file</returns>
        public Task<string> CreateMigrationBackupAsync()
        {
            return CreateBackupAsync("pre_migration");
        }

        /// <summary>
        /// Schedules periodic backups
        /// </summary>
        /// <param name="intervalHours">Interval in hours between backups</param>
        public void SchedulePeriodicBackups(int intervalHours)
        {
            // In a real implementation, this would set up a timer or background service
            // For simplicity, we'll just log that this would be implemented
            _logger.LogInformation("Periodic backups would be scheduled every {IntervalHours} hours", intervalHours);
        }

        /// <summary>
        /// Cleans up old backups, keeping only the most recent ones
        /// </summary>
        private async Task CleanupOldBackupsAsync()
        {
            try
            {
                await Task.Run(() => {
                    var backupFiles = Directory.GetFiles(_backupDirectory, "adept_*.db")
                        .Select(path => new FileInfo(path))
                        .OrderByDescending(file => file.CreationTime)
                        .ToList();

                    // Keep only the most recent backups
                    if (backupFiles.Count > _maxBackupCount)
                    {
                        foreach (var file in backupFiles.Skip(_maxBackupCount))
                        {
                            try
                            {
                                File.Delete(file.FullName);
                                _logger.LogInformation("Deleted old backup: {BackupPath}", file.FullName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete old backup: {BackupPath}", file.FullName);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old backups");
            }
        }

        /// <summary>
        /// Verifies the integrity of a backup file
        /// </summary>
        /// <param name="backupPath">The path to the backup file</param>
        /// <returns>True if the backup is valid, false otherwise</returns>
        public async Task<bool> VerifyBackupIntegrityAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger.LogError("Backup file not found: {BackupPath}", backupPath);
                    return false;
                }

                // Create a temporary connection string for the backup file
                string tempConnectionString = $"Data Source={backupPath}";

                // Try to open the database and run a simple query
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(tempConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check";
                var result = await command.ExecuteScalarAsync();

                bool isValid = result?.ToString() == "ok";
                
                if (isValid)
                {
                    _logger.LogInformation("Backup integrity verified: {BackupPath}", backupPath);
                }
                else
                {
                    _logger.LogWarning("Backup integrity check failed: {BackupPath}", backupPath);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying backup integrity: {BackupPath}", backupPath);
                return false;
            }
        }

        /// <summary>
        /// Formats a file size in bytes to a human-readable string
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes</param>
        /// <returns>A human-readable size string</returns>
        private string FormatFileSize(long sizeInBytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            return sizeInBytes switch
            {
                < KB => $"{sizeInBytes} B",
                < MB => $"{sizeInBytes / KB:F2} KB",
                < GB => $"{sizeInBytes / MB:F2} MB",
                _ => $"{sizeInBytes / GB:F2} GB"
            };
        }
    }
}
