using Adept.Common.Interfaces;
using Adept.Common.Json;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Data.Database;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Adept.TestUtilities.Helpers
{
    /// <summary>
    /// Factory for creating common mock objects used in tests
    /// </summary>
    public static class MockFactory
    {
        /// <summary>
        /// Create a mock logger
        /// </summary>
        /// <typeparam name="T">The type that the logger is for</typeparam>
        /// <returns>A mock logger</returns>
        public static Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Create a mock LLM provider
        /// </summary>
        /// <param name="providerName">The name of the provider</param>
        /// <param name="modelName">The name of the model</param>
        /// <param name="hasValidApiKey">Whether the provider has a valid API key</param>
        /// <returns>A mock LLM provider</returns>
        public static Mock<ILlmProvider> CreateMockLlmProvider(
            string providerName = "MockProvider",
            string modelName = "mock-model",
            bool hasValidApiKey = true)
        {
            var mockProvider = new Mock<ILlmProvider>();

            mockProvider.Setup(p => p.ProviderName).Returns(providerName);
            mockProvider.Setup(p => p.ModelName).Returns(modelName);
            mockProvider.Setup(p => p.HasValidApiKey).Returns(hasValidApiKey);
            mockProvider.Setup(p => p.RequiresApiKey).Returns(true);
            mockProvider.Setup(p => p.SupportsStreaming).Returns(true);
            mockProvider.Setup(p => p.SupportsToolCalls).Returns(true);
            mockProvider.Setup(p => p.SupportsVision).Returns(true);

            var models = new List<LlmModel>
            {
                new LlmModel
                {
                    Id = modelName,
                    Name = $"{providerName} Model",
                    MaxContextLength = 16000,
                    SupportsToolCalls = true,
                    SupportsVision = true
                }
            };

            mockProvider.Setup(p => p.AvailableModels).Returns(models);
            mockProvider.Setup(p => p.CurrentModel).Returns(models[0]);
            mockProvider.Setup(p => p.InitializeAsync()).Returns(Task.CompletedTask);
            mockProvider.Setup(p => p.SetApiKeyAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockProvider.Setup(p => p.SetModelAsync(It.IsAny<string>())).Returns(Task.FromResult(true));

            return mockProvider;
        }

        /// <summary>
        /// Create a mock file system service
        /// </summary>
        /// <param name="scratchpadDirectory">The scratchpad directory</param>
        /// <returns>A mock file system service</returns>
        public static Mock<IFileSystemService> CreateMockFileSystemService(string scratchpadDirectory = "C:\\Temp\\Adept\\Scratchpad")
        {
            var mockFileSystem = new Mock<IFileSystemService>();

            mockFileSystem.Setup(fs => fs.ScratchpadDirectory).Returns(scratchpadDirectory);
            mockFileSystem.Setup(fs => fs.EnsureStandardFoldersExistAsync()).Returns(Task.CompletedTask);

            return mockFileSystem;
        }

        /// <summary>
        /// Create a mock database context
        /// </summary>
        /// <returns>A mock database context</returns>
        public static Mock<IDatabaseContext> CreateMockDatabaseContext()
        {
            var mockDbContext = new Mock<IDatabaseContext>();

            mockDbContext.Setup(db => db.ExecuteScalarAsync<string>(
                    It.Is<string>(s => s.Contains("PRAGMA integrity_check")),
                    It.IsAny<object>()))
                .ReturnsAsync("ok");

            mockDbContext.Setup(db => db.ExecuteScalarAsync<long>(
                    It.Is<string>(s => s.Contains("PRAGMA foreign_key_check")),
                    It.IsAny<object>()))
                .ReturnsAsync(0);

            // Setup basic query methods for common types
            mockDbContext.Setup(db => db.QueryAsync<LessonTemplate>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(Enumerable.Empty<LessonTemplate>());

            mockDbContext.Setup(db => db.QueryAsync<LessonResource>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(Enumerable.Empty<LessonResource>());

            mockDbContext.Setup(db => db.QueryAsync<SystemPrompt>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(Enumerable.Empty<SystemPrompt>());

            mockDbContext.Setup(db => db.QuerySingleOrDefaultAsync<LessonTemplate>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync((LessonTemplate)null);

            mockDbContext.Setup(db => db.QuerySingleOrDefaultAsync<LessonResource>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync((LessonResource)null);

            mockDbContext.Setup(db => db.QuerySingleOrDefaultAsync<SystemPrompt>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync((SystemPrompt)null);

            mockDbContext.Setup(db => db.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            mockDbContext.Setup(db => db.ExecuteScalarAsync<int>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            // Setup transaction methods
            mockDbContext.Setup(db => db.BeginTransactionAsync())
                .ReturnsAsync(new Mock<Adept.Common.Interfaces.IDbTransaction>().Object);

            return mockDbContext;
        }

        /// <summary>
        /// Create a mock system prompt repository
        /// </summary>
        /// <returns>A mock system prompt repository</returns>
        public static Mock<ISystemPromptRepository> CreateMockSystemPromptRepository()
        {
            var mockRepo = new Mock<ISystemPromptRepository>();
            var prompts = new List<SystemPrompt>
            {
                new SystemPrompt
                {
                    PromptId = Guid.NewGuid().ToString(),
                    Name = "Default Prompt",
                    Content = "{\"role\": \"system\", \"content\": \"You are a helpful assistant.\"}",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new SystemPrompt
                {
                    PromptId = Guid.NewGuid().ToString(),
                    Name = "Teacher Prompt",
                    Content = "{\"role\": \"system\", \"content\": \"You are a helpful teaching assistant.\"}",
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            mockRepo.Setup(r => r.GetAllPromptsAsync())
                .ReturnsAsync(prompts);

            mockRepo.Setup(r => r.GetPromptByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => prompts.FirstOrDefault(p => p.PromptId == id));

            mockRepo.Setup(r => r.GetDefaultPromptAsync())
                .ReturnsAsync(prompts.First(p => p.IsDefault));

            mockRepo.Setup(r => r.AddPromptAsync(It.IsAny<SystemPrompt>()))
                .ReturnsAsync((SystemPrompt prompt) =>
                {
                    var newPrompt = prompt.Clone();
                    newPrompt.PromptId = Guid.NewGuid().ToString();
                    newPrompt.CreatedAt = DateTime.UtcNow;
                    newPrompt.UpdatedAt = DateTime.UtcNow;
                    prompts.Add(newPrompt);
                    return newPrompt;
                });

            mockRepo.Setup(r => r.UpdatePromptAsync(It.IsAny<SystemPrompt>()))
                .ReturnsAsync((SystemPrompt prompt) =>
                {
                    var existingPrompt = prompts.FirstOrDefault(p => p.PromptId == prompt.PromptId);
                    if (existingPrompt == null) return null;

                    existingPrompt.Name = prompt.Name;
                    existingPrompt.Content = prompt.Content;
                    existingPrompt.IsDefault = prompt.IsDefault;
                    existingPrompt.UpdatedAt = DateTime.UtcNow;
                    return existingPrompt;
                });

            mockRepo.Setup(r => r.DeletePromptAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) =>
                {
                    var existingPrompt = prompts.FirstOrDefault(p => p.PromptId == id);
                    if (existingPrompt == null) return false;

                    prompts.Remove(existingPrompt);
                    return true;
                });

            mockRepo.Setup(r => r.SetDefaultPromptAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) =>
                {
                    var existingPrompt = prompts.FirstOrDefault(p => p.PromptId == id);
                    if (existingPrompt == null) return false;

                    foreach (var p in prompts)
                    {
                        p.IsDefault = p.PromptId == id;
                    }
                    return true;
                });

            return mockRepo;
        }

        /// <summary>
        /// Create a mock lesson resource repository
        /// </summary>
        /// <returns>A mock lesson resource repository</returns>
        public static Mock<ILessonResourceRepository> CreateMockLessonResourceRepository()
        {
            var mockRepo = new Mock<ILessonResourceRepository>();
            var resources = new List<LessonResource>
            {
                new LessonResource
                {
                    ResourceId = Guid.NewGuid(),
                    LessonId = Guid.NewGuid(),
                    Name = "Sample Resource 1",
                    Type = ResourceType.File,
                    Path = "path/to/resource1.pdf",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new LessonResource
                {
                    ResourceId = Guid.NewGuid(),
                    LessonId = Guid.NewGuid(),
                    Name = "Sample Resource 2",
                    Type = ResourceType.Link,
                    Path = "https://example.com/resource2",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            mockRepo.Setup(r => r.GetResourcesByLessonIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid lessonId) => resources.Where(r => r.LessonId == lessonId).ToList());

            mockRepo.Setup(r => r.GetResourceByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => resources.FirstOrDefault(r => r.ResourceId == id));

            mockRepo.Setup(r => r.AddResourceAsync(It.IsAny<LessonResource>()))
                .ReturnsAsync((LessonResource resource) =>
                {
                    var newResource = new LessonResource
                    {
                        ResourceId = Guid.NewGuid(),
                        LessonId = resource.LessonId,
                        Name = resource.Name,
                        Type = resource.Type,
                        Path = resource.Path,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    resources.Add(newResource);
                    return newResource;
                });

            mockRepo.Setup(r => r.UpdateResourceAsync(It.IsAny<LessonResource>()))
                .ReturnsAsync((LessonResource resource) =>
                {
                    var existingResource = resources.FirstOrDefault(r => r.ResourceId == resource.ResourceId);
                    if (existingResource == null) return null;

                    existingResource.Name = resource.Name;
                    existingResource.Type = resource.Type;
                    existingResource.Path = resource.Path;
                    existingResource.UpdatedAt = DateTime.UtcNow;
                    return existingResource;
                });

            mockRepo.Setup(r => r.DeleteResourceAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var existingResource = resources.FirstOrDefault(r => r.ResourceId == id);
                    if (existingResource == null) return false;

                    resources.Remove(existingResource);
                    return true;
                });

            mockRepo.Setup(r => r.DeleteResourcesByLessonIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid lessonId) =>
                {
                    var lessonResources = resources.Where(r => r.LessonId == lessonId).ToList();
                    foreach (var resource in lessonResources)
                    {
                        resources.Remove(resource);
                    }
                    return lessonResources.Any();
                });

            return mockRepo;
        }

        /// <summary>
        /// Create a mock lesson template repository
        /// </summary>
        /// <returns>A mock lesson template repository</returns>
        public static Mock<ILessonTemplateRepository> CreateMockLessonTemplateRepository()
        {
            var mockRepo = new Mock<ILessonTemplateRepository>();
            var templates = new List<LessonTemplate>
            {
                new LessonTemplate
                {
                    TemplateId = Guid.NewGuid(),
                    Name = "Math Lesson Template",
                    Description = "A template for math lessons",
                    Category = "Math",
                    Tags = new List<string> { "algebra", "geometry", "calculus" },
                    Title = "Math Lesson",
                    LearningObjectives = "Understand basic math concepts",
                    ComponentsJson = "{\"introduction\": \"Introduction to math\", \"activities\": []}",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new LessonTemplate
                {
                    TemplateId = Guid.NewGuid(),
                    Name = "Science Lesson Template",
                    Description = "A template for science lessons",
                    Category = "Science",
                    Tags = new List<string> { "physics", "chemistry", "biology" },
                    Title = "Science Lesson",
                    LearningObjectives = "Understand basic science concepts",
                    ComponentsJson = "{\"introduction\": \"Introduction to science\", \"activities\": []}",
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            mockRepo.Setup(r => r.GetAllTemplatesAsync())
                .ReturnsAsync(templates);

            mockRepo.Setup(r => r.GetTemplateByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => templates.FirstOrDefault(t => t.TemplateId == id));

            mockRepo.Setup(r => r.GetTemplatesByCategoryAsync(It.IsAny<string>()))
                .ReturnsAsync((string category) => templates.Where(t => t.Category == category).ToList());

            mockRepo.Setup(r => r.GetTemplatesByTagAsync(It.IsAny<string>()))
                .ReturnsAsync((string tag) => templates.Where(t => t.Tags.Contains(tag)).ToList());

            mockRepo.Setup(r => r.AddTemplateAsync(It.IsAny<LessonTemplate>()))
                .ReturnsAsync((LessonTemplate template) =>
                {
                    var newTemplate = new LessonTemplate
                    {
                        TemplateId = Guid.NewGuid(),
                        Name = template.Name,
                        Description = template.Description,
                        Category = template.Category,
                        Tags = template.Tags,
                        Title = template.Title,
                        LearningObjectives = template.LearningObjectives,
                        ComponentsJson = template.ComponentsJson,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    templates.Add(newTemplate);
                    return newTemplate;
                });

            mockRepo.Setup(r => r.UpdateTemplateAsync(It.IsAny<LessonTemplate>()))
                .ReturnsAsync((LessonTemplate template) =>
                {
                    var existingTemplate = templates.FirstOrDefault(t => t.TemplateId == template.TemplateId);
                    if (existingTemplate == null) return null;

                    existingTemplate.Name = template.Name;
                    existingTemplate.Description = template.Description;
                    existingTemplate.Category = template.Category;
                    existingTemplate.Tags = template.Tags;
                    existingTemplate.Title = template.Title;
                    existingTemplate.LearningObjectives = template.LearningObjectives;
                    existingTemplate.ComponentsJson = template.ComponentsJson;
                    existingTemplate.UpdatedAt = DateTime.UtcNow;
                    return existingTemplate;
                });

            mockRepo.Setup(r => r.DeleteTemplateAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var existingTemplate = templates.FirstOrDefault(t => t.TemplateId == id);
                    if (existingTemplate == null) return false;

                    templates.Remove(existingTemplate);
                    return true;
                });

            return mockRepo;
        }
    }
}
