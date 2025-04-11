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
    /// Tests for the LessonTemplateRepository class
    /// </summary>
    public class LessonTemplateRepositoryTests : IClassFixture<RepositoryFixture>
    {
        private readonly RepositoryFixture _fixture;
        private readonly LessonTemplateRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="LessonTemplateRepositoryTests"/> class
        /// </summary>
        /// <param name="fixture">The repository fixture</param>
        public LessonTemplateRepositoryTests(RepositoryFixture fixture)
        {
            _fixture = fixture;
            _repository = new LessonTemplateRepository(
                _fixture.MockDatabaseContext.Object,
                _fixture.CreateMockLogger<LessonTemplateRepository>().Object);
        }

        /// <summary>
        /// Tests that GetAllTemplatesAsync returns all templates
        /// </summary>
        [Fact]
        public async Task GetAllTemplatesAsync_ReturnsAllTemplates()
        {
            // Arrange
            var expectedTemplates = _fixture.LessonTemplates;
            _fixture.SetupQueryAsync<LessonTemplate>("SELECT", expectedTemplates);

            // Act
            var result = await _repository.GetAllTemplatesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTemplates.Count, result.Count());
            Assert.Equal(expectedTemplates.Select(t => t.TemplateId), result.Select(t => t.TemplateId));
        }

        /// <summary>
        /// Tests that GetTemplateByIdAsync returns the template when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetTemplateByIdAsync_ValidId_ReturnsTemplate()
        {
            // Arrange
            var expectedTemplate = _fixture.LessonTemplates[0];
            _fixture.SetupQuerySingleOrDefaultAsync<LessonTemplate>("WHERE template_id = @TemplateId", expectedTemplate);

            // Act
            var result = await _repository.GetTemplateByIdAsync(expectedTemplate.TemplateId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTemplate.TemplateId, result.TemplateId);
            Assert.Equal(expectedTemplate.Name, result.Name);
            Assert.Equal(expectedTemplate.Description, result.Description);
            Assert.Equal(expectedTemplate.Category, result.Category);
            Assert.Equal(expectedTemplate.Tags, result.Tags);
            Assert.Equal(expectedTemplate.Title, result.Title);
            Assert.Equal(expectedTemplate.LearningObjectives, result.LearningObjectives);
            Assert.Equal(expectedTemplate.ComponentsJson, result.ComponentsJson);
        }

        /// <summary>
        /// Tests that GetTemplateByIdAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetTemplateByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonTemplate>("WHERE template_id = @TemplateId", null);

            // Act
            var result = await _repository.GetTemplateByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that GetTemplateByIdAsync throws an exception when given an empty ID
        /// </summary>
        [Fact]
        public async Task GetTemplateByIdAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.GetTemplateByIdAsync(emptyId),
                "Template ID cannot be empty");
        }

        /// <summary>
        /// Tests that GetTemplatesByCategoryAsync returns templates when given a valid category
        /// </summary>
        [Fact]
        public async Task GetTemplatesByCategoryAsync_ValidCategory_ReturnsTemplates()
        {
            // Arrange
            var category = "Math";
            var expectedTemplates = _fixture.LessonTemplates.Where(t => t.Category == category).ToList();
            _fixture.SetupQueryAsync<LessonTemplate>("WHERE category = @Category", expectedTemplates);

            // Act
            var result = await _repository.GetTemplatesByCategoryAsync(category);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTemplates.Count, result.Count());
            Assert.Equal(expectedTemplates.Select(t => t.TemplateId), result.Select(t => t.TemplateId));
        }

        /// <summary>
        /// Tests that GetTemplatesByCategoryAsync returns an empty list when given an invalid category
        /// </summary>
        [Fact]
        public async Task GetTemplatesByCategoryAsync_InvalidCategory_ReturnsEmptyList()
        {
            // Arrange
            var invalidCategory = "InvalidCategory";
            _fixture.SetupQueryAsync<LessonTemplate>("WHERE category = @Category", new List<LessonTemplate>());

            // Act
            var result = await _repository.GetTemplatesByCategoryAsync(invalidCategory);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that GetTemplatesByCategoryAsync throws an exception when given a null or empty category
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetTemplatesByCategoryAsync_NullOrEmptyCategory_ThrowsArgumentException(string category)
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.GetTemplatesByCategoryAsync(category),
                "Value cannot be null or empty");
        }

        /// <summary>
        /// Tests that GetTemplatesByTagAsync returns templates when given a valid tag
        /// </summary>
        [Fact]
        public async Task GetTemplatesByTagAsync_ValidTag_ReturnsTemplates()
        {
            // Arrange
            var tag = "algebra";
            var expectedTemplates = _fixture.LessonTemplates.Where(t => t.Tags.Contains(tag)).ToList();
            _fixture.SetupQueryAsync<LessonTemplate>("WHERE tags LIKE @Tag", expectedTemplates);

            // Act
            var result = await _repository.GetTemplatesByTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTemplates.Count, result.Count());
            Assert.Equal(expectedTemplates.Select(t => t.TemplateId), result.Select(t => t.TemplateId));
        }

        /// <summary>
        /// Tests that GetTemplatesByTagAsync returns an empty list when given an invalid tag
        /// </summary>
        [Fact]
        public async Task GetTemplatesByTagAsync_InvalidTag_ReturnsEmptyList()
        {
            // Arrange
            var invalidTag = "InvalidTag";
            _fixture.SetupQueryAsync<LessonTemplate>("WHERE tags LIKE @Tag", new List<LessonTemplate>());

            // Act
            var result = await _repository.GetTemplatesByTagAsync(invalidTag);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that GetTemplatesByTagAsync throws an exception when given a null or empty tag
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetTemplatesByTagAsync_NullOrEmptyTag_ThrowsArgumentException(string tag)
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.GetTemplatesByTagAsync(tag),
                "Value cannot be null or empty");
        }

        /// <summary>
        /// Tests that AddTemplateAsync adds a template and returns it
        /// </summary>
        [Fact]
        public async Task AddTemplateAsync_ValidTemplate_AddsTemplateAndReturnsIt()
        {
            // Arrange
            var template = TestDataGenerator.GenerateRandomLessonTemplate();
            _fixture.SetupExecuteAsync("INSERT INTO LessonTemplates", 1);
            _fixture.SetupQuerySingleOrDefaultAsync<LessonTemplate>("WHERE template_id = @TemplateId", template);

            // Act
            var result = await _repository.AddTemplateAsync(template);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(template.TemplateId, result.TemplateId);
            Assert.Equal(template.Name, result.Name);
            Assert.Equal(template.Description, result.Description);
            Assert.Equal(template.Category, result.Category);
            Assert.Equal(template.Tags, result.Tags);
            Assert.Equal(template.Title, result.Title);
            Assert.Equal(template.LearningObjectives, result.LearningObjectives);
            Assert.Equal(template.ComponentsJson, result.ComponentsJson);
        }

        /// <summary>
        /// Tests that AddTemplateAsync throws an exception when given a null template
        /// </summary>
        [Fact]
        public async Task AddTemplateAsync_NullTemplate_ThrowsArgumentNullException()
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentNullException>(
                () => _repository.AddTemplateAsync(null),
                "Value cannot be null");
        }

        /// <summary>
        /// Tests that UpdateTemplateAsync updates a template and returns it
        /// </summary>
        [Fact]
        public async Task UpdateTemplateAsync_ValidTemplate_UpdatesTemplateAndReturnsIt()
        {
            // Arrange
            var template = TestDataGenerator.GenerateRandomLessonTemplate();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonTemplate>("WHERE template_id = @TemplateId", template);
            _fixture.SetupExecuteAsync("UPDATE LessonTemplates", 1);

            // Act
            var result = await _repository.UpdateTemplateAsync(template);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(template.TemplateId, result.TemplateId);
            Assert.Equal(template.Name, result.Name);
            Assert.Equal(template.Description, result.Description);
            Assert.Equal(template.Category, result.Category);
            Assert.Equal(template.Tags, result.Tags);
            Assert.Equal(template.Title, result.Title);
            Assert.Equal(template.LearningObjectives, result.LearningObjectives);
            Assert.Equal(template.ComponentsJson, result.ComponentsJson);
        }

        /// <summary>
        /// Tests that UpdateTemplateAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task UpdateTemplateAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var template = TestDataGenerator.GenerateRandomLessonTemplate();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonTemplate>("WHERE template_id = @TemplateId", null);

            // Act
            var result = await _repository.UpdateTemplateAsync(template);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that UpdateTemplateAsync throws an exception when given a null template
        /// </summary>
        [Fact]
        public async Task UpdateTemplateAsync_NullTemplate_ThrowsArgumentNullException()
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentNullException>(
                () => _repository.UpdateTemplateAsync(null),
                "Value cannot be null");
        }

        /// <summary>
        /// Tests that DeleteTemplateAsync returns true when given a valid ID
        /// </summary>
        [Fact]
        public async Task DeleteTemplateAsync_ValidId_ReturnsTrue()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonTemplate>("WHERE template_id = @TemplateId", new LessonTemplate { TemplateId = templateId });
            _fixture.SetupExecuteAsync("DELETE FROM LessonTemplates", 1);

            // Act
            var result = await _repository.DeleteTemplateAsync(templateId);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that DeleteTemplateAsync returns false when given an invalid ID
        /// </summary>
        [Fact]
        public async Task DeleteTemplateAsync_InvalidId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _fixture.SetupQuerySingleOrDefaultAsync<LessonTemplate>("WHERE template_id = @TemplateId", null);

            // Act
            var result = await _repository.DeleteTemplateAsync(invalidId);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that DeleteTemplateAsync throws an exception when given an empty ID
        /// </summary>
        [Fact]
        public async Task DeleteTemplateAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.DeleteTemplateAsync(emptyId),
                "Template ID cannot be empty");
        }
    }
}
