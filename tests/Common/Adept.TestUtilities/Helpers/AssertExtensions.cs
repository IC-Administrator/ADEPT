using Adept.Common.Json;
using Adept.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Adept.TestUtilities.Helpers
{
    /// <summary>
    /// Extension methods for assertions
    /// </summary>
    public static class AssertExtensions
    {
        /// <summary>
        /// Assert that a file exists
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        public static void FileExists(string filePath)
        {
            Assert.True(File.Exists(filePath), $"File does not exist: {filePath}");
        }

        /// <summary>
        /// Assert that a directory exists
        /// </summary>
        /// <param name="directoryPath">The path to the directory</param>
        public static void DirectoryExists(string directoryPath)
        {
            Assert.True(Directory.Exists(directoryPath), $"Directory does not exist: {directoryPath}");
        }

        /// <summary>
        /// Assert that a file contains the specified text
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="expectedText">The text that the file should contain</param>
        public static void FileContains(string filePath, string expectedText)
        {
            FileExists(filePath);
            string content = File.ReadAllText(filePath);
            Assert.Contains(expectedText, content);
        }

        /// <summary>
        /// Assert that a collection contains items matching the specified predicate
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="collection">The collection to check</param>
        /// <param name="predicate">The predicate to match items against</param>
        /// <param name="expectedCount">The expected number of matching items</param>
        public static void CollectionContains<T>(IEnumerable<T> collection, Func<T, bool> predicate, int expectedCount)
        {
            int actualCount = collection.Count(predicate);
            Assert.Equal(expectedCount, actualCount);
        }

        /// <summary>
        /// Assert that a string contains all the specified substrings
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <param name="substrings">The substrings that should be contained in the string</param>
        public static void StringContainsAll(string value, params string[] substrings)
        {
            foreach (string substring in substrings)
            {
                Assert.Contains(substring, value);
            }
        }

        /// <summary>
        /// Assert that a string does not contain any of the specified substrings
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <param name="substrings">The substrings that should not be contained in the string</param>
        public static void StringDoesNotContainAny(string value, params string[] substrings)
        {
            foreach (string substring in substrings)
            {
                Assert.DoesNotContain(substring, value);
            }
        }

        /// <summary>
        /// Assert that an exception of the specified type is thrown when the action is executed
        /// </summary>
        /// <typeparam name="TException">The type of exception expected</typeparam>
        /// <param name="action">The action that should throw the exception</param>
        /// <param name="messageContains">Optional substring that the exception message should contain</param>
        /// <returns>The thrown exception</returns>
        public static TException ThrowsWithMessage<TException>(Action action, string? messageContains = null)
            where TException : Exception
        {
            TException exception = Assert.Throws<TException>(action);

            if (!string.IsNullOrEmpty(messageContains))
            {
                Assert.Contains(messageContains, exception.Message);
            }

            return exception;
        }

        /// <summary>
        /// Assert that an exception of the specified type is thrown when the async action is executed
        /// </summary>
        /// <typeparam name="TException">The type of exception expected</typeparam>
        /// <param name="action">The async action that should throw the exception</param>
        /// <param name="messageContains">Optional substring that the exception message should contain</param>
        /// <returns>The thrown exception</returns>
        public static async Task<TException> ThrowsWithMessageAsync<TException>(Func<Task> action, string? messageContains = null)
            where TException : Exception
        {
            TException exception = await Assert.ThrowsAsync<TException>(action);

            if (!string.IsNullOrEmpty(messageContains))
            {
                Assert.Contains(messageContains, exception.Message);
            }

            return exception;
        }

        /// <summary>
        /// Assert that a string is valid JSON
        /// </summary>
        /// <param name="json">The JSON string to validate</param>
        public static void IsValidJson(string json)
        {
            Assert.True(json.IsValidJson(), $"The string is not valid JSON: {json}");
        }

        /// <summary>
        /// Assert that a string is valid JSON and can be deserialized to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to validate</param>
        public static void IsValidJson<T>(string json)
        {
            Assert.True(json.IsValidJson<T>(), $"The string is not valid JSON for type {typeof(T).Name}: {json}");
        }

        /// <summary>
        /// Assert that a JSON string contains the specified property with the specified value
        /// </summary>
        /// <param name="json">The JSON string to check</param>
        /// <param name="propertyName">The name of the property to check</param>
        /// <param name="expectedValue">The expected value of the property</param>
        public static void JsonPropertyEquals(string json, string propertyName, string expectedValue)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonElement root = document.RootElement;
                Assert.True(root.TryGetProperty(propertyName, out JsonElement property), $"Property '{propertyName}' not found in JSON: {json}");
                Assert.Equal(expectedValue, property.GetString());
            }
        }

        /// <summary>
        /// Assert that a JSON string contains the specified property
        /// </summary>
        /// <param name="json">The JSON string to check</param>
        /// <param name="propertyName">The name of the property to check</param>
        public static void JsonContainsProperty(string json, string propertyName)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonElement root = document.RootElement;
                Assert.True(root.TryGetProperty(propertyName, out _), $"Property '{propertyName}' not found in JSON: {json}");
            }
        }

        /// <summary>
        /// Assert that a JSON string contains all the specified properties
        /// </summary>
        /// <param name="json">The JSON string to check</param>
        /// <param name="propertyNames">The names of the properties to check</param>
        public static void JsonContainsProperties(string json, params string[] propertyNames)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonElement root = document.RootElement;
                foreach (string propertyName in propertyNames)
                {
                    Assert.True(root.TryGetProperty(propertyName, out _), $"Property '{propertyName}' not found in JSON: {json}");
                }
            }
        }

        /// <summary>
        /// Assert that a model has valid properties
        /// </summary>
        /// <typeparam name="T">The type of the model</typeparam>
        /// <param name="model">The model to validate</param>
        /// <param name="propertyValidations">The property validations to perform</param>
        public static void HasValidProperties<T>(T model, params (string PropertyName, Func<object, bool> Validation, string ErrorMessage)[] propertyValidations)
        {
            Assert.NotNull(model);

            foreach (var validation in propertyValidations)
            {
                var property = typeof(T).GetProperty(validation.PropertyName);
                Assert.NotNull(property);

                var value = property.GetValue(model);
                Assert.True(validation.Validation(value), $"{validation.ErrorMessage}: {value}");
            }
        }

        /// <summary>
        /// Assert that a lesson resource is valid
        /// </summary>
        /// <param name="resource">The resource to validate</param>
        public static void IsValidLessonResource(LessonResource resource)
        {
            Assert.NotNull(resource);
            Assert.NotEqual(Guid.Empty, resource.ResourceId);
            Assert.NotEqual(Guid.Empty, resource.LessonId);
            Assert.False(string.IsNullOrWhiteSpace(resource.Name));
            Assert.False(string.IsNullOrWhiteSpace(resource.Path));
            Assert.True(Enum.IsDefined(typeof(ResourceType), resource.Type));
            Assert.True(resource.CreatedAt > DateTime.MinValue);
            Assert.True(resource.UpdatedAt > DateTime.MinValue);
        }

        /// <summary>
        /// Assert that a lesson template is valid
        /// </summary>
        /// <param name="template">The template to validate</param>
        public static void IsValidLessonTemplate(LessonTemplate template)
        {
            Assert.NotNull(template);
            Assert.NotEqual(Guid.Empty, template.TemplateId);
            Assert.False(string.IsNullOrWhiteSpace(template.Name));
            Assert.False(string.IsNullOrWhiteSpace(template.Category));
            Assert.False(string.IsNullOrWhiteSpace(template.Title));
            Assert.False(string.IsNullOrWhiteSpace(template.ComponentsJson));
            Assert.True(template.ComponentsJson.IsValidJson());
            Assert.True(template.CreatedAt > DateTime.MinValue);
            Assert.True(template.UpdatedAt > DateTime.MinValue);
        }

        /// <summary>
        /// Assert that a system prompt is valid
        /// </summary>
        /// <param name="prompt">The prompt to validate</param>
        public static void IsValidSystemPrompt(SystemPrompt prompt)
        {
            Assert.NotNull(prompt);
            Assert.False(string.IsNullOrWhiteSpace(prompt.PromptId));
            Assert.False(string.IsNullOrWhiteSpace(prompt.Name));
            Assert.False(string.IsNullOrWhiteSpace(prompt.Content));
            Assert.True(prompt.Content.IsValidJson());
            Assert.True(prompt.CreatedAt > DateTime.MinValue);
            Assert.True(prompt.UpdatedAt > DateTime.MinValue);
        }
    }
}
