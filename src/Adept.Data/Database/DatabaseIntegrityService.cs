using Adept.Common.Interfaces;
using Adept.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Data.Database
{
    /// <summary>
    /// Service for database integrity checks and maintenance
    /// </summary>
    public class DatabaseIntegrityService : IDatabaseIntegrityService
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly IDatabaseBackupService _backupService;
        private readonly ILogger<DatabaseIntegrityService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseIntegrityService"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="backupService">The backup service</param>
        /// <param name="logger">The logger</param>
        public DatabaseIntegrityService(
            IDatabaseContext databaseContext,
            IDatabaseBackupService backupService,
            ILogger<DatabaseIntegrityService> logger)
        {
            _databaseContext = databaseContext;
            _backupService = backupService;
            _logger = logger;
        }

        /// <summary>
        /// Checks the integrity of the database
        /// </summary>
        /// <returns>Integrity check results</returns>
        public async Task<DatabaseIntegrityResult> CheckIntegrityAsync()
        {
            try
            {
                _logger.LogInformation("Running database integrity check");

                // Run SQLite integrity check
                var integrityResult = await _databaseContext.ExecuteScalarAsync<string>("PRAGMA integrity_check");
                bool isIntegrityOk = integrityResult == "ok";

                // Run foreign key check
                var foreignKeyResult = await _databaseContext.ExecuteScalarAsync<long>("PRAGMA foreign_key_check");
                bool isForeignKeysOk = foreignKeyResult == 0;

                // Check for database corruption
                var result = new DatabaseIntegrityResult
                {
                    IsIntegrityOk = isIntegrityOk,
                    IsForeignKeysOk = isForeignKeysOk,
                    IsValid = isIntegrityOk && isForeignKeysOk,
                    CheckedAt = DateTime.UtcNow,
                    Issues = new List<string>()
                };

                if (!isIntegrityOk)
                {
                    result.Issues.Add($"Database integrity check failed: {integrityResult}");
                    _logger.LogWarning("Database integrity check failed: {Result}", integrityResult);
                }

                if (!isForeignKeysOk)
                {
                    result.Issues.Add("Foreign key constraints violated");
                    _logger.LogWarning("Foreign key constraints violated");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database integrity");
                return new DatabaseIntegrityResult
                {
                    IsValid = false,
                    CheckedAt = DateTime.UtcNow,
                    Issues = new List<string> { $"Error checking integrity: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// Performs database maintenance operations
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> PerformMaintenanceAsync()
        {
            try
            {
                _logger.LogInformation("Performing database maintenance");

                // Create a backup before maintenance
                await _backupService.CreateBackupAsync("pre_maintenance");

                // Run VACUUM to rebuild the database file
                await _databaseContext.ExecuteNonQueryAsync("VACUUM");

                // Run ANALYZE to update statistics
                await _databaseContext.ExecuteNonQueryAsync("ANALYZE");

                // Optimize the database
                await _databaseContext.ExecuteNonQueryAsync("PRAGMA optimize");

                _logger.LogInformation("Database maintenance completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing database maintenance");
                return false;
            }
        }

        /// <summary>
        /// Attempts to repair database issues
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> AttemptRepairAsync()
        {
            try
            {
                _logger.LogInformation("Attempting database repair");

                // Create a backup before repair
                string backupPath = await _backupService.CreateBackupAsync("pre_repair");

                // Check integrity first
                var integrityResult = await CheckIntegrityAsync();

                if (integrityResult.IsValid)
                {
                    _logger.LogInformation("Database integrity check passed, no repair needed");
                    return true;
                }

                // Try to fix foreign key issues
                if (!integrityResult.IsForeignKeysOk)
                {
                    await FixForeignKeyIssuesAsync();
                }

                // If integrity issues persist, try more aggressive repair
                if (!integrityResult.IsIntegrityOk)
                {
                    // For severe corruption, the best approach is often to restore from backup
                    _logger.LogWarning("Severe database corruption detected, restoration from backup may be required");

                    // Here we could implement more advanced repair strategies
                    // For now, we'll just recommend restoration
                    return false;
                }

                // Check integrity again after repair attempts
                var afterRepairResult = await CheckIntegrityAsync();

                if (afterRepairResult.IsValid)
                {
                    _logger.LogInformation("Database repair successful");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Database repair unsuccessful, consider restoring from backup");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attempting database repair");
                return false;
            }
        }

        /// <summary>
        /// Attempts to fix foreign key issues
        /// </summary>
        private async Task FixForeignKeyIssuesAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to fix foreign key issues");

                // Get foreign key violations
                var violations = await _databaseContext.QueryAsync<ForeignKeyViolation>(
                    "PRAGMA foreign_key_check");

                foreach (var violation in violations)
                {
                    _logger.LogWarning("Foreign key violation: Table {Table}, RowId {RowId}, Parent {Parent}, Index {Index}",
                        violation.Table, violation.RowId, violation.Parent, violation.Index);

                    // Option 1: Delete the offending row
                    await _databaseContext.ExecuteNonQueryAsync(
                        $"DELETE FROM {violation.Table} WHERE rowid = @RowId",
                        new { RowId = violation.RowId });

                    // Option 2 (alternative): Set foreign key to NULL if the column is nullable
                    // This would require schema information to determine the column name
                }

                _logger.LogInformation("Foreign key issues fixed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing foreign key issues");
                throw;
            }
        }

        /// <summary>
        /// Represents a foreign key violation
        /// </summary>
        private class ForeignKeyViolation
        {
            /// <summary>
            /// Gets or sets the table name
            /// </summary>
            public string Table { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the row ID
            /// </summary>
            public long RowId { get; set; }

            /// <summary>
            /// Gets or sets the parent table name
            /// </summary>
            public string Parent { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the index
            /// </summary>
            public long Index { get; set; }
        }
    }


}
