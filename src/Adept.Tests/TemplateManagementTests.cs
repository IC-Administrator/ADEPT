using Adept.Core.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace Adept.Tests
{
    public class TemplateManagementTests
    {
        [Fact]
        public void TemplateModel_Properties_ShouldBeCorrect()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            var name = "Test Template";
            var description = "Test Description";
            var category = "Test Category";
            var tags = new List<string> { "tag1", "tag2" };
            var title = "Test Title";
            var learningObjectives = "Test Learning Objectives";
            var componentsJson = "[]";
            
            // Act
            var template = new LessonTemplate
            {
                TemplateId = templateId,
                Name = name,
                Description = description,
                Category = category,
                Tags = tags,
                Title = title,
                LearningObjectives = learningObjectives,
                ComponentsJson = componentsJson
            };
            
            // Assert
            Assert.Equal(templateId, template.TemplateId);
            Assert.Equal(name, template.Name);
            Assert.Equal(description, template.Description);
            Assert.Equal(category, template.Category);
            Assert.Equal(tags, template.Tags);
            Assert.Equal(title, template.Title);
            Assert.Equal(learningObjectives, template.LearningObjectives);
            Assert.Equal(componentsJson, template.ComponentsJson);
        }
        
        [Fact]
        public void TemplateModel_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var template = new LessonTemplate();
            
            // Assert
            Assert.NotEqual(Guid.Empty, template.TemplateId);
            Assert.NotNull(template.Tags);
            Assert.Empty(template.Tags);
            Assert.True(template.CreatedAt > DateTime.MinValue);
            Assert.True(template.UpdatedAt > DateTime.MinValue);
        }
    }
}
