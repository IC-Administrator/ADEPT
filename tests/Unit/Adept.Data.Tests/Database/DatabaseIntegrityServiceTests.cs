using Adept.Common.Interfaces;
using Adept.Data.Database;
using Adept.TestUtilities.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

// Use alias to avoid ambiguity with Moq.MockFactory
using TestMockFactory = Adept.TestUtilities.Helpers.MockFactory;

namespace Adept.Data.Tests.Database
{
    /// <summary>
    /// Unit tests for the DatabaseIntegrityService
    /// </summary>
    public class DatabaseIntegrityServiceTests
    {
        private readonly Mock<IDatabaseContext> _mockDatabaseContext;
        private readonly Mock<IDatabaseBackupService> _mockBackupService;
        private readonly Mock<ILogger<DatabaseIntegrityService>> _mockLogger;
        private readonly DatabaseIntegrityService _service;

        public DatabaseIntegrityServiceTests()
        {
            _mockDatabaseContext = TestMockFactory.CreateMockDatabaseContext();
            _mockBackupService = new Mock<IDatabaseBackupService>();
            _mockLogger = TestMockFactory.CreateMockLogger<DatabaseIntegrityService>();

            _service = new DatabaseIntegrityService(
                _mockDatabaseContext.Object,
                _mockBackupService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CheckIntegrityAsync_ValidDatabase_ReturnsValidResult()
        {
            // Arrange
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>(TestConstants.DatabaseQueries.IntegrityCheck, null))
                .ReturnsAsync("ok");
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>(TestConstants.DatabaseQueries.ForeignKeyCheck, null))
                .ReturnsAsync(0);

            // Act
            var result = await _service.CheckIntegrityAsync();

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.IsIntegrityOk);
            Assert.True(result.IsForeignKeysOk);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public async Task CheckIntegrityAsync_IntegrityCheckFails_ReturnsInvalidResult()
        {
            // Arrange
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>(TestConstants.DatabaseQueries.IntegrityCheck, null))
                .ReturnsAsync("corruption detected");
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>(TestConstants.DatabaseQueries.ForeignKeyCheck, null))
                .ReturnsAsync(0);

            // Act
            var result = await _service.CheckIntegrityAsync();

            // Assert
            Assert.False(result.IsValid);
            Assert.False(result.IsIntegrityOk);
            Assert.True(result.IsForeignKeysOk);
            Assert.Contains(result.Issues, i => i.Contains("integrity"));
        }

        [Fact]
        public async Task CheckIntegrityAsync_ForeignKeyCheckFails_ReturnsInvalidResult()
        {
            // Arrange
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>(TestConstants.DatabaseQueries.IntegrityCheck, null))
                .ReturnsAsync("ok");
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>(TestConstants.DatabaseQueries.ForeignKeyCheck, null))
                .ReturnsAsync(5); // 5 foreign key violations

            // Act
            var result = await _service.CheckIntegrityAsync();

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.IsIntegrityOk);
            Assert.False(result.IsForeignKeysOk);
            Assert.Contains("Foreign key constraints violated", result.Issues);
        }

        [Fact]
        public async Task PerformMaintenanceAsync_ExecutesMaintenanceQueries()
        {
            // Arrange
            bool vacuumCalled = false;
            bool analyzeCalled = false;
            bool checkpointCalled = false;

            _mockDatabaseContext.Setup(d => d.ExecuteNonQueryAsync(TestConstants.DatabaseQueries.Vacuum, null))
                .Callback(() => vacuumCalled = true)
                .Returns(Task.FromResult(1));

            _mockDatabaseContext.Setup(d => d.ExecuteNonQueryAsync(TestConstants.DatabaseQueries.Analyze, null))
                .Callback(() => analyzeCalled = true)
                .Returns(Task.FromResult(1));

            _mockDatabaseContext.Setup(d => d.ExecuteNonQueryAsync("PRAGMA optimize", null))
                .Callback(() => checkpointCalled = true)
                .Returns(Task.FromResult(1));

            // Act
            bool result = await _service.PerformMaintenanceAsync();

            // Assert
            Assert.True(result);
            Assert.True(vacuumCalled, "VACUUM should be called");
            Assert.True(analyzeCalled, "ANALYZE should be called");
            Assert.True(checkpointCalled, "CHECKPOINT should be called");
        }

        [Fact]
        public async Task AttemptRepairAsync_CreatesBackupBeforeRepair()
        {
            // Arrange
            string backupPath = "backup_path";
            _mockBackupService.Setup(b => b.CreateBackupAsync(It.IsAny<string>()))
                .ReturnsAsync(backupPath);

            // Act
            bool result = await _service.AttemptRepairAsync();

            // Assert
            Assert.True(result);
            _mockBackupService.Verify(b => b.CreateBackupAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CompleteIntegrityWorkflow_Test()
        {
            // Arrange
            // Setup backup service
            _mockBackupService.Setup(b => b.CreateBackupAsync(It.IsAny<string>()))
                .ReturnsAsync("backup_path");

            // Setup database context for initial integrity check (showing issues)
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>(TestConstants.DatabaseQueries.IntegrityCheck, null))
                .ReturnsAsync("ok");

            // Setup foreign key check to initially show issues, then show fixed after maintenance
            var fkCheckCallCount = 0;
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>(TestConstants.DatabaseQueries.ForeignKeyCheck, null))
                .ReturnsAsync(() => fkCheckCallCount++ == 0 ? 1 : 0);

            // Act - Complete workflow
            // 1. Check integrity
            var initialCheck = await _service.CheckIntegrityAsync();
            Assert.False(initialCheck.IsValid);
            Assert.False(initialCheck.IsForeignKeysOk);

            // 2. Perform maintenance
            bool maintenanceResult = await _service.PerformMaintenanceAsync();
            Assert.True(maintenanceResult);

            // 3. Attempt repair
            bool repairResult = await _service.AttemptRepairAsync();
            Assert.True(repairResult);

            // 4. Check integrity again
            var finalCheck = await _service.CheckIntegrityAsync();
            Assert.True(finalCheck.IsValid);
            Assert.True(finalCheck.IsIntegrityOk);
            Assert.True(finalCheck.IsForeignKeysOk);
        }
    }
}
