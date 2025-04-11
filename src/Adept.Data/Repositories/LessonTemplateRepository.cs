using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for lesson templates
    /// </summary>
    public class LessonTemplateRepository : BaseRepository<LessonTemplate>, ILessonTemplateRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LessonTemplateRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public LessonTemplateRepository(IDatabaseContext databaseContext, ILogger<LessonTemplateRepository> logger)
            : base(databaseContext, logger)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonTemplate>> GetAllTemplatesAsync()
        {
            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<LessonTemplate>(
                    @"SELECT
                        template_id AS TemplateId,
                        name AS Name,
                        description AS Description,
                        category AS Category,
                        tags AS Tags,
                        title AS Title,
                        learning_objectives AS LearningObjectives,
                        components_json AS ComponentsJson,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM LessonTemplates
                      ORDER BY name"),
                "Error retrieving templates",
                Enumerable.Empty<LessonTemplate>());
        }

        /// <inheritdoc/>
        public async Task<LessonTemplate> GetTemplateByIdAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
            {
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<LessonTemplate>(
                    @"SELECT
                        template_id AS TemplateId,
                        name AS Name,
                        description AS Description,
                        category AS Category,
                        tags AS Tags,
                        title AS Title,
                        learning_objectives AS LearningObjectives,
                        components_json AS ComponentsJson,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM LessonTemplates
                      WHERE template_id = @TemplateId",
                    new { TemplateId = templateId.ToString() }),
                $"Error retrieving template {templateId}");
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonTemplate>> GetTemplatesByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("The category cannot be null or empty", nameof(category));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<LessonTemplate>(
                    @"SELECT
                        template_id AS TemplateId,
                        name AS Name,
                        description AS Description,
                        category AS Category,
                        tags AS Tags,
                        title AS Title,
                        learning_objectives AS LearningObjectives,
                        components_json AS ComponentsJson,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM LessonTemplates
                      WHERE category = @Category
                      ORDER BY name",
                    new { Category = category }),
                $"Error retrieving templates for category {category}",
                Enumerable.Empty<LessonTemplate>());
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonTemplate>> GetTemplatesByTagAsync(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException("The tag cannot be null or empty", nameof(tag));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<LessonTemplate>(
                    @"SELECT
                        template_id AS TemplateId,
                        name AS Name,
                        description AS Description,
                        category AS Category,
                        tags AS Tags,
                        title AS Title,
                        learning_objectives AS LearningObjectives,
                        components_json AS ComponentsJson,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM LessonTemplates
                      WHERE tags LIKE @Tag
                      ORDER BY name",
                    new { Tag = $"%\"{tag}\"%" }),
                $"Error retrieving templates for tag {tag}",
                Enumerable.Empty<LessonTemplate>());
        }

        /// <inheritdoc/>
        public async Task<LessonTemplate> AddTemplateAsync(LessonTemplate template)
        {
            ValidateTemplate(template);

            return await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Set created/updated timestamps
                    template.CreatedAt = DateTime.UtcNow;
                    template.UpdatedAt = template.CreatedAt;

                    // Generate a new ID if not provided
                    if (template.TemplateId == Guid.Empty)
                    {
                        template.TemplateId = Guid.NewGuid();
                    }

                    // Insert the template
                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"INSERT INTO LessonTemplates (
                            template_id, name, description, category, tags,
                            title, learning_objectives, components_json, created_at, updated_at)
                          VALUES (
                            @TemplateId, @Name, @Description, @Category, @Tags,
                            @Title, @LearningObjectives, @ComponentsJson, @CreatedAt, @UpdatedAt)",
                        new
                        {
                            TemplateId = template.TemplateId.ToString(),
                            Name = template.Name,
                            Description = template.Description,
                            Category = template.Category,
                            Tags = JsonSerializer.Serialize(template.Tags),
                            Title = template.Title,
                            LearningObjectives = template.LearningObjectives,
                            ComponentsJson = template.ComponentsJson,
                            CreatedAt = template.CreatedAt.ToString("o"),
                            UpdatedAt = template.UpdatedAt.ToString("o")
                        });

                    // Return the newly created template
                    Logger.LogInformation("Added template {TemplateId}", template.TemplateId);
                    return template;
                },
                $"Error adding template {template.Name}",
                null);
        }

        /// <inheritdoc/>
        public async Task<LessonTemplate> UpdateTemplateAsync(LessonTemplate template)
        {
            ValidateTemplate(template);

            return await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Update timestamp
                    template.UpdatedAt = DateTime.UtcNow;

                    // Update the template
                    int rowsAffected = await DatabaseContext.ExecuteNonQueryAsync(
                        @"UPDATE LessonTemplates
                          SET name = @Name,
                              description = @Description,
                              category = @Category,
                              tags = @Tags,
                              title = @Title,
                              learning_objectives = @LearningObjectives,
                              components_json = @ComponentsJson,
                              updated_at = @UpdatedAt
                          WHERE template_id = @TemplateId",
                        new
                        {
                            TemplateId = template.TemplateId.ToString(),
                            Name = template.Name,
                            Description = template.Description,
                            Category = template.Category,
                            Tags = JsonSerializer.Serialize(template.Tags),
                            Title = template.Title,
                            LearningObjectives = template.LearningObjectives,
                            ComponentsJson = template.ComponentsJson,
                            UpdatedAt = template.UpdatedAt.ToString("o")
                        });

                    // Check if the template exists before updating
                    var existingTemplate = await GetTemplateByIdAsync(template.TemplateId);
                    if (existingTemplate == null)
                    {
                        Logger.LogWarning("Template {TemplateId} not found for update", template.TemplateId);
                        return null;
                    }

                    Logger.LogInformation("Updated template {TemplateId}", template.TemplateId);
                    return template;
                },
                $"Error updating template {template.Name}",
                null);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
            {
                throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Check if the template exists before deleting
                    var existingTemplate = await GetTemplateByIdAsync(templateId);
                    if (existingTemplate == null)
                    {
                        Logger.LogWarning("Template {TemplateId} not found for deletion", templateId);
                        return false;
                    }

                    await DatabaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM LessonTemplates WHERE template_id = @TemplateId",
                        new { TemplateId = templateId.ToString() });

                    Logger.LogInformation("Deleted template {TemplateId}", templateId);
                    return true;
                },
                $"Error deleting template {templateId}",
                false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonTemplate>> SearchTemplatesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllTemplatesAsync();
            }

            return await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var templates = await DatabaseContext.QueryAsync<LessonTemplate>(
                        @"SELECT
                            template_id AS TemplateId,
                            name AS Name,
                            description AS Description,
                            category AS Category,
                            tags AS Tags,
                            title AS Title,
                            learning_objectives AS LearningObjectives,
                            components_json AS ComponentsJson,
                            created_at AS CreatedAt,
                            updated_at AS UpdatedAt
                          FROM LessonTemplates
                          WHERE name LIKE @SearchTerm
                             OR description LIKE @SearchTerm
                             OR title LIKE @SearchTerm
                             OR learning_objectives LIKE @SearchTerm
                          ORDER BY name",
                        new { SearchTerm = $"%{searchTerm}%" });

                    Logger.LogInformation("Retrieved {Count} templates for search term {SearchTerm}", templates.Count(), searchTerm);
                    return templates;
                },
                $"Error searching templates for term {searchTerm}",
                Enumerable.Empty<LessonTemplate>());
        }

        /// <summary>
        /// Validates a template
        /// </summary>
        /// <param name="template">The template to validate</param>
        private void ValidateTemplate(LessonTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            ValidateStringNotNullOrEmpty(template.Name, nameof(template.Name));
            ValidateStringNotNullOrEmpty(template.Title, nameof(template.Title));
            ValidateStringNotNullOrEmpty(template.LearningObjectives, nameof(template.LearningObjectives));
        }
    }
}
