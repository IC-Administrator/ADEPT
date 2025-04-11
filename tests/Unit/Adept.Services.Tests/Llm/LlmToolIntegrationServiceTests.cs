using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Services.Llm;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static Adept.Core.Interfaces.IMcpServerManager;

namespace Adept.Services.Tests.Llm
{
    public class LlmToolIntegrationServiceTests
    {
        private readonly Mock<IMcpServerManager> _mockMcpServerManager;
        private readonly Mock<ILogger<LlmToolIntegrationService>> _mockLogger;
        private readonly LlmToolIntegrationService _toolIntegrationService;

        public LlmToolIntegrationServiceTests()
        {
            _mockMcpServerManager = new Mock<IMcpServerManager>();
            _mockLogger = new Mock<ILogger<LlmToolIntegrationService>>();
            _toolIntegrationService = new LlmToolIntegrationService(_mockMcpServerManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ProcessToolCallsAsync_ShouldExecuteToolsAndUpdateResponse()
        {
            // Arrange
            var toolCalls = new List<LlmToolCall>
            {
                new LlmToolCall
                {
                    Id = "tool1",
                    ToolName = "get_weather",
                    Arguments = "{\"location\": \"New York\"}"
                },
                new LlmToolCall
                {
                    Id = "tool2",
                    ToolName = "search",
                    Arguments = "{\"query\": \"latest news\"}"
                }
            };

            var response = new LlmResponse
            {
                Message = new LlmMessage
                {
                    Role = LlmRole.Assistant,
                    Content = "I'll check that for you."
                },
                ToolCalls = toolCalls,
                ProviderName = "TestProvider",
                ModelName = "TestModel"
            };

            // Setup mock responses for tool execution
            _mockMcpServerManager.Setup(m => m.ExecuteToolAsync("get_weather", It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new McpToolResult(true, "{\"temperature\": 25, \"condition\": \"sunny\"}"));

            _mockMcpServerManager.Setup(m => m.ExecuteToolAsync("search", It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new McpToolResult(true, "[{\"title\": \"Breaking News\", \"url\": \"https://example.com/news\"}]"));

            // Act
            var result = await _toolIntegrationService.ProcessToolCallsAsync(response);

            // Assert
            Assert.Contains("Tool Results", result.Message.Content);
            Assert.Contains("get_weather", result.Message.Content);
            Assert.Contains("search", result.Message.Content);
            Assert.Contains("temperature", result.Message.Content);
            Assert.Contains("Breaking News", result.Message.Content);

            _mockMcpServerManager.Verify(m => m.ExecuteToolAsync("get_weather", It.IsAny<Dictionary<string, object>>()), Times.Once);
            _mockMcpServerManager.Verify(m => m.ExecuteToolAsync("search", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageToolCallsAsync_ShouldProcessToolCallsInMessage()
        {
            // Arrange
            var message = "Let me check the weather for you.\n\n```tool get_weather\n{\"location\": \"New York\"}\n```\n\nAnd also search for news:\n\n```tool search\n{\"query\": \"latest news\"}\n```";

            // Setup mock responses for tool execution
            _mockMcpServerManager.Setup(m => m.ExecuteToolAsync("get_weather", It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new McpToolResult(true, "{\"temperature\": 25, \"condition\": \"sunny\"}"));

            _mockMcpServerManager.Setup(m => m.ExecuteToolAsync("search", It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new McpToolResult(true, "[{\"title\": \"Breaking News\", \"url\": \"https://example.com/news\"}]"));

            // Act
            var result = await _toolIntegrationService.ProcessMessageToolCallsAsync(message);

            // Assert
            Assert.Contains("Tool Result", result);
            Assert.Contains("temperature", result);
            Assert.Contains("Breaking News", result);

            _mockMcpServerManager.Verify(m => m.ExecuteToolAsync("get_weather", It.IsAny<Dictionary<string, object>>()), Times.Once);
            _mockMcpServerManager.Verify(m => m.ExecuteToolAsync("search", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task ProcessToolCallsAsync_ShouldHandleToolExecutionErrors()
        {
            // Arrange
            var toolCalls = new List<LlmToolCall>
            {
                new LlmToolCall
                {
                    Id = "tool1",
                    ToolName = "get_weather",
                    Arguments = "{\"location\": \"New York\"}"
                }
            };

            var response = new LlmResponse
            {
                Message = new LlmMessage
                {
                    Role = LlmRole.Assistant,
                    Content = "I'll check that for you."
                },
                ToolCalls = toolCalls,
                ProviderName = "TestProvider",
                ModelName = "TestModel"
            };

            // Setup mock to return an error result
            _mockMcpServerManager.Setup(m => m.ExecuteToolAsync("get_weather", It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new McpToolResult { Success = false, ErrorMessage = "Tool execution failed" });

            // Act
            var result = await _toolIntegrationService.ProcessToolCallsAsync(response);

            // Assert
            Assert.Contains("Tool Results", result.Message.Content);
            Assert.Contains("get_weather", result.Message.Content);
            Assert.Contains("Error:", result.Message.Content);

            _mockMcpServerManager.Verify(m => m.ExecuteToolAsync("get_weather", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }
    }
}
