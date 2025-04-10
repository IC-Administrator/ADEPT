using Adept.Core.Interfaces;
using Adept.Services.Mcp;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Services.Tests.Mcp
{
    public class PuppeteerToolProviderTests
    {
        private readonly Mock<ILogger<PuppeteerToolProvider>> _loggerMock;
        private readonly PuppeteerToolProvider _provider;

        public PuppeteerToolProviderTests()
        {
            _loggerMock = new Mock<ILogger<PuppeteerToolProvider>>();
            _provider = new PuppeteerToolProvider(_loggerMock.Object);
        }

        [Fact]
        public void ProviderName_ShouldReturnCorrectName()
        {
            // Assert
            Assert.Equal("Puppeteer", _provider.ProviderName);
        }

        [Fact]
        public void Tools_ShouldContainExpectedTools()
        {
            // Arrange
            var expectedToolNames = new[]
            {
                "puppeteer_navigate",
                "puppeteer_screenshot",
                "puppeteer_extract_content",
                "puppeteer_click",
                "puppeteer_type"
            };

            // Act
            var tools = _provider.Tools;

            // Assert
            Assert.Equal(expectedToolNames.Length, tools.Count());
            foreach (var toolName in expectedToolNames)
            {
                Assert.Contains(tools, t => t.Name == toolName);
            }
        }

        [Fact]
        public void GetTool_ShouldReturnCorrectTool()
        {
            // Act
            var tool = _provider.GetTool("puppeteer_navigate");

            // Assert
            Assert.NotNull(tool);
            Assert.Equal("puppeteer_navigate", tool.Name);
        }

        [Fact]
        public void GetTool_ShouldReturnNull_WhenToolDoesNotExist()
        {
            // Act
            var tool = _provider.GetTool("non_existent_tool");

            // Assert
            Assert.Null(tool);
        }

        [Fact]
        public void GetToolSchema_ShouldReturnSchemaForAllTools()
        {
            // Act
            var schemas = _provider.GetToolSchema();

            // Assert
            Assert.Equal(5, schemas.Count());
            Assert.Contains(schemas, s => s.Name == "puppeteer_navigate");
            Assert.Contains(schemas, s => s.Name == "puppeteer_screenshot");
            Assert.Contains(schemas, s => s.Name == "puppeteer_extract_content");
            Assert.Contains(schemas, s => s.Name == "puppeteer_click");
            Assert.Contains(schemas, s => s.Name == "puppeteer_type");
        }

        [Fact]
        public async Task ExecuteToolAsync_ShouldReturnError_WhenToolDoesNotExist()
        {
            // Act
            var result = await _provider.ExecuteToolAsync("non_existent_tool", new Dictionary<string, object>());

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not found", result.ErrorMessage);
        }
    }
}
