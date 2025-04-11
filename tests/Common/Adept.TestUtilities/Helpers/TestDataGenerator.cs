using Adept.Common.Json;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Adept.TestUtilities.Helpers
{
    /// <summary>
    /// Helper class for generating test data
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random Random = new Random();

        /// <summary>
        /// Generate a random string of the specified length
        /// </summary>
        /// <param name="length">The length of the string to generate</param>
        /// <returns>A random string</returns>
        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[Random.Next(chars.Length)];
            }

            return new string(stringChars);
        }

        /// <summary>
        /// Generate a random date within the specified range
        /// </summary>
        /// <param name="startDate">The start date of the range</param>
        /// <param name="endDate">The end date of the range</param>
        /// <returns>A random date within the range</returns>
        public static DateTime GenerateRandomDate(DateTime startDate, DateTime endDate)
        {
            TimeSpan timeSpan = endDate - startDate;
            TimeSpan newSpan = new TimeSpan(0, Random.Next(0, (int)timeSpan.TotalMinutes), 0);
            return startDate + newSpan;
        }

        /// <summary>
        /// Generate a list of random LLM messages for testing
        /// </summary>
        /// <param name="count">The number of messages to generate</param>
        /// <returns>A list of random LLM messages</returns>
        public static List<LlmMessage> GenerateRandomLlmMessages(int count)
        {
            var messages = new List<LlmMessage>();

            // Always add a system message first
            messages.Add(LlmMessage.System("You are a helpful assistant."));

            // Add alternating user and assistant messages
            for (int i = 0; i < count - 1; i++)
            {
                if (i % 2 == 0)
                {
                    messages.Add(LlmMessage.User($"User message {i/2 + 1}: {GenerateRandomString(20)}"));
                }
                else
                {
                    messages.Add(LlmMessage.Assistant($"Assistant response {i/2 + 1}: {GenerateRandomString(30)}"));
                }
            }

            return messages;
        }

        /// <summary>
        /// Generate a random file path
        /// </summary>
        /// <returns>A random file path</returns>
        public static string GenerateRandomFilePath()
        {
            string[] extensions = { ".txt", ".md", ".json", ".csv", ".xml" };
            string fileName = $"{GenerateRandomString(8)}{extensions[Random.Next(extensions.Length)]}";
            string[] directories = { "", "folder1", "folder2/subfolder", "documents" };
            string directory = directories[Random.Next(directories.Length)];

            return string.IsNullOrEmpty(directory) ? fileName : $"{directory}/{fileName}";
        }

        /// <summary>
        /// Generate a random system prompt
        /// </summary>
        /// <param name="isDefault">Whether the prompt should be the default</param>
        /// <returns>A random system prompt</returns>
        public static SystemPrompt GenerateRandomSystemPrompt(bool isDefault = false)
        {
            return new SystemPrompt
            {
                PromptId = Guid.NewGuid().ToString(),
                Name = $"Test Prompt {GenerateRandomString(5)}",
                Content = $"{{\"role\": \"system\", \"content\": \"You are a {GenerateRandomString(10)} assistant.\"}}",
                IsDefault = isDefault,
                CreatedAt = GenerateRandomDate(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1)),
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Generate a list of random system prompts
        /// </summary>
        /// <param name="count">The number of prompts to generate</param>
        /// <returns>A list of random system prompts</returns>
        public static List<SystemPrompt> GenerateRandomSystemPrompts(int count)
        {
            var prompts = new List<SystemPrompt>();

            // Add one default prompt
            prompts.Add(GenerateRandomSystemPrompt(true));

            // Add the rest as non-default prompts
            for (int i = 1; i < count; i++)
            {
                prompts.Add(GenerateRandomSystemPrompt(false));
            }

            return prompts;
        }

        /// <summary>
        /// Generate a random lesson resource
        /// </summary>
        /// <param name="lessonId">The ID of the lesson</param>
        /// <param name="type">The type of resource</param>
        /// <returns>A random lesson resource</returns>
        public static LessonResource GenerateRandomLessonResource(Guid? lessonId = null, ResourceType? type = null)
        {
            if (lessonId == null)
            {
                lessonId = Guid.NewGuid();
            }

            if (type == null)
            {
                type = (ResourceType)Random.Next(0, 2); // File or Link
            }

            string path = type == ResourceType.File
                ? $"path/to/{GenerateRandomString(8)}.pdf"
                : $"https://example.com/{GenerateRandomString(10)}";

            return new LessonResource
            {
                ResourceId = Guid.NewGuid(),
                LessonId = lessonId.Value,
                Name = $"Resource {GenerateRandomString(5)}",
                Type = type.Value,
                Path = path,
                CreatedAt = GenerateRandomDate(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1)),
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Generate a list of random lesson resources
        /// </summary>
        /// <param name="count">The number of resources to generate</param>
        /// <param name="lessonId">The ID of the lesson</param>
        /// <returns>A list of random lesson resources</returns>
        public static List<LessonResource> GenerateRandomLessonResources(int count, Guid? lessonId = null)
        {
            if (lessonId == null)
            {
                lessonId = Guid.NewGuid();
            }

            var resources = new List<LessonResource>();

            for (int i = 0; i < count; i++)
            {
                resources.Add(GenerateRandomLessonResource(lessonId));
            }

            return resources;
        }

        /// <summary>
        /// Generate a random lesson template
        /// </summary>
        /// <param name="category">The category of the template</param>
        /// <returns>A random lesson template</returns>
        public static LessonTemplate GenerateRandomLessonTemplate(string category = null)
        {
            if (string.IsNullOrEmpty(category))
            {
                string[] categories = { "Math", "Science", "English", "History", "Art" };
                category = categories[Random.Next(categories.Length)];
            }

            string[] tags = { "beginner", "intermediate", "advanced", "interactive", "homework", "project" };
            var selectedTags = new List<string>();
            int tagCount = Random.Next(1, 4); // 1-3 tags

            for (int i = 0; i < tagCount; i++)
            {
                string tag = tags[Random.Next(tags.Length)];
                if (!selectedTags.Contains(tag))
                {
                    selectedTags.Add(tag);
                }
            }

            var components = new LessonComponents
            {
                Introduction = $"Introduction to {category} lesson",
                MainContent = $"Main content for {category} lesson",
                Activities = new List<string> { $"Activity 1 for {category}", $"Activity 2 for {category}" },
                Assessment = $"Assessment for {category} lesson",
                Homework = $"Homework for {category} lesson",
                Resources = new List<string> { $"Resource 1 for {category}", $"Resource 2 for {category}" }
            };

            string componentsJson = components.ToJson();

            return new LessonTemplate
            {
                TemplateId = Guid.NewGuid(),
                Name = $"{category} Lesson Template {GenerateRandomString(5)}",
                Description = $"A template for {category.ToLower()} lessons",
                Category = category,
                Tags = selectedTags,
                Title = $"{category} Lesson",
                LearningObjectives = $"Understand basic {category.ToLower()} concepts",
                ComponentsJson = componentsJson,
                CreatedAt = GenerateRandomDate(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1)),
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Generate a list of random lesson templates
        /// </summary>
        /// <param name="count">The number of templates to generate</param>
        /// <returns>A list of random lesson templates</returns>
        public static List<LessonTemplate> GenerateRandomLessonTemplates(int count)
        {
            var templates = new List<LessonTemplate>();
            string[] categories = { "Math", "Science", "English", "History", "Art" };

            for (int i = 0; i < count; i++)
            {
                string category = categories[i % categories.Length]; // Cycle through categories
                templates.Add(GenerateRandomLessonTemplate(category));
            }

            return templates;
        }

        /// <summary>
        /// Generate random JSON data
        /// </summary>
        /// <param name="complexity">The complexity of the JSON (1-3)</param>
        /// <returns>A random JSON string</returns>
        public static string GenerateRandomJson(int complexity = 1)
        {
            switch (complexity)
            {
                case 1: // Simple JSON
                    var simpleObject = new
                    {
                        id = Guid.NewGuid().ToString(),
                        name = GenerateRandomString(10),
                        value = Random.Next(1, 100),
                        isActive = Random.Next(0, 2) == 1
                    };
                    return JsonSerializer.Serialize(simpleObject, JsonSerializerOptionsFactory.Default);

                case 2: // Medium complexity
                    var mediumObject = new
                    {
                        id = Guid.NewGuid().ToString(),
                        name = GenerateRandomString(10),
                        properties = new
                        {
                            color = new[] { "red", "green", "blue" }[Random.Next(0, 3)],
                            size = new[] { "small", "medium", "large" }[Random.Next(0, 3)],
                            weight = Random.Next(1, 100)
                        },
                        tags = Enumerable.Range(0, Random.Next(1, 5))
                            .Select(_ => GenerateRandomString(5))
                            .ToArray(),
                        isActive = Random.Next(0, 2) == 1
                    };
                    return JsonSerializer.Serialize(mediumObject, JsonSerializerOptionsFactory.Default);

                case 3: // High complexity
                    var items = Enumerable.Range(0, Random.Next(2, 6))
                        .Select(i => new
                        {
                            id = i,
                            name = $"Item {GenerateRandomString(5)}",
                            value = Random.Next(1, 100),
                            properties = new Dictionary<string, object>
                            {
                                { "color", new[] { "red", "green", "blue" }[Random.Next(0, 3)] },
                                { "size", new[] { "small", "medium", "large" }[Random.Next(0, 3)] },
                                { "features", Enumerable.Range(0, Random.Next(1, 4))
                                    .Select(_ => GenerateRandomString(6))
                                    .ToArray() }
                            }
                        })
                        .ToArray();

                    var complexObject = new
                    {
                        id = Guid.NewGuid().ToString(),
                        name = GenerateRandomString(10),
                        description = GenerateRandomString(30),
                        created = DateTime.UtcNow.AddDays(-Random.Next(1, 30)),
                        items = items,
                        metadata = new
                        {
                            version = $"{Random.Next(1, 5)}.{Random.Next(0, 10)}.{Random.Next(0, 10)}",
                            environment = new[] { "development", "staging", "production" }[Random.Next(0, 3)],
                            debug = Random.Next(0, 2) == 1
                        }
                    };
                    return JsonSerializer.Serialize(complexObject, JsonSerializerOptionsFactory.Default);

                default:
                    return "{}";
            }
        }
    }
}
