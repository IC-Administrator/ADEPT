using Adept.Common.Interfaces;
using Adept.Data.Repositories;
using Adept.TestUtilities.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Data.Tests.Repository
{
    /// <summary>
    /// Unit tests for the BaseRepository class
    /// </summary>
    public class BaseRepositoryTests
    {
        private readonly Mock<IDatabaseContext> _mockDatabaseContext;
        private readonly Mock<ILogger> _mockLogger;
        private readonly TestBaseRepository _repository;

        public BaseRepositoryTests()
        {
            _mockDatabaseContext = new Mock<IDatabaseContext>();
            _mockLogger = new Mock<ILogger>();
            _repository = new TestBaseRepository(_mockDatabaseContext.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullDatabaseContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestBaseRepository(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestBaseRepository(_mockDatabaseContext.Object, null));
        }

        [Fact]
        public async Task ExecuteWithErrorHandlingAsync_WhenOperationSucceeds_ReturnsResult()
        {
            // Arrange
            var expectedResult = new List<TestEntity> { new TestEntity { Id = "1", Name = "Test" } };
            
            // Act
            var result = await _repository.TestExecuteWithErrorHandlingAsync(
                () => Task.FromResult(expectedResult),
                "Test error message",
                new List<TestEntity>());

            // Assert
            Assert.Same(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithErrorHandlingAsync_WhenOperationFails_ReturnsDefaultValue()
        {
            // Arrange
            var defaultValue = new List<TestEntity>();
            
            // Act
            var result = await _repository.TestExecuteWithErrorHandlingAsync(
                () => throw new Exception("Test exception"),
                "Test error message",
                defaultValue);

            // Assert
            Assert.Same(defaultValue, result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteWithErrorHandlingAndThrowAsync_WhenOperationSucceeds_ReturnsResult()
        {
            // Arrange
            var expectedResult = "test-result";
            
            // Act
            var result = await _repository.TestExecuteWithErrorHandlingAndThrowAsync(
                () => Task.FromResult(expectedResult),
                "Test error message");

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithErrorHandlingAndThrowAsync_WhenOperationFails_LogsAndRethrows()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _repository.TestExecuteWithErrorHandlingAndThrowAsync(
                    () => throw expectedException,
                    "Test error message"));
            
            Assert.Same(expectedException, exception);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WhenOperationSucceeds_CommitsTransaction()
        {
            // Arrange
            var expectedResult = "test-result";
            var mockTransaction = new Mock<IDbTransaction>();
            _mockDatabaseContext.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(mockTransaction.Object);
            
            // Act
            var result = await _repository.TestExecuteInTransactionAsync(
                async (transaction) => 
                {
                    Assert.Same(mockTransaction.Object, transaction);
                    return expectedResult;
                },
                "Test error message");

            // Assert
            Assert.Equal(expectedResult, result);
            mockTransaction.Verify(x => x.CommitAsync(), Times.Once);
            mockTransaction.Verify(x => x.RollbackAsync(), Times.Never);
            mockTransaction.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WhenOperationFails_RollsBackTransaction()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            var mockTransaction = new Mock<IDbTransaction>();
            _mockDatabaseContext.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(mockTransaction.Object);
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _repository.TestExecuteInTransactionAsync(
                    async (transaction) => throw expectedException,
                    "Test error message"));
            
            Assert.Same(expectedException, exception);
            mockTransaction.Verify(x => x.CommitAsync(), Times.Never);
            mockTransaction.Verify(x => x.RollbackAsync(), Times.Once);
            mockTransaction.Verify(x => x.Dispose(), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void ValidateEntityNotNull_WithNullEntity_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                _repository.TestValidateEntityNotNull(null, "testEntity"));
            
            Assert.Equal("testEntity", exception.ParamName);
        }

        [Fact]
        public void ValidateEntityNotNull_WithNonNullEntity_DoesNotThrow()
        {
            // Act & Assert
            _repository.TestValidateEntityNotNull(new TestEntity(), "testEntity");
            // No exception means the test passes
        }

        [Fact]
        public void ValidateStringNotNullOrEmpty_WithNullString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                _repository.TestValidateStringNotNullOrEmpty(null, "testString"));
            
            Assert.Equal("testString", exception.ParamName);
        }

        [Fact]
        public void ValidateStringNotNullOrEmpty_WithEmptyString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                _repository.TestValidateStringNotNullOrEmpty("", "testString"));
            
            Assert.Equal("testString", exception.ParamName);
        }

        [Fact]
        public void ValidateStringNotNullOrEmpty_WithNonEmptyString_DoesNotThrow()
        {
            // Act & Assert
            _repository.TestValidateStringNotNullOrEmpty("test", "testString");
            // No exception means the test passes
        }

        [Fact]
        public void ValidateId_WithNullId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                _repository.TestValidateId(null, "testEntity"));
            
            Assert.Contains("ID cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ValidateId_WithEmptyId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                _repository.TestValidateId("", "testEntity"));
            
            Assert.Contains("ID cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ValidateId_WithValidId_DoesNotThrow()
        {
            // Act & Assert
            _repository.TestValidateId("test-id", "testEntity");
            // No exception means the test passes
        }

        /// <summary>
        /// Test implementation of BaseRepository for testing
        /// </summary>
        private class TestBaseRepository : BaseRepository<TestEntity>
        {
            public TestBaseRepository(IDatabaseContext databaseContext, ILogger logger)
                : base(databaseContext, logger)
            {
            }

            public async Task<TResult> TestExecuteWithErrorHandlingAsync<TResult>(
                Func<Task<TResult>> operation,
                string errorMessage,
                TResult defaultValue)
            {
                return await ExecuteWithErrorHandlingAsync(operation, errorMessage, defaultValue);
            }

            public async Task<TResult> TestExecuteWithErrorHandlingAndThrowAsync<TResult>(
                Func<Task<TResult>> operation,
                string errorMessage)
            {
                return await ExecuteWithErrorHandlingAndThrowAsync(operation, errorMessage);
            }

            public async Task<TResult> TestExecuteInTransactionAsync<TResult>(
                Func<IDbTransaction, Task<TResult>> operation,
                string errorMessage)
            {
                return await ExecuteInTransactionAsync(operation, errorMessage);
            }

            public void TestValidateEntityNotNull(TestEntity entity, string entityName)
            {
                ValidateEntityNotNull(entity, entityName);
            }

            public void TestValidateStringNotNullOrEmpty(string value, string propertyName)
            {
                ValidateStringNotNullOrEmpty(value, propertyName);
            }

            public void TestValidateId(string id, string entityName)
            {
                ValidateId(id, entityName);
            }
        }

        /// <summary>
        /// Test entity for testing
        /// </summary>
        private class TestEntity
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}
