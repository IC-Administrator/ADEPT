using Adept.Core.Interfaces;
using Adept.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adept.Data.Validation
{
    /// <summary>
    /// Provides validation methods for entity objects
    /// </summary>
    public static class EntityValidator
    {
        /// <summary>
        /// Validates a class entity
        /// </summary>
        /// <param name="classEntity">The class to validate</param>
        /// <returns>Validation results</returns>
        public static ValidationResult ValidateClass(Class classEntity)
        {
            var result = new ValidationResult();

            if (classEntity == null)
            {
                result.AddError("Class entity cannot be null");
                return result;
            }

            // Validate class code
            if (string.IsNullOrWhiteSpace(classEntity.ClassCode))
            {
                result.AddError("Class code is required");
            }
            else if (classEntity.ClassCode.Length > 50)
            {
                result.AddError("Class code cannot exceed 50 characters");
            }

            // Validate education level
            if (string.IsNullOrWhiteSpace(classEntity.EducationLevel))
            {
                result.AddError("Education level is required");
            }

            return result;
        }

        /// <summary>
        /// Validates a student entity
        /// </summary>
        /// <param name="student">The student to validate</param>
        /// <returns>Validation results</returns>
        public static ValidationResult ValidateStudent(Student student)
        {
            var result = new ValidationResult();

            if (student == null)
            {
                result.AddError("Student entity cannot be null");
                return result;
            }

            // Validate student name
            if (string.IsNullOrWhiteSpace(student.Name))
            {
                result.AddError("Student name is required");
            }
            else if (student.Name.Length > 100)
            {
                result.AddError("Student name cannot exceed 100 characters");
            }

            // Validate class ID
            if (string.IsNullOrWhiteSpace(student.ClassId))
            {
                result.AddError("Class ID is required");
            }

            // Validate status fields (should be 0, 1, or null)
            if (student.FsmStatus.HasValue && student.FsmStatus != 0 && student.FsmStatus != 1)
            {
                result.AddError("FSM status must be 0, 1, or null");
            }

            if (student.SenStatus.HasValue && student.SenStatus != 0 && student.SenStatus != 1)
            {
                result.AddError("SEN status must be 0, 1, or null");
            }

            if (student.EalStatus.HasValue && student.EalStatus != 0 && student.EalStatus != 1)
            {
                result.AddError("EAL status must be 0, 1, or null");
            }

            return result;
        }

        /// <summary>
        /// Validates a lesson plan entity
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to validate</param>
        /// <returns>Validation results</returns>
        public static ValidationResult ValidateLessonPlan(LessonPlan lessonPlan)
        {
            var result = new ValidationResult();

            if (lessonPlan == null)
            {
                result.AddError("Lesson plan entity cannot be null");
                return result;
            }

            // Validate title
            if (string.IsNullOrWhiteSpace(lessonPlan.Title))
            {
                result.AddError("Lesson title is required");
            }

            // Validate class ID
            if (string.IsNullOrWhiteSpace(lessonPlan.ClassId))
            {
                result.AddError("Class ID is required");
            }

            // Validate date
            if (string.IsNullOrWhiteSpace(lessonPlan.Date))
            {
                result.AddError("Date is required");
            }
            else if (!Regex.IsMatch(lessonPlan.Date, @"^\d{4}-\d{2}-\d{2}$"))
            {
                result.AddError("Date must be in YYYY-MM-DD format");
            }

            // Validate time slot
            if (lessonPlan.TimeSlot < 0 || lessonPlan.TimeSlot > 4)
            {
                result.AddError("Time slot must be between 0 and 4");
            }

            return result;
        }

        /// <summary>
        /// Validates a conversation entity
        /// </summary>
        /// <param name="conversation">The conversation to validate</param>
        /// <returns>Validation results</returns>
        public static ValidationResult ValidateConversation(Conversation conversation)
        {
            var result = new ValidationResult();

            if (conversation == null)
            {
                result.AddError("Conversation entity cannot be null");
                return result;
            }

            // Validate date
            if (string.IsNullOrWhiteSpace(conversation.Date))
            {
                result.AddError("Date is required");
            }
            else if (!Regex.IsMatch(conversation.Date, @"^\d{4}-\d{2}-\d{2}$"))
            {
                result.AddError("Date must be in YYYY-MM-DD format");
            }

            // Validate time slot if provided
            if (conversation.TimeSlot.HasValue && (conversation.TimeSlot < 0 || conversation.TimeSlot > 4))
            {
                result.AddError("Time slot must be between 0 and 4");
            }

            return result;
        }

        /// <summary>
        /// Validates a system prompt entity
        /// </summary>
        /// <param name="systemPrompt">The system prompt to validate</param>
        /// <returns>Validation results</returns>
        public static ValidationResult ValidateSystemPrompt(SystemPrompt systemPrompt)
        {
            var result = new ValidationResult();

            if (systemPrompt == null)
            {
                result.AddError("System prompt entity cannot be null");
                return result;
            }

            // Validate name
            if (string.IsNullOrWhiteSpace(systemPrompt.Name))
            {
                result.AddError("Prompt name is required");
            }

            // Validate content
            if (string.IsNullOrWhiteSpace(systemPrompt.Content))
            {
                result.AddError("Prompt content is required");
            }

            return result;
        }
    }

    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        private readonly List<string> _errors = new List<string>();

        /// <summary>
        /// Gets a value indicating whether the validation passed
        /// </summary>
        public bool IsValid => !_errors.Any();

        /// <summary>
        /// Gets the validation errors
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Adds an error to the validation result
        /// </summary>
        /// <param name="error">The error message</param>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                _errors.Add(error);
            }
        }

        /// <summary>
        /// Adds multiple errors to the validation result
        /// </summary>
        /// <param name="errors">The error messages</param>
        public void AddErrors(IEnumerable<string> errors)
        {
            if (errors != null)
            {
                foreach (var error in errors.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    _errors.Add(error);
                }
            }
        }

        /// <summary>
        /// Combines this validation result with another
        /// </summary>
        /// <param name="other">The other validation result</param>
        /// <returns>This validation result</returns>
        public ValidationResult Combine(ValidationResult other)
        {
            if (other != null)
            {
                AddErrors(other.Errors);
            }
            return this;
        }

        /// <summary>
        /// Throws an exception if the validation failed
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation failed</exception>
        public void ThrowIfInvalid()
        {
            if (!IsValid)
            {
                throw new ValidationException(string.Join(Environment.NewLine, Errors));
            }
        }
    }

    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public ValidationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
