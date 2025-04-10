using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
            ValidateStringNotNullOrEmpty(category, nameof(category));

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
            ValidateStringNotNullOrEmpty(tag, nameof(tag));

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
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        INSERT INTO LessonTemplates (TemplateId, Name, Description, Category, Tags, Title, LearningObjectives, ComponentsJson, CreatedAt, UpdatedAt)
                        VALUES (@TemplateId, @Name, @Description, @Category, @Tags, @Title, @LearningObjectives, @ComponentsJson, @CreatedAt, @UpdatedAt)";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                    {
                        command.Parameters.AddWithValue("@TemplateId", template.TemplateId.ToString());
                        command.Parameters.AddWithValue("@Name", template.Name);
                        command.Parameters.AddWithValue("@Description", template.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Category", template.Category ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(template.Tags));
                        command.Parameters.AddWithValue("@Title", template.Title ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LearningObjectives", template.LearningObjectives ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ComponentsJson", template.ComponentsJson ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedAt", template.CreatedAt.ToString("o"));
                        command.Parameters.AddWithValue("@UpdatedAt", template.UpdatedAt.ToString("o"));

                        await command.ExecuteNonQueryAsync();
                    }
                    }
                }

                _logger.LogInformation("Added template {TemplateId}", template.TemplateId);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding template");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LessonTemplate> UpdateTemplateAsync(LessonTemplate template)
        {
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        UPDATE LessonTemplates
                        SET Name = @Name, Description = @Description, Category = @Category, Tags = @Tags,
                            Title = @Title, LearningObjectives = @LearningObjectives, ComponentsJson = @ComponentsJson, UpdatedAt = @UpdatedAt
                        WHERE TemplateId = @TemplateId";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                    {
                        command.Parameters.AddWithValue("@TemplateId", template.TemplateId.ToString());
                        command.Parameters.AddWithValue("@Name", template.Name);
                        command.Parameters.AddWithValue("@Description", template.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Category", template.Category ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(template.Tags));
                        command.Parameters.AddWithValue("@Title", template.Title ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LearningObjectives", template.LearningObjectives ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ComponentsJson", template.ComponentsJson ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            _logger.LogWarning("Template {TemplateId} not found for update", template.TemplateId);
                            return null;
                        }
                    }
                    }
                }

                _logger.LogInformation("Updated template {TemplateId}", template.TemplateId);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", template.TemplateId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = "DELETE FROM LessonTemplates WHERE TemplateId = @TemplateId";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                    {
                        command.Parameters.AddWithValue("@TemplateId", templateId.ToString());

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            _logger.LogWarning("Template {TemplateId} not found for deletion", templateId);
                            return false;
                        }
                    }
                    }
                }

                _logger.LogInformation("Deleted template {TemplateId}", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonTemplate>> SearchTemplatesAsync(string searchTerm)
        {
            var templates = new List<LessonTemplate>();

            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        SELECT TemplateId, Name, Description, Category, Tags, Title, LearningObjectives, ComponentsJson, CreatedAt, UpdatedAt
                        FROM LessonTemplates
                        WHERE Name LIKE @SearchTerm OR Description LIKE @SearchTerm OR Title LIKE @SearchTerm OR LearningObjectives LIKE @SearchTerm
                        ORDER BY Name";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                    {
                        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                templates.Add(MapTemplateFromReader(reader));
                            }
                        }
                    }
                    }
                }

                _logger.LogInformation("Retrieved {Count} templates for search term {SearchTerm}", templates.Count, searchTerm);
                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching templates for term {SearchTerm}", searchTerm);
                throw;
            }
        }

        /// <summary>
        /// Maps a template from a data reader
        /// </summary>
        /// <param name="reader">The data reader</param>
        /// <returns>The mapped template</returns>
        private LessonTemplate MapTemplateFromReader(IDataReader reader)
        {
            var tags = reader["Tags"] != DBNull.Value
                ? JsonSerializer.Deserialize<List<string>>(reader["Tags"].ToString())
                : new List<string>();

            return new LessonTemplate
            {
                TemplateId = Guid.Parse(reader["TemplateId"].ToString()),
                Name = reader["Name"].ToString(),
                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null,
                Category = reader["Category"] != DBNull.Value ? reader["Category"].ToString() : null,
                Tags = tags,
                Title = reader["Title"] != DBNull.Value ? reader["Title"].ToString() : null,
                LearningObjectives = reader["LearningObjectives"] != DBNull.Value ? reader["LearningObjectives"].ToString() : null,
                ComponentsJson = reader["ComponentsJson"] != DBNull.Value ? reader["ComponentsJson"].ToString() : null,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
