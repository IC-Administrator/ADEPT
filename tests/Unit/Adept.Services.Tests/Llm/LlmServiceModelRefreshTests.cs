using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Services.Llm;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Services.Tests.Llm
{
    public class LlmServiceModelRefreshTests
    {
        private readonly Mock<IConversationRepository> _mockConversationRepository;
        private readonly Mock<ISystemPromptService> _mockSystemPromptService;
        private readonly Mock<LlmToolIntegrationService> _mockToolIntegrationService;
        private readonly Mock<ILogger<LlmService>> _mockLogger;

        public LlmServiceModelRefreshTests()
        {
            _mockConversationRepository = new Mock<IConversationRepository>();
            _mockSystemPromptService = new Mock<ISystemPromptService>();
            _mockToolIntegrationService = new Mock<LlmToolIntegrationService>();
            _mockLogger = new Mock<ILogger<LlmService>>();
        }

        [Fact]
        public async Task RefreshModelsAsync_ShouldCallFetchAvailableModelsAsync_ForAllProvidersWithValidApiKeys()
        {
            // Arrange
            var mockProvider1 = new Mock<ILlmProvider>();
            mockProvider1.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider1.Setup(p => p.HasValidApiKey).Returns(true);
            mockProvider1.Setup(p => p.FetchAvailableModelsAsync()).ReturnsAsync(new List<LlmModel>
            {
                new LlmModel("model1", "Model 1", 1000)
            });

            var mockProvider2 = new Mock<ILlmProvider>();
            mockProvider2.Setup(p => p.ProviderName).Returns("Provider2");
            mockProvider2.Setup(p => p.HasValidApiKey).Returns(false);

            var mockProvider3 = new Mock<ILlmProvider>();
            mockProvider3.Setup(p => p.ProviderName).Returns("Provider3");
            mockProvider3.Setup(p => p.HasValidApiKey).Returns(true);
            mockProvider3.Setup(p => p.FetchAvailableModelsAsync()).ReturnsAsync(new List<LlmModel>
            {
                new LlmModel("model3", "Model 3", 1000)
            });

            var providers = new List<ILlmProvider>
            {
                mockProvider1.Object,
                mockProvider2.Object,
                mockProvider3.Object
            };

            var llmService = new LlmService(
                providers,
                _mockConversationRepository.Object,
                _mockSystemPromptService.Object,
                _mockToolIntegrationService.Object,
                _mockLogger.Object);

            // Act
            await llmService.RefreshModelsAsync();

            // Assert
            mockProvider1.Verify(p => p.FetchAvailableModelsAsync(), Times.Once);
            mockProvider2.Verify(p => p.FetchAvailableModelsAsync(), Times.Never);
            mockProvider3.Verify(p => p.FetchAvailableModelsAsync(), Times.Once);
        }

        [Fact]
        public async Task RefreshModelsForProviderAsync_ShouldReturnFalse_WhenProviderNotFound()
        {
            // Arrange
            var providers = new List<ILlmProvider>();
            var llmService = new LlmService(
                providers,
                _mockConversationRepository.Object,
                _mockSystemPromptService.Object,
                _mockToolIntegrationService.Object,
                _mockLogger.Object);

            // Act
            var result = await llmService.RefreshModelsForProviderAsync("NonExistentProvider");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RefreshModelsForProviderAsync_ShouldReturnFalse_WhenProviderHasNoValidApiKey()
        {
            // Arrange
            var mockProvider = new Mock<ILlmProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Provider");
            mockProvider.Setup(p => p.HasValidApiKey).Returns(false);

            var providers = new List<ILlmProvider> { mockProvider.Object };
            var llmService = new LlmService(
                providers,
                _mockConversationRepository.Object,
                _mockSystemPromptService.Object,
                _mockToolIntegrationService.Object,
                _mockLogger.Object);

            // Act
            var result = await llmService.RefreshModelsForProviderAsync("Provider");

            // Assert
            Assert.False(result);
            mockProvider.Verify(p => p.FetchAvailableModelsAsync(), Times.Never);
        }

        [Fact]
        public async Task RefreshModelsForProviderAsync_ShouldReturnTrue_WhenProviderRefreshedSuccessfully()
        {
            // Arrange
            var mockProvider = new Mock<ILlmProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Provider");
            mockProvider.Setup(p => p.HasValidApiKey).Returns(true);
            mockProvider.Setup(p => p.FetchAvailableModelsAsync()).ReturnsAsync(new List<LlmModel>
            {
                new LlmModel("model1", "Model 1", 1000)
            });

            var providers = new List<ILlmProvider> { mockProvider.Object };
            var llmService = new LlmService(
                providers,
                _mockConversationRepository.Object,
                _mockSystemPromptService.Object,
                _mockToolIntegrationService.Object,
                _mockLogger.Object);

            // Act
            var result = await llmService.RefreshModelsForProviderAsync("Provider");

            // Assert
            Assert.True(result);
            mockProvider.Verify(p => p.FetchAvailableModelsAsync(), Times.Once);
        }
    }
}
