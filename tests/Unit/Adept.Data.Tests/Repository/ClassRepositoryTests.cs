using Adept.Common.Interfaces;
using Adept.Core.Models;
using Adept.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Data.Tests.Repository
{
    /// <summary>
    /// Unit tests for the ClassRepository class
    /// </summary>
    public class ClassRepositoryTests
    {
        private readonly Mock<IDatabaseContext> _mockDatabaseContext;
        private readonly Mock<ILogger<ClassRepository>> _mockLogger;
        private readonly ClassRepository _repository;

        public ClassRepositoryTests()
        {
            _mockDatabaseContext = new Mock<IDatabaseContext>();
            _mockLogger = new Mock<ILogger<ClassRepository>>();
            _repository = new ClassRepository(_mockDatabaseContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllClassesAsync_ReturnsClasses()
        {
            // Arrange
            var expectedClasses = new List<Class>
            {
                new Class { ClassId = "1", ClassCode = "CS101", EducationLevel = "Undergraduate" },
                new Class { ClassId = "2", ClassCode = "CS102", EducationLevel = "Undergraduate" }
            };

            _mockDatabaseContext.Setup(x => x.QueryAsync<Class>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(expectedClasses);

            // Act
            var result = await _repository.GetAllClassesAsync();

            // Assert
            Assert.Equal(expectedClasses, result);
            _mockDatabaseContext.Verify(x => x.QueryAsync<Class>(
                It.IsAny<string>(),
                It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllClassesAsync_WhenExceptionOccurs_ReturnsEmptyList()
        {
            // Arrange
            _mockDatabaseContext.Setup(x => x.QueryAsync<Class>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _repository.GetAllClassesAsync();

            // Assert
            Assert.Empty(result);
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
        public async Task GetClassByIdAsync_WithValidId_ReturnsClass()
        {
            // Arrange
            var classId = "test-id";
            var expectedClass = new Class { ClassId = classId, ClassCode = "CS101", EducationLevel = "Undergraduate" };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync(expectedClass);

            // Act
            var result = await _repository.GetClassByIdAsync(classId);

            // Assert
            Assert.Equal(expectedClass, result);
            _mockDatabaseContext.Verify(x => x.QuerySingleOrDefaultAsync<Class>(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                             p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)),
                Times.Once);
        }

        [Fact]
        public async Task GetClassByIdAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetClassByIdAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetClassByIdAsync(""));
        }

        [Fact]
        public async Task GetClassByCodeAsync_WithValidCode_ReturnsClass()
        {
            // Arrange
            var classCode = "CS101";
            var expectedClass = new Class { ClassId = "1", ClassCode = classCode, EducationLevel = "Undergraduate" };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassCode") != null &&
                                 p.GetType().GetProperty("ClassCode").GetValue(p).ToString() == classCode)))
                .ReturnsAsync(expectedClass);

            // Act
            var result = await _repository.GetClassByCodeAsync(classCode);

            // Assert
            Assert.Equal(expectedClass, result);
            _mockDatabaseContext.Verify(x => x.QuerySingleOrDefaultAsync<Class>(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("ClassCode") != null &&
                             p.GetType().GetProperty("ClassCode").GetValue(p).ToString() == classCode)),
                Times.Once);
        }

        [Fact]
        public async Task GetClassByCodeAsync_WithInvalidCode_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullCode = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetClassByCodeAsync(nullCode!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetClassByCodeAsync(""));
        }

        [Fact]
        public async Task AddClassAsync_WithValidClass_ReturnsClassId()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = "test-id",
                ClassCode = "CS101",
                EducationLevel = "Undergraduate",
                CurrentTopic = "Variables"
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassCode") != null &&
                                 p.GetType().GetProperty("ClassCode").GetValue(p).ToString() == classEntity.ClassCode)))
                .ReturnsAsync((Class?)null);

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            // Act
            var result = await _repository.AddClassAsync(classEntity);

            // Assert
            Assert.Equal(classEntity.ClassId, result);
            _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                             p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classEntity.ClassId)),
                Times.Once);
        }

        [Fact]
        public async Task AddClassAsync_WithNullClass_ThrowsArgumentNullException()
        {
            // Act & Assert
            Class? nullClass = null;
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddClassAsync(nullClass!));
        }

        [Fact]
        public async Task AddClassAsync_WithInvalidClass_ThrowsArgumentException()
        {
            // Arrange
            var classWithoutCode = new Class
            {
                ClassId = "test-id",
                EducationLevel = "Undergraduate"
            };

            var classWithoutEducationLevel = new Class
            {
                ClassId = "test-id",
                ClassCode = "CS101"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddClassAsync(classWithoutCode));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddClassAsync(classWithoutEducationLevel));
        }

        [Fact]
        public async Task AddClassAsync_WithDuplicateClassCode_ThrowsInvalidOperationException()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = "test-id",
                ClassCode = "CS101",
                EducationLevel = "Undergraduate"
            };

            var existingClass = new Class
            {
                ClassId = "existing-id",
                ClassCode = "CS101",
                EducationLevel = "Undergraduate"
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassCode") != null &&
                                 p.GetType().GetProperty("ClassCode").GetValue(p).ToString() == classEntity.ClassCode)))
                .ReturnsAsync(existingClass);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddClassAsync(classEntity));
        }

        [Fact]
        public async Task UpdateClassAsync_WithValidClass_UpdatesClass()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = "test-id",
                ClassCode = "CS101",
                EducationLevel = "Undergraduate",
                CurrentTopic = "Variables"
            };

            var existingClass = new Class
            {
                ClassId = "test-id",
                ClassCode = "CS100",
                EducationLevel = "Undergraduate",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classEntity.ClassId)))
                .ReturnsAsync(existingClass);

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassCode") != null &&
                                 p.GetType().GetProperty("ClassCode").GetValue(p).ToString() == classEntity.ClassCode)))
                .ReturnsAsync((Class?)null);

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            // Act
            await _repository.UpdateClassAsync(classEntity);

            // Assert
            _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                             p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classEntity.ClassId)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateClassAsync_WithNullClass_ThrowsArgumentException()
        {
            // Act & Assert
            Class? nullClass = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.UpdateClassAsync(nullClass!));
        }

        [Fact]
        public async Task UpdateClassAsync_WithoutId_ThrowsInvalidOperationException()
        {
            // Arrange
            var classWithoutId = new Class
            {
                ClassCode = "CS101",
                EducationLevel = "Undergraduate"
            };

            // Make sure the mock doesn't return a class when queried
            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync((Class?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateClassAsync(classWithoutId));
        }

        [Fact]
        public async Task UpdateClassAsync_WithoutCode_ThrowsArgumentException()
        {
            // Arrange
            var classWithoutCode = new Class
            {
                ClassId = "test-id",
                EducationLevel = "Undergraduate"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.UpdateClassAsync(classWithoutCode));
        }

        [Fact]
        public async Task UpdateClassAsync_WithNonExistentClass_ThrowsInvalidOperationException()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = "test-id",
                ClassCode = "CS101",
                EducationLevel = "Undergraduate"
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classEntity.ClassId)))
                .ReturnsAsync((Class?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateClassAsync(classEntity));
        }

        [Fact]
        public async Task DeleteClassAsync_WithValidId_DeletesClass()
        {
            // Arrange
            var classId = "test-id";
            var existingClass = new Class
            {
                ClassId = classId,
                ClassCode = "CS101",
                EducationLevel = "Undergraduate"
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync(existingClass);

            var mockTransaction = new Mock<IDbTransaction>();
            _mockDatabaseContext.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(mockTransaction.Object);

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync(1);

            // Act
            bool result = await _repository.DeleteClassAsync(classId);

            // Assert
            Assert.True(result);
            _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                             p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)),
                Times.Once);
        }

        [Fact]
        public async Task DeleteClassAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.DeleteClassAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.DeleteClassAsync(""));
        }

        [Fact]
        public async Task DeleteClassAsync_WithNonExistentClass_ReturnsFalse()
        {
            // Arrange
            var classId = "test-id";

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync((Class?)null);

            // Act
            bool result = await _repository.DeleteClassAsync(classId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetStudentsForClassAsync_WithValidId_ReturnsStudents()
        {
            // Arrange
            var classId = "test-id";
            var expectedStudents = new List<Student>
            {
                new Student { StudentId = "1", ClassId = classId, Name = "John Doe" },
                new Student { StudentId = "2", ClassId = classId, Name = "Jane Smith" }
            };

            var existingClass = new Class
            {
                ClassId = classId,
                ClassCode = "CS101",
                EducationLevel = "Undergraduate"
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p) != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync(existingClass);

            _mockDatabaseContext.Setup(x => x.QueryAsync<Student>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p) != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync(expectedStudents);

            // Act
            var result = await _repository.GetStudentsForClassAsync(classId);

            // Assert
            Assert.Equal(expectedStudents, result);
            _mockDatabaseContext.Verify(x => x.QueryAsync<Student>(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                             p.GetType().GetProperty("ClassId") != null &&
                             p.GetType().GetProperty("ClassId").GetValue(p) != null &&
                             p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)),
                Times.Once);
        }

        [Fact]
        public async Task GetStudentsForClassAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetStudentsForClassAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetStudentsForClassAsync(""));
        }

        [Fact]
        public async Task GetStudentsForClassAsync_WithNonExistentClass_ReturnsEmptyList()
        {
            // Arrange
            var classId = "test-id";

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Class>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p) != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync((Class?)null);

            // Act
            var result = await _repository.GetStudentsForClassAsync(classId);

            // Assert
            Assert.Empty(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    (Exception?)null,
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }
    }
}
