using Adept.Common.Interfaces;
using Adept.Data.Database;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Data.Tests.Database
{
    public class DatabaseIntegrityServiceTests
    {
        private readonly Mock<IDatabaseContext> _mockDatabaseContext;
        private readonly Mock<IDatabaseBackupService> _mockBackupService;
        private readonly Mock<ILogger<DatabaseIntegrityService>> _mockLogger;
        private readonly DatabaseIntegrityService _service;

        public DatabaseIntegrityServiceTests()
        {
            _mockDatabaseContext = new Mock<IDatabaseContext>();
            _mockBackupService = new Mock<IDatabaseBackupService>();
            _mockLogger = new Mock<ILogger<DatabaseIntegrityService>>();

            _service = new DatabaseIntegrityService(
                _mockDatabaseContext.Object,
                _mockBackupService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CheckIntegrityAsync_ValidDatabase_ReturnsValidResult()
        {
            // Arrange
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>("PRAGMA integrity_check", null))
                .ReturnsAsync("ok");
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>("PRAGMA foreign_key_check", null))
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
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>("PRAGMA integrity_check", null))
                .ReturnsAsync("error in page 123");
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>("PRAGMA foreign_key_check", null))
                .ReturnsAsync(0);

            // Act
            var result = await _service.CheckIntegrityAsync();

            // Assert
            Assert.False(result.IsValid);
            Assert.False(result.IsIntegrityOk);
            Assert.True(result.IsForeignKeysOk);
            Assert.Contains(result.Issues, i => i.Contains("Database integrity check failed"));
        }

        [Fact]
        public async Task CheckIntegrityAsync_ForeignKeyCheckFails_ReturnsInvalidResult()
        {
            // Arrange
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>("PRAGMA integrity_check", null))
                .ReturnsAsync("ok");
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>("PRAGMA foreign_key_check", null))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CheckIntegrityAsync();

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.IsIntegrityOk);
            Assert.False(result.IsForeignKeysOk);
            Assert.Contains(result.Issues, i => i.Contains("Foreign key constraints violated"));
        }

        [Fact]
        public async Task PerformMaintenanceAsync_ExecutesMaintenanceCommands()
        {
            // Arrange
            _mockBackupService.Setup(b => b.CreateBackupAsync("pre_maintenance"))
                .ReturnsAsync("backup_path");

            // Act
            bool result = await _service.PerformMaintenanceAsync();

            // Assert
            Assert.True(result);
            _mockBackupService.Verify(b => b.CreateBackupAsync("pre_maintenance"), Times.Once);
            _mockDatabaseContext.Verify(d => d.ExecuteNonQueryAsync("VACUUM"), Times.Once);
            _mockDatabaseContext.Verify(d => d.ExecuteNonQueryAsync("ANALYZE"), Times.Once);
            _mockDatabaseContext.Verify(d => d.ExecuteNonQueryAsync("PRAGMA optimize"), Times.Once);
        }

        [Fact]
        public async Task AttemptRepairAsync_DatabaseValid_ReturnsTrue()
        {
            // Arrange
            _mockBackupService.Setup(b => b.CreateBackupAsync("pre_repair"))
                .ReturnsAsync("backup_path");

            // Setup integrity check to return valid result
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>("PRAGMA integrity_check", null))
                .ReturnsAsync("ok");
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>("PRAGMA foreign_key_check", null))
                .ReturnsAsync(0);

            // Act
            bool result = await _service.AttemptRepairAsync();

            // Assert
            Assert.True(result);
            _mockBackupService.Verify(b => b.CreateBackupAsync("pre_repair"), Times.Once);
        }

        [Fact]
        public async Task AttemptRepairAsync_ForeignKeyIssues_AttemptsRepair()
        {
            // Arrange
            _mockBackupService.Setup(b => b.CreateBackupAsync("pre_repair"))
                .ReturnsAsync("backup_path");

            // Setup first integrity check to return foreign key issues
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<string>("PRAGMA integrity_check", null))
                .ReturnsAsync("ok");

            // First check shows foreign key issues, second check (after repair) shows no issues
            var checkCallCount = 0;
            _mockDatabaseContext.Setup(d => d.ExecuteScalarAsync<long>("PRAGMA foreign_key_check", null))
                .ReturnsAsync(() => checkCallCount++ == 0 ? 1 : 0);

            // Setup foreign key violations query
            _mockDatabaseContext.Setup(d => d.QueryAsync<object>(It.IsAny<string>()))
                .ReturnsAsync(new List<object> { new { Table = "Students", RowId = 1L, Parent = "Classes", Index = 1L } });

            // Act
            bool result = await _service.AttemptRepairAsync();

            // Assert
            Assert.True(result);
            _mockDatabaseContext.Verify(d => d.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
        }
    }
}
