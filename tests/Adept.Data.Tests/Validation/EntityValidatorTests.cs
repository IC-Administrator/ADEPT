using Adept.Core.Models;
using Adept.Data.Validation;
using System;
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
            var result = EntityValidator.ValidateClass(null);

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
            var result = EntityValidator.ValidateStudent(null);

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
            var result = EntityValidator.ValidateLessonPlan(null);

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
    }
}
