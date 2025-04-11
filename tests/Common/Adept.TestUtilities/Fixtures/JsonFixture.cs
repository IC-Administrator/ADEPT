using Adept.Common.Json;
using Adept.Core.Models;
using Adept.TestUtilities.Helpers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Adept.TestUtilities.Fixtures
{
    /// <summary>
    /// A fixture for JSON tests that provides test data and serialization options
    /// </summary>
    public class JsonFixture : IDisposable
    {
        // JSON serialization options
        public JsonSerializerOptions DefaultOptions { get; }
        public JsonSerializerOptions IndentedOptions { get; }
        public JsonSerializerOptions ApiRequestOptions { get; }
        public JsonSerializerOptions ApiResponseOptions { get; }
        public JsonSerializerOptions FileStorageOptions { get; }

        // Test data
        public string SimpleJson { get; }
        public string MediumJson { get; }
        public string ComplexJson { get; }
        public string InvalidJson { get; }

        // Test models
        public SystemPrompt SystemPrompt { get; }
        public LessonResource LessonResource { get; }
        public LessonTemplate LessonTemplate { get; }
        public LessonComponents LessonComponents { get; }

        public JsonFixture()
        {
            // Initialize JSON serialization options
            DefaultOptions = JsonSerializerOptionsFactory.Default;
            IndentedOptions = JsonSerializerOptionsFactory.Indented;
            ApiRequestOptions = JsonSerializerOptionsFactory.ApiRequest;
            ApiResponseOptions = JsonSerializerOptionsFactory.ApiResponse;
            FileStorageOptions = JsonSerializerOptionsFactory.FileStorage;

            // Create test JSON data
            SimpleJson = TestDataGenerator.GenerateRandomJson(1);
            MediumJson = TestDataGenerator.GenerateRandomJson(2);
            ComplexJson = TestDataGenerator.GenerateRandomJson(3);
            InvalidJson = "{\"name\": \"Test\", \"value\": 42,"; // Missing closing brace

            // Create test models
            SystemPrompt = TestDataGenerator.GenerateRandomSystemPrompt();
            LessonResource = TestDataGenerator.GenerateRandomLessonResource();
            LessonTemplate = TestDataGenerator.GenerateRandomLessonTemplate();
            LessonComponents = new LessonComponents
            {
                Introduction = "Introduction to the lesson",
                MainContent = "Main content of the lesson",
                Activities = new List<string> { "Activity 1", "Activity 2" },
                Assessment = "Assessment for the lesson",
                Homework = "Homework for the lesson",
                Resources = new List<string> { "Resource 1", "Resource 2" }
            };
        }

        /// <summary>
        /// Serialize an object to JSON using the specified options
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="value">The object to serialize</param>
        /// <param name="options">The serialization options (optional)</param>
        /// <returns>The JSON string</returns>
        public string Serialize<T>(T value, JsonSerializerOptions options = null)
        {
            return JsonHelper.Serialize(value, options ?? DefaultOptions);
        }

        /// <summary>
        /// Deserialize a JSON string to an object using the specified options
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="json">The JSON string</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>The deserialized object</returns>
        public T Deserialize<T>(string json, JsonSerializerOptions options = null)
        {
            return JsonHelper.Deserialize<T>(json, options ?? DefaultOptions);
        }

        /// <summary>
        /// Validate that a JSON string is valid and can be deserialized to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>True if the JSON is valid, false otherwise</returns>
        public bool ValidateJson<T>(string json, JsonSerializerOptions options = null)
        {
            return JsonHelper.ValidateJson<T>(json, out _, options ?? DefaultOptions);
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
        }
    }
}
