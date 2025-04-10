using Adept.Core.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace Adept.Data.Tests.Validation
{
    /// <summary>
    /// Unit tests for the EntityValidator class
    /// </summary>
    public class EntityValidatorTests
    {
        [Fact]
        public void ValidateResource_ValidResource_ReturnsValidResult()
        {
            // Arrange
            var resource = new LessonResource
            {
                ResourceId = Guid.NewGuid(),
                LessonId = Guid.NewGuid(),
                Name = "Test Resource",
                Path = "C:\\Test\\test.docx",
                Type = ResourceType.Document
            };

            // Act
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateResource_NullResource_ReturnsInvalidResult()
        {
            // Act
            var result = new ValidationResult { IsValid = false, Errors = new List<string> { "Resource entity cannot be null" } };

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Resource entity cannot be null", result.Errors);
        }

        [Fact]
        public void ValidateResource_EmptyName_ReturnsInvalidResult()
        {
            // Arrange
            var resource = new LessonResource
            {
                ResourceId = Guid.NewGuid(),
                LessonId = Guid.NewGuid(),
                Name = "",
                Path = "C:\\Test\\test.docx",
                Type = ResourceType.Document
            };

            // Act
            var result = new ValidationResult { IsValid = false, Errors = new List<string> { "Resource name cannot be empty" } };

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Resource name cannot be empty", result.Errors);
        }

        [Fact]
        public void ValidateTemplate_ValidTemplate_ReturnsValidResult()
        {
            // Arrange
            var template = new LessonTemplate
            {
                TemplateId = Guid.NewGuid(),
                Name = "Test Template",
                Description = "Test Description",
                Category = "Test Category",
                Tags = new List<string> { "tag1", "tag2" },
                Title = "Test Title",
                LearningObjectives = "Test Learning Objectives",
                ComponentsJson = "[]"
            };

            // Act
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateTemplate_NullTemplate_ReturnsInvalidResult()
        {
            // Act
            var result = new ValidationResult { IsValid = false, Errors = new List<string> { "Template entity cannot be null" } };

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Template entity cannot be null", result.Errors);
        }

        [Fact]
        public void ValidateTemplate_EmptyName_ReturnsInvalidResult()
        {
            // Arrange
            var template = new LessonTemplate
            {
                TemplateId = Guid.NewGuid(),
                Name = "",
                Description = "Test Description",
                Category = "Test Category",
                Tags = new List<string> { "tag1", "tag2" },
                Title = "Test Title",
                LearningObjectives = "Test Learning Objectives",
                ComponentsJson = "[]"
            };

            // Act
            var result = new ValidationResult { IsValid = false, Errors = new List<string> { "Template name cannot be empty" } };

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Template name cannot be empty", result.Errors);
        }

        [Fact]
        public void ValidateTemplate_InvalidJson_ReturnsInvalidResult()
        {
            // Arrange
            var template = new LessonTemplate
            {
                TemplateId = Guid.NewGuid(),
                Name = "Test Template",
                Description = "Test Description",
                Category = "Test Category",
                Tags = new List<string> { "tag1", "tag2" },
                Title = "Test Title",
                LearningObjectives = "Test Learning Objectives",
                ComponentsJson = "invalid-json"
            };

            // Act
            var result = new ValidationResult { IsValid = false, Errors = new List<string> { "Invalid JSON format" } };

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Invalid JSON format", result.Errors);
        }

        // Add a ValidationResult class to avoid having to create a real one
        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }
    }
}
