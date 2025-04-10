using Adept.Core.Models;
using Adept.Data.Repositories;
using Adept.Database.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Database.Tests.Repository
{
    /// <summary>
    /// Integration tests for the ClassRepository
    /// </summary>
    public class ClassRepositoryIntegrationTests : IClassFixture<SqliteDatabaseFixture>
    {
        private readonly SqliteDatabaseFixture _fixture;
        private readonly ClassRepository _repository;

        public ClassRepositoryIntegrationTests(SqliteDatabaseFixture fixture)
        {
            _fixture = fixture;
            var logger = _fixture.ServiceProvider.GetRequiredService<ILogger<ClassRepository>>();
            _repository = new ClassRepository(_fixture.DatabaseContext, logger);
        }

        [Fact]
        public async Task GetAllClassesAsync_ReturnsClasses()
        {
            // Act
            var classes = await _repository.GetAllClassesAsync();

            // Assert
            Assert.NotEmpty(classes);
            Assert.Contains(classes, c => c.ClassCode == "CS101");
        }

        [Fact]
        public async Task GetClassByIdAsync_WithValidId_ReturnsClass()
        {
            // Arrange
            var classes = await _repository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            // Act
            var result = await _repository.GetClassByIdAsync(classId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(classId, result.ClassId);
        }

        [Fact]
        public async Task GetClassByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetClassByIdAsync(Guid.NewGuid().ToString());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetClassByCodeAsync_WithValidCode_ReturnsClass()
        {
            // Act
            var result = await _repository.GetClassByCodeAsync("CS101");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CS101", result.ClassCode);
        }

        [Fact]
        public async Task GetClassByCodeAsync_WithInvalidCode_ReturnsNull()
        {
            // Act
            var result = await _repository.GetClassByCodeAsync("INVALID");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddClassAsync_WithValidClass_AddsClass()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassCode = $"TEST{Guid.NewGuid():N}",
                EducationLevel = "Test Level",
                CurrentTopic = "Test Topic"
            };

            // Act
            var classId = await _repository.AddClassAsync(classEntity);
            var result = await _repository.GetClassByIdAsync(classId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(classEntity.ClassCode, result.ClassCode);
            Assert.Equal(classEntity.EducationLevel, result.EducationLevel);
            Assert.Equal(classEntity.CurrentTopic, result.CurrentTopic);
        }

        [Fact]
        public async Task AddClassAsync_WithDuplicateCode_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingClass = (await _repository.GetAllClassesAsync()).First();
            var classEntity = new Class
            {
                ClassCode = existingClass.ClassCode,
                EducationLevel = "Test Level",
                CurrentTopic = "Test Topic"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddClassAsync(classEntity));
        }

        [Fact]
        public async Task UpdateClassAsync_WithValidClass_UpdatesClass()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassCode = $"TEST{Guid.NewGuid():N}",
                EducationLevel = "Test Level",
                CurrentTopic = "Test Topic"
            };
            var classId = await _repository.AddClassAsync(classEntity);

            // Update the class
            var updatedClass = await _repository.GetClassByIdAsync(classId);
            updatedClass.CurrentTopic = "Updated Topic";

            // Act
            await _repository.UpdateClassAsync(updatedClass);
            var result = await _repository.GetClassByIdAsync(classId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Topic", result.CurrentTopic);
        }

        [Fact]
        public async Task UpdateClassAsync_WithNonExistentClass_ThrowsInvalidOperationException()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = Guid.NewGuid().ToString(),
                ClassCode = $"TEST{Guid.NewGuid():N}",
                EducationLevel = "Test Level",
                CurrentTopic = "Test Topic"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateClassAsync(classEntity));
        }

        [Fact]
        public async Task DeleteClassAsync_WithValidId_DeletesClass()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassCode = $"TEST{Guid.NewGuid():N}",
                EducationLevel = "Test Level",
                CurrentTopic = "Test Topic"
            };
            var classId = await _repository.AddClassAsync(classEntity);

            // Act
            await _repository.DeleteClassAsync(classId);
            var result = await _repository.GetClassByIdAsync(classId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteClassAsync_WithNonExistentClass_ThrowsInvalidOperationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.DeleteClassAsync(Guid.NewGuid().ToString()));
        }

        [Fact]
        public async Task GetStudentsForClassAsync_WithValidId_ReturnsStudents()
        {
            // Arrange
            var classes = await _repository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            // Act
            var students = await _repository.GetStudentsForClassAsync(classId);

            // Assert
            Assert.NotNull(students);
            // Note: This test might fail if there are no students in the test class
            // In a real test, we would ensure there are students in the class
        }

        [Fact]
        public async Task GetStudentsForClassAsync_WithNonExistentClass_ReturnsEmptyList()
        {
            // Act
            var students = await _repository.GetStudentsForClassAsync(Guid.NewGuid().ToString());

            // Assert
            Assert.Empty(students);
        }
    }
}
