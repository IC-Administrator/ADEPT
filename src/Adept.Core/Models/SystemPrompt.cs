using Adept.Common.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Adept.Core.Models
{
    /// <summary>
    /// System prompt for LLM interactions
    /// </summary>
    public class SystemPrompt
    {
        /// <summary>
        /// The ID of the prompt
        /// </summary>
        public string PromptId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The name of the prompt
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The content of the prompt
        /// </summary>
        [Required]
        [ValidJson]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is the default prompt
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// When the prompt was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the prompt was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Validates the prompt content
        /// </summary>
        /// <returns>True if the content is valid, false otherwise</returns>
        public bool ValidateContent()
        {
            return !string.IsNullOrWhiteSpace(Content) && Content.IsValidJson();
        }

        /// <summary>
        /// Creates a deep clone of the prompt
        /// </summary>
        /// <returns>A new instance with the same values</returns>
        public SystemPrompt Clone()
        {
            return this.DeepClone() ?? new SystemPrompt
            {
                PromptId = this.PromptId,
                Name = this.Name,
                Content = this.Content,
                IsDefault = this.IsDefault,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt
            };
        }
    }
}
