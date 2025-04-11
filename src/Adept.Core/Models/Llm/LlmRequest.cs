namespace Adept.Core.Models.Llm
{
    /// <summary>
    /// Request for LLM completion (legacy)
    /// </summary>
    public class LlmRequest
    {
        /// <summary>
        /// The prompt to send to the LLM
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Optional system prompt to use
        /// </summary>
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// Temperature for sampling (0.0 to 1.0)
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Top-p sampling parameter (0.0 to 1.0)
        /// </summary>
        public float TopP { get; set; } = 1.0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmRequest"/> class
        /// </summary>
        public LlmRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmRequest"/> class
        /// </summary>
        /// <param name="prompt">The prompt to send to the LLM</param>
        /// <param name="systemPrompt">Optional system prompt to use</param>
        public LlmRequest(string prompt, string? systemPrompt = null)
        {
            Prompt = prompt;
            SystemPrompt = systemPrompt;
        }
    }
}
