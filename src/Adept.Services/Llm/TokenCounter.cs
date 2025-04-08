using Adept.Core.Interfaces;
using Adept.Core.Models;
using System.Text.RegularExpressions;

namespace Adept.Services.Llm
{
    /// <summary>
    /// Utility class for estimating token counts for LLM messages
    /// </summary>
    public static class TokenCounter
    {
        // Average tokens per character for different languages (approximate)
        private const double EnglishTokensPerChar = 0.25;
        private const double ChineseJapaneseTokensPerChar = 1.0;
        private const double OtherLanguagesTokensPerChar = 0.35;

        // Regex patterns for detecting languages
        private static readonly Regex ChineseJapanesePattern = new Regex(@"[\p{IsHangulJamo}\p{IsCJKUnifiedIdeographs}\p{IsCJKSymbolsAndPunctuation}\p{IsHiragana}\p{IsKatakana}]", RegexOptions.Compiled);

        // Base token overhead for different message types
        private const int SystemMessageOverhead = 4;
        private const int UserMessageOverhead = 4;
        private const int AssistantMessageOverhead = 4;
        private const int ToolCallOverhead = 10;
        private const int ToolResponseOverhead = 10;

        /// <summary>
        /// Estimates the token count for a single message
        /// </summary>
        /// <param name="message">The message to estimate tokens for</param>
        /// <returns>The estimated token count</returns>
        public static int EstimateTokenCount(LlmMessage message)
        {
            if (string.IsNullOrEmpty(message.Content))
            {
                return GetMessageOverhead(message.Role);
            }

            int contentTokens = EstimateContentTokens(message.Content);
            int overhead = GetMessageOverhead(message.Role);

            return contentTokens + overhead;
        }

        /// <summary>
        /// Estimates the token count for a list of messages
        /// </summary>
        /// <param name="messages">The messages to estimate tokens for</param>
        /// <returns>The estimated token count</returns>
        public static int EstimateTokenCount(IEnumerable<LlmMessage> messages)
        {
            int totalTokens = 0;

            foreach (var message in messages)
            {
                totalTokens += EstimateTokenCount(message);
            }

            // Add conversation overhead
            totalTokens += 3;

            return totalTokens;
        }

        /// <summary>
        /// Estimates the token count for a tool call
        /// </summary>
        /// <param name="toolCall">The tool call to estimate tokens for</param>
        /// <returns>The estimated token count</returns>
        public static int EstimateToolCallTokens(LlmToolCall toolCall)
        {
            int nameTokens = EstimateContentTokens(toolCall.ToolName);
            int argsTokens = EstimateContentTokens(toolCall.Arguments);

            return nameTokens + argsTokens + ToolCallOverhead;
        }

        /// <summary>
        /// Estimates the token count for a tool response
        /// </summary>
        /// <param name="toolResponse">The tool response to estimate tokens for</param>
        /// <returns>The estimated token count</returns>
        public static int EstimateToolResponseTokens(string toolResponseContent)
        {
            int contentTokens = EstimateContentTokens(toolResponseContent);

            return contentTokens + ToolResponseOverhead;
        }

        /// <summary>
        /// Trims conversation history to fit within a token limit
        /// </summary>
        /// <param name="messages">The messages to trim</param>
        /// <param name="maxTokens">The maximum number of tokens</param>
        /// <param name="preserveSystemMessage">Whether to preserve the system message</param>
        /// <returns>The trimmed messages</returns>
        public static List<LlmMessage> TrimConversationToFitTokenLimit(
            IEnumerable<LlmMessage> messages,
            int maxTokens,
            bool preserveSystemMessage = true)
        {
            var messageList = messages.ToList();
            int totalTokens = EstimateTokenCount(messageList);

            // If we're already under the limit, return the original list
            if (totalTokens <= maxTokens)
            {
                return messageList;
            }

            // Create a new list for the trimmed messages
            var trimmedMessages = new List<LlmMessage>();

            // If preserving system message, add it first
            if (preserveSystemMessage)
            {
                var systemMessage = messageList.FirstOrDefault(m => m.Role == LlmRole.System);
                if (systemMessage != null)
                {
                    trimmedMessages.Add(systemMessage);
                    totalTokens = EstimateTokenCount(trimmedMessages);
                }
            }

            // Add the most recent messages until we hit the token limit
            for (int i = messageList.Count - 1; i >= 0; i--)
            {
                var message = messageList[i];

                // Skip system messages if we already added one
                if (preserveSystemMessage && message.Role == LlmRole.System)
                {
                    continue;
                }

                int messageTokens = EstimateTokenCount(message);

                // If adding this message would exceed the limit, stop
                if (totalTokens + messageTokens > maxTokens)
                {
                    break;
                }

                // Add the message to the beginning of the list (to maintain order)
                trimmedMessages.Insert(0, message);
                totalTokens += messageTokens;
            }

            return trimmedMessages;
        }

        /// <summary>
        /// Gets the overhead token count for a message role
        /// </summary>
        /// <param name="role">The message role</param>
        /// <returns>The overhead token count</returns>
        private static int GetMessageOverhead(LlmRole role)
        {
            return role switch
            {
                LlmRole.System => SystemMessageOverhead,
                LlmRole.User => UserMessageOverhead,
                LlmRole.Assistant => AssistantMessageOverhead,
                LlmRole.Tool => ToolResponseOverhead,
                _ => UserMessageOverhead
            };
        }

        /// <summary>
        /// Estimates the token count for message content
        /// </summary>
        /// <param name="content">The content to estimate tokens for</param>
        /// <returns>The estimated token count</returns>
        private static int EstimateContentTokens(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return 0;
            }

            // Check if the content contains Chinese or Japanese characters
            bool containsCJK = ChineseJapanesePattern.IsMatch(content);

            // Use the appropriate tokens per character ratio
            double tokensPerChar = containsCJK ? ChineseJapaneseTokensPerChar : EnglishTokensPerChar;

            // Calculate the estimated token count
            int estimatedTokens = (int)Math.Ceiling(content.Length * tokensPerChar);

            return estimatedTokens;
        }
    }
}
