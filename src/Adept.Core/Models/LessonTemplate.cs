using Adept.Common.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Adept.Core.Models
{
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
        public LessonPlan.LessonComponents Components
        {
            get
            {
                if (string.IsNullOrEmpty(ComponentsJson) || ComponentsJson == "{}")
                {
                    return new LessonPlan.LessonComponents();
                }

                if (ComponentsJson.TryFromJson<LessonPlan.LessonComponents>(out var components) && components != null)
                {
                    return components;
                }

                return new LessonPlan.LessonComponents();
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
