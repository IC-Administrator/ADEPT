using System;

namespace Adept.Core.Models
{
    /// <summary>
    /// Represents a chunk of an LLM response during streaming
    /// </summary>
    public class LlmResponseChunk
    {
        /// <summary>
        /// The content of the chunk
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is the final chunk
        /// </summary>
        public bool IsFinal { get; set; }

        /// <summary>
        /// The timestamp when the chunk was received
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmResponseChunk"/> class
        /// </summary>
        public LlmResponseChunk()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmResponseChunk"/> class
        /// </summary>
        /// <param name="content">The content of the chunk</param>
        /// <param name="isFinal">Whether this is the final chunk</param>
        public LlmResponseChunk(string content, bool isFinal = false)
        {
            Content = content;
            IsFinal = isFinal;
        }
    }
}
