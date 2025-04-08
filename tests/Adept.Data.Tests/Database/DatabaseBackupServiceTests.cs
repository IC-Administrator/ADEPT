using Adept.Common.Interfaces;
using Adept.Data.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Data.Tests.Database
{
    public class DatabaseBackupServiceTests
    {
        private readonly Mock<IDatabaseContext> _mockDatabaseContext;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<DatabaseBackupService>> _mockLogger;
        private readonly string _testBackupDir;
        private readonly string _testDbPath;

        public DatabaseBackupServiceTests()
        {
            _mockDatabaseContext = new Mock<IDatabaseContext>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<DatabaseBackupService>>();

            // Setup test paths
            _testBackupDir = Path.Combine(Path.GetTempPath(), "adept_test_backups");
            _testDbPath = Path.Combine(Path.GetTempPath(), "adept_test.db");

            // Create test file
            if (!File.Exists(_testDbPath))
            {
                File.WriteAllText(_testDbPath, "Test database content");
            }

            // Setup configuration
            _mockConfiguration.Setup(c => c["Database:ConnectionString"]).Returns($"Data Source={_testDbPath}");
            _mockConfiguration.Setup(c => c["Database:BackupDirectory"]).Returns(_testBackupDir);
            _mockConfiguration.Setup(c => c["Database:MaxBackupCount"]).Returns("3");

            // Setup database context
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync("ok");
        }

        [Fact]
        public async Task CreateBackupAsync_CreatesBackupFile()
        {
            // Arrange
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);

            try
            {
                // Act
                string backupPath = await service.CreateBackupAsync("test_backup");

                // Assert
                Assert.True(File.Exists(backupPath));
                Assert.Contains("test_backup", backupPath);
                Assert.Contains(_testBackupDir, backupPath);
            }
            finally
            {
                // Cleanup
                CleanupTestFiles();
            }
        }

        [Fact]
        public async Task GetAvailableBackupsAsync_ReturnsBackupList()
        {
            // Arrange
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);

            try
            {
                // Create some test backups
                await service.CreateBackupAsync("backup1");
                await service.CreateBackupAsync("backup2");

                // Act
                var backups = await service.GetAvailableBackupsAsync();

                // Assert
                Assert.NotEmpty(backups);
                Assert.Equal(2, backups.Count());
                Assert.Contains(backups, b => b.FileName.Contains("backup1"));
                Assert.Contains(backups, b => b.FileName.Contains("backup2"));
            }
            finally
            {
                // Cleanup
                CleanupTestFiles();
            }
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_RemovesOldBackups()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["Database:MaxBackupCount"]).Returns("2");
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);

            try
            {
                // Create more backups than the max count
                await service.CreateBackupAsync("backup1");
                await Task.Delay(100); // Ensure different timestamps
                await service.CreateBackupAsync("backup2");
                await Task.Delay(100); // Ensure different timestamps
                await service.CreateBackupAsync("backup3");

                // Act
                var backups = await service.GetAvailableBackupsAsync();

                // Assert
                Assert.Equal(2, backups.Count());
                Assert.DoesNotContain(backups, b => b.FileName.Contains("backup1"));
                Assert.Contains(backups, b => b.FileName.Contains("backup2"));
                Assert.Contains(backups, b => b.FileName.Contains("backup3"));
            }
            finally
            {
                // Cleanup
                CleanupTestFiles();
            }
        }

        [Fact]
        public async Task VerifyBackupIntegrityAsync_ValidBackup_ReturnsTrue()
        {
            // Arrange
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync("ok");
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);

            try
            {
                // Create a backup
                string backupPath = await service.CreateBackupAsync("integrity_test");

                // Act
                bool isValid = await service.VerifyBackupIntegrityAsync(backupPath);

                // Assert
                Assert.True(isValid);
            }
            finally
            {
                // Cleanup
                CleanupTestFiles();
            }
        }

        [Fact]
        public async Task CreateMigrationBackupAsync_CreatesBackupWithCorrectName()
        {
            // Arrange
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);

            try
            {
                // Act
                string backupPath = await service.CreateMigrationBackupAsync();

                // Assert
                Assert.True(File.Exists(backupPath));
                Assert.Contains("pre_migration", backupPath);
            }
            finally
            {
                // Cleanup
                CleanupTestFiles();
            }
        }

        private void CleanupTestFiles()
        {
            // Clean up test files
            if (Directory.Exists(_testBackupDir))
            {
                try
                {
                    Directory.Delete(_testBackupDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
