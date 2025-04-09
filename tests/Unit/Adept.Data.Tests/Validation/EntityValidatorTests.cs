using Adept.Core.Models;
using Adept.Data.Validation;
using System;
using Xunit;

namespace Adept.Data.Tests.Validation
{
    /// <summary>
    /// Unit tests for the EntityValidator class
    /// </summary>
    public class EntityValidatorTests
    {
        [Fact]
        public void ValidateClass_ValidClass_ReturnsValidResult()
        {
            // Arrange
            var classEntity = new Class
            {
                ClassId = TestConstants.EntityIds.ClassId,
                ClassCode = TestConstants.EntityData.ClassCode,
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
                ClassId = TestConstants.EntityIds.ClassId,
                ClassCode = "",
                EducationLevel = "Undergraduate",
                CurrentTopic = "Introduction to Programming"
            };

            // Act
            var result = EntityValidator.ValidateClass(classEntity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("ClassCode is required", result.Errors);
        }

        [Fact]
        public void ValidateStudent_ValidStudent_ReturnsValidResult()
        {
            // Arrange
            var student = new Student
            {
                StudentId = TestConstants.EntityIds.StudentId,
                Name = TestConstants.EntityData.StudentName,
                Email = TestConstants.EntityData.StudentEmail,
                EnrollmentDate = DateTime.Now.AddDays(-30)
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
                StudentId = TestConstants.EntityIds.StudentId,
                Name = "",
                Email = TestConstants.EntityData.StudentEmail,
                EnrollmentDate = DateTime.Now.AddDays(-30)
            };

            // Act
            var result = EntityValidator.ValidateStudent(student);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Name is required", result.Errors);
        }

        [Fact]
        public void ValidateStudent_InvalidEmail_ReturnsInvalidResult()
        {
            // Arrange
            var student = new Student
            {
                StudentId = TestConstants.EntityIds.StudentId,
                Name = TestConstants.EntityData.StudentName,
                Email = "invalid-email",
                EnrollmentDate = DateTime.Now.AddDays(-30)
            };

            // Act
            var result = EntityValidator.ValidateStudent(student);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Email is not valid", result.Errors);
        }

        [Fact]
        public void ValidateLesson_ValidLesson_ReturnsValidResult()
        {
            // Arrange
            var lesson = new Lesson
            {
                LessonId = TestConstants.EntityIds.LessonId,
                Title = TestConstants.EntityData.LessonTitle,
                ClassId = TestConstants.EntityIds.ClassId,
                Content = "Lesson content goes here",
                CreatedDate = DateTime.Now.AddDays(-5)
            };

            // Act
            var result = EntityValidator.ValidateLesson(lesson);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateLesson_NullLesson_ReturnsInvalidResult()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = EntityValidator.ValidateLesson(null);
#pragma warning restore CS8625

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Lesson entity cannot be null", result.Errors);
        }

        [Fact]
        public void ValidateLesson_EmptyTitle_ReturnsInvalidResult()
        {
            // Arrange
            var lesson = new Lesson
            {
                LessonId = TestConstants.EntityIds.LessonId,
                Title = "",
                ClassId = TestConstants.EntityIds.ClassId,
                Content = "Lesson content goes here",
                CreatedDate = DateTime.Now.AddDays(-5)
            };

            // Act
            var result = EntityValidator.ValidateLesson(lesson);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Title is required", result.Errors);
        }

        [Fact]
        public void ValidateAssignment_ValidAssignment_ReturnsValidResult()
        {
            // Arrange
            var assignment = new Assignment
            {
                AssignmentId = TestConstants.EntityIds.AssignmentId,
                Title = TestConstants.EntityData.AssignmentTitle,
                LessonId = TestConstants.EntityIds.LessonId,
                Description = "Assignment description",
                DueDate = DateTime.Now.AddDays(7)
            };

            // Act
            var result = EntityValidator.ValidateAssignment(assignment);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateAssignment_NullAssignment_ReturnsInvalidResult()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = EntityValidator.ValidateAssignment(null);
#pragma warning restore CS8625

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Assignment entity cannot be null", result.Errors);
        }

        [Fact]
        public void ValidateAssignment_EmptyTitle_ReturnsInvalidResult()
        {
            // Arrange
            var assignment = new Assignment
            {
                AssignmentId = TestConstants.EntityIds.AssignmentId,
                Title = "",
                LessonId = TestConstants.EntityIds.LessonId,
                Description = "Assignment description",
                DueDate = DateTime.Now.AddDays(7)
            };

            // Act
            var result = EntityValidator.ValidateAssignment(assignment);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Title is required", result.Errors);
        }

        [Fact]
        public void ValidateConversation_ValidConversation_ReturnsValidResult()
        {
            // Arrange
            var conversation = new Conversation
            {
                ConversationId = TestConstants.EntityIds.ConversationId,
                Title = "Test Conversation",
                SystemPrompt = TestConstants.EntityData.SystemPrompt,
                CreatedDate = DateTime.Now
            };

            // Act
            var result = EntityValidator.ValidateConversation(conversation);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateConversation_NullConversation_ReturnsInvalidResult()
        {
            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = EntityValidator.ValidateConversation(null);
#pragma warning restore CS8625

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Conversation entity cannot be null", result.Errors);
        }
    }
}
