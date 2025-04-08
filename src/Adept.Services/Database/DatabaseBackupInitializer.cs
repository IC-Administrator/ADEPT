using Adept.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Adept.Services.Database
{
    /// <summary>
    /// Service for initializing database backup schedules
    /// </summary>
    public class DatabaseBackupInitializer
    {
        private readonly IDatabaseBackupService _backupService;
        private readonly IDatabaseIntegrityService _integrityService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseBackupInitializer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseBackupInitializer"/> class
        /// </summary>
        /// <param name="backupService">The backup service</param>
        /// <param name="integrityService">The integrity service</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        public DatabaseBackupInitializer(
            IDatabaseBackupService backupService,
            IDatabaseIntegrityService integrityService,
            IConfiguration configuration,
            ILogger<DatabaseBackupInitializer> logger)
        {
            _backupService = backupService;
            _integrityService = integrityService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Initializes database backup and maintenance schedules
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing database backup and maintenance services");

                // Check database integrity on startup
                var integrityResult = await _integrityService.CheckIntegrityAsync();
                if (!integrityResult.IsValid)
                {
                    _logger.LogWarning("Database integrity check failed: {Issues}", string.Join(", ", integrityResult.Issues));
                    
                    // Attempt repair if there are issues
                    bool repaired = await _integrityService.AttemptRepairAsync();
                    if (repaired)
                    {
                        _logger.LogInformation("Database repair successful");
                    }
                    else
                    {
                        _logger.LogWarning("Database repair unsuccessful, manual intervention may be required");
                    }
                }
                else
                {
                    _logger.LogInformation("Database integrity check passed");
                }

                // Create initial backup if none exists
                var backups = await _backupService.GetAvailableBackupsAsync();
                if (!backups.Any())
                {
                    _logger.LogInformation("No database backups found, creating initial backup");
                    await _backupService.CreateBackupAsync("initial");
                }

                // Schedule periodic backups
                if (int.TryParse(_configuration["Database:AutoBackupIntervalHours"], out int intervalHours) && intervalHours > 0)
                {
                    _backupService.SchedulePeriodicBackups(intervalHours);
                }
                else
                {
                    // Default to daily backups
                    _backupService.SchedulePeriodicBackups(24);
                }

                // Schedule periodic maintenance
                // In a real implementation, this would set up a timer or background service
                _logger.LogInformation("Database backup and maintenance services initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database backup and maintenance services");
            }
        }
    }
}
