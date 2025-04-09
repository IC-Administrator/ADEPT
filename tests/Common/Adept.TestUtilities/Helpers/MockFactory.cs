using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
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

            return mockDbContext;
        }
    }
}
