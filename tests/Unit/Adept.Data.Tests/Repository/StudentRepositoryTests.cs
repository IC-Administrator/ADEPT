using Adept.Common.Interfaces;
using Adept.Core.Models;
using Adept.Data.Repositories;
using Adept.Data.Validation;
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
    /// Unit tests for the StudentRepository class
    /// </summary>
    public class StudentRepositoryTests
    {
        private readonly Mock<IDatabaseContext> _mockDatabaseContext;
        private readonly Mock<ILogger<StudentRepository>> _mockLogger;
        private readonly StudentRepository _repository;

        public StudentRepositoryTests()
        {
            _mockDatabaseContext = new Mock<IDatabaseContext>();
            _mockLogger = new Mock<ILogger<StudentRepository>>();
            _repository = new StudentRepository(_mockDatabaseContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllStudentsAsync_ReturnsStudents()
        {
            // Arrange
            var expectedStudents = new List<Student>
            {
                new Student { StudentId = "1", ClassId = "class-1", Name = "John Doe" },
                new Student { StudentId = "2", ClassId = "class-1", Name = "Jane Smith" }
            };

            _mockDatabaseContext.Setup(x => x.QueryAsync<Student>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(expectedStudents);

            // Act
            var result = await _repository.GetAllStudentsAsync();

            // Assert
            Assert.Equal(expectedStudents, result);
            _mockDatabaseContext.Verify(x => x.QueryAsync<Student>(
                It.IsAny<string>(),
                It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllStudentsAsync_WhenExceptionOccurs_ReturnsEmptyList()
        {
            // Arrange
            _mockDatabaseContext.Setup(x => x.QueryAsync<Student>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _repository.GetAllStudentsAsync();

            // Assert
            Assert.Empty(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetStudentByIdAsync_WithValidId_ReturnsStudent()
        {
            // Arrange
            var studentId = "test-id";
            var expectedStudent = new Student { StudentId = studentId, ClassId = "class-1", Name = "John Doe" };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<Student>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("StudentId") != null &&
                                 p.GetType().GetProperty("StudentId").GetValue(p).ToString() == studentId)))
                .ReturnsAsync(expectedStudent);

            // Act
            var result = await _repository.GetStudentByIdAsync(studentId);

            // Assert
            Assert.Equal(expectedStudent, result);
            _mockDatabaseContext.Verify(x => x.QuerySingleOrDefaultAsync<Student>(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("StudentId") != null &&
                             p.GetType().GetProperty("StudentId").GetValue(p).ToString() == studentId)),
                Times.Once);
        }

        [Fact]
        public async Task GetStudentByIdAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetStudentByIdAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetStudentByIdAsync(""));
        }

        [Fact]
        public async Task GetStudentsByClassIdAsync_WithValidClassId_ReturnsStudents()
        {
            // Arrange
            var classId = "class-1";
            var expectedStudents = new List<Student>
            {
                new Student { StudentId = "1", ClassId = classId, Name = "John Doe" },
                new Student { StudentId = "2", ClassId = classId, Name = "Jane Smith" }
            };

            _mockDatabaseContext.Setup(x => x.QueryAsync<Student>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync(expectedStudents);

            // Act
            var result = await _repository.GetStudentsByClassIdAsync(classId);

            // Assert
            Assert.Equal(expectedStudents, result);
            _mockDatabaseContext.Verify(x => x.QueryAsync<Student>(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                             p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)),
                Times.Once);
        }

        [Fact]
        public async Task GetStudentsByClassIdAsync_WithInvalidClassId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetStudentsByClassIdAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetStudentsByClassIdAsync(""));
        }

        [Fact]
        public async Task AddStudentAsync_WithValidStudent_ReturnsStudentId()
        {
            // Arrange
            var student = new Student
            {
                StudentId = "test-id",
                ClassId = "class-1",
                Name = "John Doe"
            };

            // Setup validation result
            var validationResult = new ValidationResult { };

            // Store the original method for restoration
            var originalValidateStudent = EntityValidator.ValidateStudent;

            // Use reflection to set the IsValid property
            var isValidProperty = typeof(ValidationResult).GetProperty("IsValid");
            isValidProperty?.SetValue(validationResult, true);

            // Set up our test delegate
            EntityValidator.ValidateStudent = _ => validationResult;

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            try
            {
                // Act
                var result = await _repository.AddStudentAsync(student);

                // Assert
                Assert.Equal(student.StudentId, result);
                _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("StudentId") != null &&
                                 p.GetType().GetProperty("StudentId").GetValue(p).ToString() == student.StudentId)),
                    Times.Once);
            }
            finally
            {
                // Restore the original method
                EntityValidator.ValidateStudent = originalValidateStudent;
            }
        }

        [Fact]
        public async Task AddStudentAsync_WithInvalidStudent_ThrowsValidationException()
        {
            // Arrange
            var student = new Student
            {
                StudentId = "test-id",
                ClassId = "class-1",
                Name = "" // Invalid - name is required
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _repository.AddStudentAsync(student));
        }

        [Fact]
        public async Task UpdateStudentAsync_WithValidStudent_UpdatesStudent()
        {
            // Arrange
            var student = new Student
            {
                StudentId = "test-id",
                ClassId = "class-1",
                Name = "John Doe"
            };

            // Setup validation result
            var validationResult = new ValidationResult { };

            // Store the original method for restoration
            var originalValidateStudent = EntityValidator.ValidateStudent;

            // Use reflection to set the IsValid property
            var isValidProperty = typeof(ValidationResult).GetProperty("IsValid");
            isValidProperty?.SetValue(validationResult, true);

            // Set up our test delegate
            EntityValidator.ValidateStudent = _ => validationResult;

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            try
            {
                // Act
                await _repository.UpdateStudentAsync(student);

                // Assert
                _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("StudentId") != null &&
                                 p.GetType().GetProperty("StudentId").GetValue(p).ToString() == student.StudentId)),
                    Times.Once);
            }
            finally
            {
                // Restore the original method
                EntityValidator.ValidateStudent = originalValidateStudent;
            }
        }

        [Fact]
        public async Task UpdateStudentAsync_WithInvalidStudent_ThrowsValidationException()
        {
            // Arrange
            var student = new Student
            {
                StudentId = "test-id",
                ClassId = "class-1",
                Name = "" // Invalid - name is required
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _repository.UpdateStudentAsync(student));
        }

        [Fact]
        public async Task DeleteStudentAsync_WithValidId_DeletesStudent()
        {
            // Arrange
            var studentId = "test-id";

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.Is<object>(p => ((dynamic)p).StudentId == studentId)))
                .ReturnsAsync(1);

            // Act
            await _repository.DeleteStudentAsync(studentId);

            // Assert
            _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p => ((dynamic)p).StudentId == studentId)),
                Times.Once);
        }

        [Fact]
        public async Task DeleteStudentAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.DeleteStudentAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.DeleteStudentAsync(""));
        }

        [Fact]
        public async Task AddStudentsAsync_WithValidStudents_ReturnsStudentIds()
        {
            // Arrange
            var students = new List<Student>
            {
                new Student { StudentId = "1", ClassId = "class-1", Name = "John Doe" },
                new Student { StudentId = "2", ClassId = "class-1", Name = "Jane Smith" }
            };

            // Setup validation result
            var validationResult = new ValidationResult { };

            // Store the original method for restoration
            var originalValidateStudent = EntityValidator.ValidateStudent;

            // Use reflection to set the IsValid property
            var isValidProperty = typeof(ValidationResult).GetProperty("IsValid");
            isValidProperty?.SetValue(validationResult, true);

            // Set up our test delegate
            EntityValidator.ValidateStudent = _ => validationResult;

            var mockTransaction = new Mock<IDbTransaction>();
            _mockDatabaseContext.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(mockTransaction.Object);

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            try
            {
                // Act
                var result = await _repository.AddStudentsAsync(students);

                // Assert
                Assert.Equal(students.Count, result.Count());
                Assert.Contains(students[0].StudentId, result);
                Assert.Contains(students[1].StudentId, result);
                mockTransaction.Verify(x => x.CommitAsync(), Times.Once);
            }
            finally
            {
                // Restore the original method
                EntityValidator.ValidateStudent = originalValidateStudent;
            }
        }

        [Fact]
        public async Task AddStudentsAsync_WithNullCollection_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddStudentsAsync(null));
        }

        [Fact]
        public async Task AddStudentsAsync_WithInvalidStudent_ThrowsValidationException()
        {
            // Arrange
            var students = new List<Student>
            {
                new Student { StudentId = "1", ClassId = "class-1", Name = "John Doe" },
                new Student { StudentId = "2", ClassId = "class-1", Name = "" } // Invalid - name is required
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _repository.AddStudentsAsync(students));
        }
    }
}
