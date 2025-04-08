using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Services.Llm;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Services.Tests.Llm
{
    public class LlmServiceFallbackTests
    {
        private readonly Mock<ILlmProvider> _mockPrimaryProvider;
        private readonly Mock<ILlmProvider> _mockFallbackProvider;
        private readonly Mock<IConversationRepository> _mockConversationRepository;
        private readonly Mock<ISystemPromptService> _mockSystemPromptService;
        private readonly Mock<ILogger<LlmService>> _mockLogger;
        private readonly LlmService _llmService;

        public LlmServiceFallbackTests()
        {
            // Setup primary provider
            _mockPrimaryProvider = new Mock<ILlmProvider>();
            _mockPrimaryProvider.Setup(p => p.ProviderName).Returns("PrimaryProvider");
            _mockPrimaryProvider.Setup(p => p.HasValidApiKey).Returns(true);
            _mockPrimaryProvider.Setup(p => p.ModelName).Returns("primary-model");
            
            // Setup fallback provider
            _mockFallbackProvider = new Mock<ILlmProvider>();
            _mockFallbackProvider.Setup(p => p.ProviderName).Returns("FallbackProvider");
            _mockFallbackProvider.Setup(p => p.HasValidApiKey).Returns(true);
            _mockFallbackProvider.Setup(p => p.ModelName).Returns("fallback-model");
            
            // Setup other dependencies
            _mockConversationRepository = new Mock<IConversationRepository>();
            _mockSystemPromptService = new Mock<ISystemPromptService>();
            _mockLogger = new Mock<ILogger<LlmService>>();
            
            // Create service with providers
            var providers = new List<ILlmProvider> { _mockPrimaryProvider.Object, _mockFallbackProvider.Object };
            _llmService = new LlmService(
                providers, 
                _mockConversationRepository.Object,
                _mockSystemPromptService.Object,
                null, // Tool integration service is not needed for these tests
                _mockLogger.Object);
        }

        [Fact]
        public async Task SendMessageAsync_PrimaryProviderFails_FallsBackToSecondProvider()
        {
            // Arrange
            var message = "Test message";
            var systemPrompt = "System prompt";
            var conversationId = "conv123";
            
            // Setup conversation repository
            var conversation = new Conversation { ConversationId = conversationId };
            _mockConversationRepository.Setup(r => r.GetConversationByIdAsync(conversationId))
                .ReturnsAsync(conversation);
            
            // Setup primary provider to fail
            _mockPrimaryProvider.Setup(p => p.SendMessagesAsync(
                    It.IsAny<IEnumerable<LlmMessage>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("API error"));
            
            // Setup fallback provider to succeed
            var expectedResponse = new LlmResponse
            {
                Message = LlmMessage.Assistant("Fallback response"),
                ProviderName = "FallbackProvider",
                ModelName = "fallback-model"
            };
            
            _mockFallbackProvider.Setup(p => p.SendMessagesAsync(
                    It.IsAny<IEnumerable<LlmMessage>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);
            
            // Act
            var result = await _llmService.SendMessageAsync(message, systemPrompt, conversationId);
            
            // Assert
            Assert.Equal(expectedResponse.Message.Content, result.Message.Content);
            Assert.Equal("FallbackProvider", result.ProviderName);
            
            // Verify primary provider was called
            _mockPrimaryProvider.Verify(p => p.SendMessagesAsync(
                It.IsAny<IEnumerable<LlmMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
            
            // Verify fallback provider was called
            _mockFallbackProvider.Verify(p => p.SendMessagesAsync(
                It.IsAny<IEnumerable<LlmMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
