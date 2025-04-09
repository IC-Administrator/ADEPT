using Adept.Core.Models;
using Adept.TestUtilities.Helpers;
using System;
using Xunit;

namespace Adept.Core.Tests.Models
{
    public class LlmMessageTests
    {
        [Fact]
        public void User_CreatesUserMessage()
        {
            // Arrange
            string content = "Hello, world!";

            // Act
            var message = LlmMessage.User(content);

            // Assert
            Assert.Equal(LlmRole.User, message.Role);
            Assert.Equal(content, message.Content);
            Assert.Null(message.Name);
        }

        [Fact]
        public void System_CreatesSystemMessage()
        {
            // Arrange
            string content = "You are a helpful assistant.";

            // Act
            var message = LlmMessage.System(content);

            // Assert
            Assert.Equal(LlmRole.System, message.Role);
            Assert.Equal(content, message.Content);
            Assert.Null(message.Name);
        }

        [Fact]
        public void Assistant_CreatesAssistantMessage()
        {
            // Arrange
            string content = "I'm here to help!";

            // Act
            var message = LlmMessage.Assistant(content);

            // Assert
            Assert.Equal(LlmRole.Assistant, message.Role);
            Assert.Equal(content, message.Content);
            Assert.Null(message.Name);
        }

        [Fact]
        public void Tool_CreatesToolMessage()
        {
            // Arrange
            string content = "Tool response";
            string name = "calculator";

            // Act
            var message = LlmMessage.Tool(content, name);

            // Assert
            Assert.Equal(LlmRole.Tool, message.Role);
            Assert.Equal(content, message.Content);
            Assert.Equal(name, message.Name);
        }

        [Fact]
        public void Tool_WithoutName_ThrowsArgumentException()
        {
            // Arrange
            string content = "Tool response";

            // Act & Assert
            var exception = AssertExtensions.ThrowsWithMessage<ArgumentException>(
                () => LlmMessage.Tool(content, null),
                "Tool messages must have a name");
        }
    }
}
