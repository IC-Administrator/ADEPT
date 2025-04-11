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
    /// Tests for the SystemPromptRepository class
    /// </summary>
    public class SystemPromptRepositoryTests : IClassFixture<RepositoryFixture>
    {
        private readonly RepositoryFixture _fixture;
        private readonly SystemPromptRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemPromptRepositoryTests"/> class
        /// </summary>
        /// <param name="fixture">The repository fixture</param>
        public SystemPromptRepositoryTests(RepositoryFixture fixture)
        {
            _fixture = fixture;
            _repository = new SystemPromptRepository(
                _fixture.MockDatabaseContext.Object,
                _fixture.CreateMockLogger<SystemPromptRepository>().Object);
        }

        /// <summary>
        /// Tests that GetAllPromptsAsync returns all prompts
        /// </summary>
        [Fact]
        public async Task GetAllPromptsAsync_ReturnsAllPrompts()
        {
            // Arrange
            var expectedPrompts = _fixture.SystemPrompts;
            _fixture.SetupQueryAsync<SystemPrompt>("SELECT", expectedPrompts);

            // Act
            var result = await _repository.GetAllPromptsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPrompts.Count, result.Count());
            Assert.Equal(expectedPrompts.Select(p => p.PromptId), result.Select(p => p.PromptId));
        }

        /// <summary>
        /// Tests that GetPromptByIdAsync returns the prompt when given a valid ID
        /// </summary>
        [Fact]
        public async Task GetPromptByIdAsync_ValidId_ReturnsPrompt()
        {
            // Arrange
            var expectedPrompt = _fixture.SystemPrompts[0];
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", expectedPrompt);

            // Act
            var result = await _repository.GetPromptByIdAsync(expectedPrompt.PromptId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPrompt.PromptId, result.PromptId);
            Assert.Equal(expectedPrompt.Name, result.Name);
            Assert.Equal(expectedPrompt.Content, result.Content);
            Assert.Equal(expectedPrompt.IsDefault, result.IsDefault);
        }

        /// <summary>
        /// Tests that GetPromptByIdAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task GetPromptByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", null);

            // Act
            var result = await _repository.GetPromptByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that GetPromptByIdAsync throws an exception when given a null or empty ID
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetPromptByIdAsync_NullOrEmptyId_ThrowsArgumentException(string promptId)
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.GetPromptByIdAsync(promptId),
                "Value cannot be null or empty");
        }

        /// <summary>
        /// Tests that GetDefaultPromptAsync returns the default prompt
        /// </summary>
        [Fact]
        public async Task GetDefaultPromptAsync_ReturnsDefaultPrompt()
        {
            // Arrange
            var expectedPrompt = _fixture.SystemPrompts.First(p => p.IsDefault);
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE is_default = 1", expectedPrompt);

            // Act
            var result = await _repository.GetDefaultPromptAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPrompt.PromptId, result.PromptId);
            Assert.Equal(expectedPrompt.Name, result.Name);
            Assert.Equal(expectedPrompt.Content, result.Content);
            Assert.True(result.IsDefault);
        }

        /// <summary>
        /// Tests that GetDefaultPromptAsync returns null when there is no default prompt
        /// </summary>
        [Fact]
        public async Task GetDefaultPromptAsync_NoDefaultPrompt_ReturnsNull()
        {
            // Arrange
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE is_default = 1", null);

            // Act
            var result = await _repository.GetDefaultPromptAsync();

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that AddPromptAsync adds a prompt and returns it
        /// </summary>
        [Fact]
        public async Task AddPromptAsync_ValidPrompt_AddsPromptAndReturnsIt()
        {
            // Arrange
            var prompt = TestDataGenerator.GenerateRandomSystemPrompt();
            _fixture.SetupExecuteAsync("INSERT INTO SystemPrompts", 1);
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", prompt);

            // Act
            var result = await _repository.AddPromptAsync(prompt);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(prompt.PromptId, result.PromptId);
            Assert.Equal(prompt.Name, result.Name);
            Assert.Equal(prompt.Content, result.Content);
            Assert.Equal(prompt.IsDefault, result.IsDefault);
        }

        /// <summary>
        /// Tests that AddPromptAsync throws an exception when given a null prompt
        /// </summary>
        [Fact]
        public async Task AddPromptAsync_NullPrompt_ThrowsArgumentNullException()
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentNullException>(
                () => _repository.AddPromptAsync(null),
                "Value cannot be null");
        }

        /// <summary>
        /// Tests that UpdatePromptAsync updates a prompt and returns it
        /// </summary>
        [Fact]
        public async Task UpdatePromptAsync_ValidPrompt_UpdatesPromptAndReturnsIt()
        {
            // Arrange
            var prompt = TestDataGenerator.GenerateRandomSystemPrompt();
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", prompt);
            _fixture.SetupExecuteAsync("UPDATE SystemPrompts", 1);

            // Act
            var result = await _repository.UpdatePromptAsync(prompt);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(prompt.PromptId, result.PromptId);
            Assert.Equal(prompt.Name, result.Name);
            Assert.Equal(prompt.Content, result.Content);
            Assert.Equal(prompt.IsDefault, result.IsDefault);
        }

        /// <summary>
        /// Tests that UpdatePromptAsync returns null when given an invalid ID
        /// </summary>
        [Fact]
        public async Task UpdatePromptAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var prompt = TestDataGenerator.GenerateRandomSystemPrompt();
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", null);

            // Act
            var result = await _repository.UpdatePromptAsync(prompt);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that UpdatePromptAsync throws an exception when given a null prompt
        /// </summary>
        [Fact]
        public async Task UpdatePromptAsync_NullPrompt_ThrowsArgumentNullException()
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentNullException>(
                () => _repository.UpdatePromptAsync(null),
                "Value cannot be null");
        }

        /// <summary>
        /// Tests that DeletePromptAsync returns true when given a valid ID
        /// </summary>
        [Fact]
        public async Task DeletePromptAsync_ValidId_ReturnsTrue()
        {
            // Arrange
            var promptId = Guid.NewGuid().ToString();
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", new SystemPrompt { PromptId = promptId });
            _fixture.SetupExecuteAsync("DELETE FROM SystemPrompts", 1);

            // Act
            var result = await _repository.DeletePromptAsync(promptId);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that DeletePromptAsync returns false when given an invalid ID
        /// </summary>
        [Fact]
        public async Task DeletePromptAsync_InvalidId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", null);

            // Act
            var result = await _repository.DeletePromptAsync(invalidId);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that DeletePromptAsync throws an exception when given a null or empty ID
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeletePromptAsync_NullOrEmptyId_ThrowsArgumentException(string promptId)
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.DeletePromptAsync(promptId),
                "Value cannot be null or empty");
        }

        /// <summary>
        /// Tests that SetDefaultPromptAsync sets the default prompt and returns true
        /// </summary>
        [Fact]
        public async Task SetDefaultPromptAsync_ValidId_SetsDefaultPromptAndReturnsTrue()
        {
            // Arrange
            var promptId = Guid.NewGuid().ToString();
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", new SystemPrompt { PromptId = promptId });
            _fixture.SetupExecuteAsync("UPDATE SystemPrompts SET is_default = 0", 1);
            _fixture.SetupExecuteAsync("UPDATE SystemPrompts SET is_default = 1", 1);

            // Act
            var result = await _repository.SetDefaultPromptAsync(promptId);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that SetDefaultPromptAsync returns false when given an invalid ID
        /// </summary>
        [Fact]
        public async Task SetDefaultPromptAsync_InvalidId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();
            _fixture.SetupQuerySingleOrDefaultAsync<SystemPrompt>("WHERE prompt_id = @PromptId", null);

            // Act
            var result = await _repository.SetDefaultPromptAsync(invalidId);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that SetDefaultPromptAsync throws an exception when given a null or empty ID
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SetDefaultPromptAsync_NullOrEmptyId_ThrowsArgumentException(string promptId)
        {
            // Act & Assert
            await AssertExtensions.ThrowsWithMessageAsync<ArgumentException>(
                () => _repository.SetDefaultPromptAsync(promptId),
                "Value cannot be null or empty");
        }
    }
}
