using Adept.Core.Interfaces;
using Adept.Services.Llm;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Services.Tests.Llm
{
    public class TokenCounterTests
    {
        [Fact]
        public void EstimateTokenCount_SingleMessage_ReturnsCorrectCount()
        {
            // Arrange
            var message = LlmMessage.User("Hello, world!");

            // Act
            var tokenCount = TokenCounter.EstimateTokenCount(message);

            // Assert
            Assert.True(tokenCount > 0);
            Assert.True(tokenCount < 20); // "Hello, world!" should be less than 20 tokens
        }

        [Fact]
        public void EstimateTokenCount_MultipleMessages_ReturnsCorrectCount()
        {
            // Arrange
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant."),
                LlmMessage.User("Hello, how are you?"),
                LlmMessage.Assistant("I'm doing well, thank you for asking. How can I help you today?")
            };

            // Act
            var tokenCount = TokenCounter.EstimateTokenCount(messages);

            // Assert
            Assert.True(tokenCount > 0);
            Assert.True(tokenCount < 100); // These messages should be less than 100 tokens
        }

        [Fact]
        public void TrimConversationToFitTokenLimit_UnderLimit_ReturnsOriginalList()
        {
            // Arrange
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant."),
                LlmMessage.User("Hello, how are you?"),
                LlmMessage.Assistant("I'm doing well, thank you for asking. How can I help you today?")
            };

            // Act
            var trimmedMessages = TokenCounter.TrimConversationToFitTokenLimit(messages, 1000);

            // Assert
            Assert.Equal(messages.Count, trimmedMessages.Count);
            Assert.Equal(messages[0].Content, trimmedMessages[0].Content);
            Assert.Equal(messages[1].Content, trimmedMessages[1].Content);
            Assert.Equal(messages[2].Content, trimmedMessages[2].Content);
        }

        [Fact]
        public void TrimConversationToFitTokenLimit_OverLimit_ReturnsTrimmedList()
        {
            // Arrange
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant."),
                LlmMessage.User("First message"),
                LlmMessage.Assistant("First response"),
                LlmMessage.User("Second message"),
                LlmMessage.Assistant("Second response"),
                LlmMessage.User("Third message"),
                LlmMessage.Assistant("Third response")
            };

            // Act - set a very low token limit to force trimming
            var trimmedMessages = TokenCounter.TrimConversationToFitTokenLimit(messages, 30);

            // Assert
            Assert.True(trimmedMessages.Count < messages.Count);
            Assert.Contains(trimmedMessages, m => m.Role == LlmRole.System); // System message should be preserved
        }

        [Fact]
        public void TrimConversationToFitTokenLimit_PreserveSystemMessage_SystemMessageIsPreserved()
        {
            // Arrange
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant with a very long system prompt that contains many tokens."),
                LlmMessage.User("Hello"),
                LlmMessage.Assistant("Hi")
            };

            // Act - set a token limit that would exclude the system message if not preserved
            var trimmedMessages = TokenCounter.TrimConversationToFitTokenLimit(messages, 20, true);

            // Assert
            Assert.Contains(trimmedMessages, m => m.Role == LlmRole.System);
        }

        [Fact]
        public void TrimConversationToFitTokenLimit_DoNotPreserveSystemMessage_SystemMessageMayBeRemoved()
        {
            // Arrange
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant with a very long system prompt that contains many tokens."),
                LlmMessage.User("Hello"),
                LlmMessage.Assistant("Hi")
            };

            // Act - set a token limit that would exclude the system message
            var trimmedMessages = TokenCounter.TrimConversationToFitTokenLimit(messages, 10, false);

            // Assert
            Assert.DoesNotContain(trimmedMessages, m => m.Role == LlmRole.System);
        }
    }
}
