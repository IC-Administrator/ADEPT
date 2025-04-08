using Adept.Core.Models;
using Adept.Core.Interfaces;
using Adept.Data.Validation;
using System;
using System.Collections.Generic;
using Xunit;

namespace Adept.Data.Tests.Validation
{
    public class EntityValidatorTests
    {
        [Fact]
        public void ValidateClass_ValidClass_ReturnsValidResult()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = Guid.NewGuid().ToString(),
                ClassCode = "CS101",
                EducationLevel = "Undergraduate",
                CurrentTopic = "Introduction to Programming"
            };

            // Act
            var result = EntityValidator.ValidateClass(classEntity);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateClass_NullClass_ReturnsInvalidResult()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = EntityValidator.ValidateClass(null);
#pragma warning restore CS8625

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Class entity cannot be null", result.Errors);
        }

        [Fact]
        public void ValidateClass_EmptyClassCode_ReturnsInvalidResult()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = Guid.NewGuid().ToString(),
                ClassCode = "",
                EducationLevel = "Undergraduate"
            };

            // Act
            var result = EntityValidator.ValidateClass(classEntity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Class code is required", result.Errors);
        }

        [Fact]
        public void ValidateClass_EmptyEducationLevel_ReturnsInvalidResult()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = Guid.NewGuid().ToString(),
                ClassCode = "CS101",
                EducationLevel = ""
            };

            // Act
            var result = EntityValidator.ValidateClass(classEntity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Education level is required", result.Errors);
        }

        [Fact]
        public void ValidateStudent_ValidStudent_ReturnsValidResult()
        {
            // Arrange
            var student = new Student
            {
                StudentId = Guid.NewGuid().ToString(),
                ClassId = Guid.NewGuid().ToString(),
                Name = "John Doe",
                FsmStatus = 1,
                SenStatus = 0,
                EalStatus = null
            };

            // Act
            var result = EntityValidator.ValidateStudent(student);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateStudent_NullStudent_ReturnsInvalidResult()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = EntityValidator.ValidateStudent(null);
#pragma warning restore CS8625

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Student entity cannot be null", result.Errors);
        }

        [Fact]
        public void ValidateStudent_EmptyName_ReturnsInvalidResult()
        {
            // Arrange
            var student = new Student
            {
                StudentId = Guid.NewGuid().ToString(),
                ClassId = Guid.NewGuid().ToString(),
                Name = ""
            };

            // Act
            var result = EntityValidator.ValidateStudent(student);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Student name is required", result.Errors);
        }

        [Fact]
        public void ValidateStudent_EmptyClassId_ReturnsInvalidResult()
        {
            // Arrange
            var student = new Student
            {
                StudentId = Guid.NewGuid().ToString(),
                ClassId = "",
                Name = "John Doe"
            };

            // Act
            var result = EntityValidator.ValidateStudent(student);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Class ID is required", result.Errors);
        }

        [Fact]
        public void ValidateStudent_InvalidFsmStatus_ReturnsInvalidResult()
        {
            // Arrange
            var student = new Student
            {
                StudentId = Guid.NewGuid().ToString(),
                ClassId = Guid.NewGuid().ToString(),
                Name = "John Doe",
                FsmStatus = 2 // Invalid value, should be 0, 1, or null
            };

            // Act
            var result = EntityValidator.ValidateStudent(student);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("FSM status must be 0, 1, or null", result.Errors);
        }

        [Fact]
        public void ValidateLessonPlan_ValidLessonPlan_ReturnsValidResult()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = Guid.NewGuid().ToString(),
                ClassId = Guid.NewGuid().ToString(),
                Title = "Introduction to Programming",
                Date = "2023-05-15",
                TimeSlot = 2,
                LearningObjectives = "Learn basic programming concepts"
            };

            // Act
            var result = EntityValidator.ValidateLessonPlan(lessonPlan);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateLessonPlan_NullLessonPlan_ReturnsInvalidResult()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = EntityValidator.ValidateLessonPlan(null);
#pragma warning restore CS8625

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Lesson plan entity cannot be null", result.Errors);
        }

        [Fact]
        public void ValidateLessonPlan_EmptyTitle_ReturnsInvalidResult()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = Guid.NewGuid().ToString(),
                ClassId = Guid.NewGuid().ToString(),
                Title = "",
                Date = "2023-05-15",
                TimeSlot = 2
            };

            // Act
            var result = EntityValidator.ValidateLessonPlan(lessonPlan);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Lesson title is required", result.Errors);
        }

        [Fact]
        public void ValidateLessonPlan_InvalidDate_ReturnsInvalidResult()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = Guid.NewGuid().ToString(),
                ClassId = Guid.NewGuid().ToString(),
                Title = "Introduction to Programming",
                Date = "15/05/2023", // Invalid format, should be YYYY-MM-DD
                TimeSlot = 2
            };

            // Act
            var result = EntityValidator.ValidateLessonPlan(lessonPlan);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Date must be in YYYY-MM-DD format", result.Errors);
        }

        [Fact]
        public void ValidateLessonPlan_InvalidTimeSlot_ReturnsInvalidResult()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = Guid.NewGuid().ToString(),
                ClassId = Guid.NewGuid().ToString(),
                Title = "Introduction to Programming",
                Date = "2023-05-15",
                TimeSlot = 5 // Invalid value, should be between 0 and 4
            };

            // Act
            var result = EntityValidator.ValidateLessonPlan(lessonPlan);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Time slot must be between 0 and 4", result.Errors);
        }

        [Fact]
        public void ValidationResult_Combine_CombinesTwoResults()
        {
            // Arrange
            var result1 = new ValidationResult();
            result1.AddError("Error 1");

            var result2 = new ValidationResult();
            result2.AddError("Error 2");

            // Act
            var combined = result1.Combine(result2);

            // Assert
            Assert.False(combined.IsValid);
            Assert.Equal(2, combined.Errors.Count);
            Assert.Contains("Error 1", combined.Errors);
            Assert.Contains("Error 2", combined.Errors);
        }

        [Fact]
        public void ValidationResult_ThrowIfInvalid_ThrowsExceptionWhenInvalid()
        {
            // Arrange
            var result = new ValidationResult();
            result.AddError("Validation error");

            // Act & Assert
            var exception = Assert.Throws<ValidationException>(() => result.ThrowIfInvalid());
            Assert.Contains("Validation error", exception.Message);
        }

        [Fact]
        public void ValidationResult_ThrowIfInvalid_DoesNotThrowWhenValid()
        {
            // Arrange
            var result = new ValidationResult();

            // Act & Assert
            var exception = Record.Exception(() => result.ThrowIfInvalid());
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateSystemPrompt_ValidSystemPrompt_ReturnsValidResult()
        {
            // Arrange
            var systemPrompt = new SystemPrompt
            {
                PromptId = Guid.NewGuid().ToString(),
                Name = "Default Assistant",
                Content = "You are a helpful AI assistant.",

                IsDefault = true
            };

            // Act
            var result = EntityValidator.ValidateSystemPrompt(systemPrompt);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateSystemPrompt_NullSystemPrompt_ReturnsInvalidResult()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = EntityValidator.ValidateSystemPrompt(null);
#pragma warning restore CS8625

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("System prompt entity cannot be null", result.Errors);
        }

        [Fact]
        public void ValidateSystemPrompt_EmptyName_ReturnsInvalidResult()
        {
            // Arrange
            var systemPrompt = new SystemPrompt
            {
                PromptId = Guid.NewGuid().ToString(),
                Name = "",
                Content = "You are a helpful AI assistant."
            };

            // Act
            var result = EntityValidator.ValidateSystemPrompt(systemPrompt);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Prompt name is required", result.Errors);
        }

        [Fact]
        public void ValidateSystemPrompt_EmptyContent_ReturnsInvalidResult()
        {
            // Arrange
            var systemPrompt = new SystemPrompt
            {
                PromptId = Guid.NewGuid().ToString(),
                Name = "Default Assistant",
                Content = ""
            };

            // Act
            var result = EntityValidator.ValidateSystemPrompt(systemPrompt);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Prompt content is required", result.Errors);
        }

        [Fact]
        public void ValidateSystemPrompt_MultipleErrors_ReturnsAllErrors()
        {
            // Arrange
            var systemPrompt = new SystemPrompt
            {
                PromptId = Guid.NewGuid().ToString(),
                Name = "",
                Content = ""
            };

            // Act
            var result = EntityValidator.ValidateSystemPrompt(systemPrompt);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
            Assert.Contains("Prompt name is required", result.Errors);
            Assert.Contains("Prompt content is required", result.Errors);
        }
    }
}
