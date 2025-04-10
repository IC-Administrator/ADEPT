using Adept.Common.Interfaces;
using Adept.Data.Database;
using Adept.Data.Tests;
using Adept.TestUtilities.Helpers;
using Adept.TestUtilities.TestBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

// Use alias to avoid ambiguity with Moq.MockFactory
using TestMockFactory = Adept.TestUtilities.Helpers.MockFactory;

namespace Adept.Data.Tests.Database
{
    /// <summary>
    /// Unit tests for the DatabaseBackupService
    /// </summary>
    public class DatabaseBackupServiceTests : DatabaseTestBase
    {
        private readonly Mock<IDatabaseContext> _mockDatabaseContext;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<DatabaseBackupService>> _mockLogger;
        private readonly string _testBackupDir;

        public DatabaseBackupServiceTests()
        {
            _mockDatabaseContext = TestMockFactory.CreateMockDatabaseContext();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = TestMockFactory.CreateMockLogger<DatabaseBackupService>();

            // Setup test paths
            _testBackupDir = TestConstants.DatabasePaths.TestBackupDirectory;

            // Setup configuration
            _mockConfiguration.Setup(c => c["Database:ConnectionString"]).Returns($"Data Source={TestDatabasePath}");
            _mockConfiguration.Setup(c => c["Database:BackupDirectory"]).Returns(_testBackupDir);
            _mockConfiguration.Setup(c => c["Database:MaxBackupCount"]).Returns("3");

            // Create test directory if it doesn't exist
            Directory.CreateDirectory(_testBackupDir);
        }

        protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            // Not needed for these tests
        }

        protected override Task InitializeDatabaseAsync()
        {
            // Create test file
            if (!File.Exists(TestDatabasePath))
            {
                File.WriteAllText(TestDatabasePath, "Test database content");
            }
            return Task.CompletedTask;
        }

        [Fact]
        public async Task CreateBackupAsync_CreatesBackupFile()
        {
            // Arrange
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);
            var backupName = TestConstants.BackupNames.TestBackup;

            try
            {
                // Act
                string backupPath = await service.CreateBackupAsync(backupName);

                // Assert
                AssertExtensions.FileExists(backupPath);
                Assert.Contains(backupName, backupPath);
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
                await service.CreateBackupAsync("test1");
                await service.CreateBackupAsync("test2");

                // Act
                var backups = await service.GetAvailableBackupsAsync();

                // Assert
                Assert.True(backups.Count() >= 2);
                Assert.Contains(backups, b => b.FileName.Contains("test1"));
                Assert.Contains(backups, b => b.FileName.Contains("test2"));
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
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);

            try
            {
                // Create a backup
                string backupPath = await service.CreateBackupAsync("integrity_test");

                // Act
                bool result = await service.VerifyBackupIntegrityAsync(backupPath);

                // Assert
                Assert.True(result);
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
                AssertExtensions.FileExists(backupPath);
                Assert.Contains("pre_migration", backupPath);
            }
            finally
            {
                // Cleanup
                CleanupTestFiles();
            }
        }

        [Fact]
        public async Task RestoreFromBackupAsync_RestoresDatabase()
        {
            // Arrange
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);
            string originalContent = "Original database content";
            string modifiedContent = "Modified database content";

            try
            {
                // Create original database
                File.WriteAllText(TestDatabasePath, originalContent);

                // Create a backup
                string backupPath = await service.CreateBackupAsync("restore_test");

                // Modify the database
                File.WriteAllText(TestDatabasePath, modifiedContent);

                // Act
                bool result = await service.RestoreFromBackupAsync(backupPath);

                // Assert
                Assert.True(result);
                Assert.Equal(originalContent, File.ReadAllText(TestDatabasePath));
            }
            finally
            {
                // Cleanup
                CleanupTestFiles();
            }
        }

        [Fact]
        public async Task CompleteBackupWorkflowTest()
        {
            // Arrange
            var service = new DatabaseBackupService(_mockDatabaseContext.Object, _mockConfiguration.Object, _mockLogger.Object);
            string initialContent = "Initial database content";

            try
            {
                // 1. Create initial database
                File.WriteAllText(TestDatabasePath, initialContent);

                // 2. Create first backup
                string backup1 = await service.CreateBackupAsync("workflow_test_1");
                AssertExtensions.FileExists(backup1);

                // 3. Modify database
                File.WriteAllText(TestDatabasePath, "Modified content 1");

                // 4. Create second backup
                string backup2 = await service.CreateBackupAsync("workflow_test_2");
                AssertExtensions.FileExists(backup2);

                // 5. Get available backups
                var backups = await service.GetAvailableBackupsAsync();
                Assert.True(backups.Count() >= 2);
                Assert.Contains(backups, b => b.FileName.Contains("workflow_test_1"));
                Assert.Contains(backups, b => b.FileName.Contains("workflow_test_2"));

                // 6. Verify backup integrity
                await service.VerifyBackupIntegrityAsync(backup1);

                // 7. Restore from first backup
                bool restoreResult = await service.RestoreFromBackupAsync(backup1);
                Assert.True(restoreResult, "Restore operation should succeed");

                // 8. Verify the file was restored correctly
                Assert.Equal(initialContent, File.ReadAllText(TestDatabasePath));
            }
            finally
            {
                // Cleanup
                CleanupTestFiles();
            }
        }

        private void CleanupTestFiles()
        {
            try
            {
                // Clean up backup directory
                if (Directory.Exists(_testBackupDir))
                {
                    Directory.Delete(_testBackupDir, true);
                }
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }
        }
    }
}
