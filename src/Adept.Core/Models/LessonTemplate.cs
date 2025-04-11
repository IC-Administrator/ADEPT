using Adept.Common.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Adept.Core.Models
{
    /// <summary>
    /// Represents the components of a lesson
    /// </summary>
    public class LessonComponents
    {
        /// <summary>
        /// Introduction to the lesson
        /// </summary>
        public string? Introduction { get; set; }

        /// <summary>
        /// Main content of the lesson
        /// </summary>
        public string? MainContent { get; set; }

        /// <summary>
        /// Activities for the lesson
        /// </summary>
        public List<string> Activities { get; set; } = new List<string>();

        /// <summary>
        /// Assessment for the lesson
        /// </summary>
        public string? Assessment { get; set; }

        /// <summary>
        /// Homework for the lesson
        /// </summary>
        public string? Homework { get; set; }

        /// <summary>
        /// Resources for the lesson
        /// </summary>
        public List<string> Resources { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a lesson template
    /// </summary>
    public class LessonTemplate
    {
        /// <summary>
        /// Gets or sets the template ID
        /// </summary>
        public Guid TemplateId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the template name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the template description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the template category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the template tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the template title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the template learning objectives
        /// </summary>
        public string LearningObjectives { get; set; }

        /// <summary>
        /// Gets or sets the template components JSON
        /// </summary>
        public string ComponentsJson { get; set; } = "{}";

        /// <summary>
        /// Gets or sets the template components
        /// </summary>
        [JsonIgnore]
        public LessonComponents Components
        {
            get
            {
                if (string.IsNullOrEmpty(ComponentsJson) || ComponentsJson == "{}")
                {
                    return new LessonComponents();
                }

                if (ComponentsJson.TryFromJson<LessonComponents>(out var components) && components != null)
                {
                    return components;
                }

                return new LessonComponents();
            }
            set
            {
                ComponentsJson = value.ToJson();
            }
        }

        /// <summary>
        /// Gets or sets the date the template was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date the template was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
