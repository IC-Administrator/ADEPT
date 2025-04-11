using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Services.Llm.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using Xunit;

namespace Adept.Services.Tests.Llm
{
    public class OpenRouterProviderTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ISecureStorageService> _mockSecureStorage;
        private readonly Mock<ILogger<OpenRouterProvider>> _mockLogger;
        private readonly OpenRouterProvider _provider;
        private readonly OpenRouterProvider _providerWithHttp;

        public OpenRouterProviderTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockSecureStorage = new Mock<ISecureStorageService>();
            _mockLogger = new Mock<ILogger<OpenRouterProvider>>();

            // Provider without HTTP client factory
            _provider = new OpenRouterProvider(_mockSecureStorage.Object, _mockLogger.Object);

            // Provider with HTTP client factory
            _providerWithHttp = new OpenRouterProvider(
                _mockHttpClientFactory.Object,
                _mockSecureStorage.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task InitializeAsync_WithValidApiKey_SetsInitializedState()
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.RetrieveSecureValueAsync("openrouter_api_key"))
                .ReturnsAsync("test_api_key");

            // Act
            await _provider.InitializeAsync();

            // Assert
            Assert.True(_provider.HasValidApiKey);
        }

        [Fact]
        public async Task InitializeAsync_WithInvalidApiKey_LogsWarning()
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.RetrieveSecureValueAsync("openrouter_api_key"))
                .ReturnsAsync(string.Empty);

            // Act
            await _provider.InitializeAsync();

            // Assert
            Assert.False(_provider.HasValidApiKey);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API key not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once
            );
        }

        [Fact]
        public void ProviderProperties_ReturnExpectedValues()
        {
            // Assert
            Assert.Equal("OpenRouter", _provider.ProviderName);
            Assert.True(_provider.RequiresApiKey);
            Assert.True(_provider.SupportsStreaming);
            Assert.True(_provider.SupportsToolCalls);
            Assert.True(_provider.SupportsVision);
            Assert.NotEmpty(_provider.AvailableModels);
            Assert.NotNull(_provider.CurrentModel);
            Assert.Equal(_provider.CurrentModel!.Id, _provider.ModelName);
        }

        [Fact]
        public async Task SetApiKeyAsync_StoresKeyInSecureStorage()
        {
            // Arrange
            string testApiKey = "test_api_key_123";
            _mockSecureStorage.Setup(x => x.StoreSecureValueAsync("openrouter_api_key", testApiKey))
                .Returns(Task.CompletedTask);

            // Act
            await _provider.SetApiKeyAsync(testApiKey);

            // Assert
            Assert.True(_provider.HasValidApiKey);
            _mockSecureStorage.Verify(x => x.StoreSecureValueAsync("openrouter_api_key", testApiKey), Times.Once);
        }

        [Fact]
        public async Task SetModelAsync_WithValidModel_ReturnsTrue()
        {
            // Arrange
            var availableModels = _provider.AvailableModels.ToList();
            var modelToSet = availableModels[1].Id; // Get the second model

            // Act
            var result = await _provider.SetModelAsync(modelToSet);

            // Assert
            Assert.True(result);
            Assert.Equal(modelToSet, _provider.CurrentModel!.Id);
        }

        [Fact]
        public async Task SetModelAsync_WithInvalidModel_ReturnsFalse()
        {
            // Act
            var result = await _provider.SetModelAsync("non_existent_model");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendMessageAsync_ReturnsValidResponse()
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.RetrieveSecureValueAsync("openrouter_api_key"))
                .ReturnsAsync("test_api_key");
            await _provider.InitializeAsync();

            // Act
            var response = await _provider.SendMessageAsync("Hello, world!");

            // Assert
            Assert.NotNull(response);
            Assert.Equal(LlmRole.Assistant, response.Message.Role);
            Assert.NotEmpty(response.Message.Content);
            Assert.Equal("OpenRouter", response.ProviderName);
            Assert.Equal(_provider.CurrentModel!.Name, response.ModelName);
        }
    }
}