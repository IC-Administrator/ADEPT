using Adept.Core.Models;
using Adept.Data.Repositories;
using Adept.TestUtilities.Fixtures;
using Adept.TestUtilities.Helpers;
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
    /// Tests for the LessonResourceRepository class
    /// </summary>
    public class LessonResourceRepositoryTests : IClassFixture<RepositoryFixture>
    {
        private readonly RepositoryFixture _fixture;
        private readonly LessonResourceRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="LessonResourceRepositoryTests"/> class
        /// </summary>
        /// <param name="fixture">The repository fixture</param>
        public LessonResourceRepositoryTests(RepositoryFixture fixture)
        {
            _fixture = fixture;
            _repository = new LessonResourceRepository(
                _fixture.MockDatabaseContext.Object,
                _fixture.CreateMockLogger<LessonResourceRepository>().Object);
        }

        /// <summary>
        /// Tests that GetResourcesByLessonIdAsync returns resources when given a valid lesson ID
        /// </summary>
        [Fact]
        public async Task GetResourcesByLessonIdAsync_ValidLessonId_ReturnsResources()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            var expectedResources = TestDataGenerator.GenerateRandomLessonResources(3, lessonId);
            _fixture.SetupQueryAsync<LessonResource>("WHERE lesson_id = @LessonId", expectedResources);

            // Act
            var result = await _repository.GetResourcesByLessonIdAsync(lessonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResources.Count, result.Count());
            Assert.Equal(expectedResources.Select(r => r.ResourceId), result.Select(r => r.ResourceId));
            Assert.All(result, r => Assert.Equal(lessonId, r.LessonId));
        }

        /// <summary>
        /// Tests that GetResourcesByLessonIdAsync returns an empty list when given a lesson ID with no resources
        /// </summary>
        [Fact]
        public async Task GetResourcesByLessonIdAsync_LessonIdWithNoResources_ReturnsEmptyList()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            _fixture.SetupQueryAsync<LessonResource>("WHERE lesson_id = @LessonId", new List<LessonResource>());

            // Act
            var result = await _repository.GetResourcesByLessonIdAsync(lessonId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that GetResourcesByLessonIdAsync throws an exception when given an empty lesson ID
        /// </summary>
        [Fact]
        public async Task GetResourcesByLessonIdAsync_EmptyLessonId_ThrowsArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.GetResourcesByLessonIdAsync(emptyId),
                "Lesson ID cannot be empty");
        }

        /// <summary>
        /// Tests that GetResourceByIdAsync returns the resource when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetResourceByIdAsync_ValidId_ReturnsResource()
        {
            // Arrange
            var expectedResource = _fixture.LessonResources[0];
            _fixture.SetupQuerySingleOrDefaultAsync<LessonResource>("WHERE resource_id = @ResourceId", expectedResource);

            // Act
            var result = await _repository.GetResourceByIdAsync(expectedResource.ResourceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResource.ResourceId, result.ResourceId);
            Assert.Equal(expectedResource.LessonId, result.LessonId);
            Assert.Equal(expectedResource.Name, result.Name);
            Assert.Equal(expectedResource.Type, result.Type);
            Assert.Equal(expectedResource.Path, result.Path);
        }

        /// <summary>
        /// Tests that GetResourceByIdAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetResourceByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonResource>("WHERE resource_id = @ResourceId", null);

            // Act
            var result = await _repository.GetResourceByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that GetResourceByIdAsync throws an exception when given an empty ID
        /// </summary>
        [Fact]
        public async Task GetResourceByIdAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.GetResourceByIdAsync(emptyId),
                "Resource ID cannot be empty");
        }

        /// <summary>
        /// Tests that AddResourceAsync adds a resource and returns it
        /// </summary>
        [Fact]
        public async Task AddResourceAsync_ValidResource_AddsResourceAndReturnsIt()
        {
            // Arrange
            var resource = TestDataGenerator.GenerateRandomLessonResource();
            _fixture.SetupExecuteAsync("INSERT INTO LessonResources", 1);
            _fixture.SetupQuerySingleOrDefaultAsync<LessonResource>("WHERE resource_id = @ResourceId", resource);

            // Act
            var result = await _repository.AddResourceAsync(resource);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(resource.ResourceId, result.ResourceId);
            Assert.Equal(resource.LessonId, result.LessonId);
            Assert.Equal(resource.Name, result.Name);
            Assert.Equal(resource.Type, result.Type);
            Assert.Equal(resource.Path, result.Path);
        }

        /// <summary>
        /// Tests that AddResourceAsync throws an exception when given a null resource
        /// </summary>
        [Fact]
        public async Task AddResourceAsync_NullResource_ThrowsArgumentNullException()
        {
            // Act & Assert
            LessonResource? nullResource = null;
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentNullException>(
                () => _repository.AddResourceAsync(nullResource!),
                "Value cannot be null");
        }

        /// <summary>
        /// Tests that AddResourceAsync throws an exception when given a resource with an empty lesson ID
        /// </summary>
        [Fact]
        public async Task AddResourceAsync_EmptyLessonId_ThrowsArgumentException()
        {
            // Arrange
            var resource = TestDataGenerator.GenerateRandomLessonResource();
            resource.LessonId = Guid.Empty;

            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.AddResourceAsync(resource),
                "Lesson ID cannot be empty");
        }

        /// <summary>
        /// Tests that UpdateResourceAsync updates a resource and returns it
        /// </summary>
        [Fact]
        public async Task UpdateResourceAsync_ValidResource_UpdatesResourceAndReturnsIt()
        {
            // Arrange
            var resource = TestDataGenerator.GenerateRandomLessonResource();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonResource>("WHERE resource_id = @ResourceId", resource);
            _fixture.SetupExecuteAsync("UPDATE LessonResources", 1);

            // Act
            var result = await _repository.UpdateResourceAsync(resource);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(resource.ResourceId, result.ResourceId);
            Assert.Equal(resource.LessonId, result.LessonId);
            Assert.Equal(resource.Name, result.Name);
            Assert.Equal(resource.Type, result.Type);
            Assert.Equal(resource.Path, result.Path);
        }

        /// <summary>
        /// Tests that UpdateResourceAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task UpdateResourceAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var resource = TestDataGenerator.GenerateRandomLessonResource();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonResource>("WHERE resource_id = @ResourceId", null);

            // Act
            var result = await _repository.UpdateResourceAsync(resource);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that UpdateResourceAsync throws an exception when given a null resource
        /// </summary>
        [Fact]
        public async Task UpdateResourceAsync_NullResource_ThrowsArgumentNullException()
        {
            // Act & Assert
            LessonResource? nullResource = null;
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentNullException>(
                () => _repository.UpdateResourceAsync(nullResource!),
                "Value cannot be null");
        }

        /// <summary>
        /// Tests that DeleteResourceAsync returns true when given a valid ID
        /// </summary>
        [Fact]
        public async Task DeleteResourceAsync_ValidId_ReturnsTrue()
        {
            // Arrange
            var resourceId = Guid.NewGuid();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonResource>("WHERE resource_id = @ResourceId", new LessonResource { ResourceId = resourceId });
            _fixture.SetupExecuteAsync("DELETE FROM LessonResources", 1);

            // Act
            var result = await _repository.DeleteResourceAsync(resourceId);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that DeleteResourceAsync returns false when given an invalid ID
        /// </summary>
        [Fact]
        public async Task DeleteResourceAsync_InvalidId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonResource>("WHERE resource_id = @ResourceId", null);

            // Act
            var result = await _repository.DeleteResourceAsync(invalidId);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that DeleteResourceAsync throws an exception when given an empty ID
        /// </summary>
        [Fact]
        public async Task DeleteResourceAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.DeleteResourceAsync(emptyId),
                "Resource ID cannot be empty");
        }

        /// <summary>
        /// Tests that DeleteResourcesByLessonIdAsync returns true when given a valid lesson ID
        /// </summary>
        [Fact]
        public async Task DeleteResourcesByLessonIdAsync_ValidLessonId_ReturnsTrue()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            _fixture.SetupExecuteAsync("DELETE FROM LessonResources WHERE lesson_id = @LessonId", 3);

            // Act
            var result = await _repository.DeleteResourcesByLessonIdAsync(lessonId);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that DeleteResourcesByLessonIdAsync returns false when given a lesson ID with no resources
        /// </summary>
        [Fact]
        public async Task DeleteResourcesByLessonIdAsync_LessonIdWithNoResources_ReturnsFalse()
        {
            // Arrange
            var lessonId = Guid.NewGuid();
            _fixture.SetupExecuteAsync("DELETE FROM LessonResources WHERE lesson_id = @LessonId", 0);

            // Act
            var result = await _repository.DeleteResourcesByLessonIdAsync(lessonId);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that DeleteResourcesByLessonIdAsync throws an exception when given an empty lesson ID
        /// </summary>
        [Fact]
        public async Task DeleteResourcesByLessonIdAsync_EmptyLessonId_ThrowsArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.DeleteResourcesByLessonIdAsync(emptyId),
                "Lesson ID cannot be empty");
        }
    }
}
