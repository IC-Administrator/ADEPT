using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Data.Database;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text.Json;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for lesson templates
    /// </summary>
    public class LessonTemplateRepository : ILessonTemplateRepository
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly ILogger<LessonTemplateRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LessonTemplateRepository"/> class
        /// </summary>
        /// <param name="databaseProvider">The database provider</param>
        /// <param name="logger">The logger</param>
        public LessonTemplateRepository(IDatabaseProvider databaseProvider, ILogger<LessonTemplateRepository> logger)
        {
            _databaseProvider = databaseProvider;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonTemplate>> GetAllTemplatesAsync()
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
                        ORDER BY Name";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                templates.Add(MapTemplateFromReader(reader));
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} templates", templates.Count);
                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LessonTemplate> GetTemplateByIdAsync(Guid templateId)
        {
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        SELECT TemplateId, Name, Description, Category, Tags, Title, LearningObjectives, ComponentsJson, CreatedAt, UpdatedAt
                        FROM LessonTemplates
                        WHERE TemplateId = @TemplateId";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@TemplateId", templateId.ToString());

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var template = MapTemplateFromReader(reader);
                                _logger.LogInformation("Retrieved template {TemplateId}", templateId);
                                return template;
                            }
                        }
                    }
                }

                _logger.LogWarning("Template {TemplateId} not found", templateId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template {TemplateId}", templateId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonTemplate>> GetTemplatesByCategoryAsync(string category)
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
                        WHERE Category = @Category
                        ORDER BY Name";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Category", category);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                templates.Add(MapTemplateFromReader(reader));
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} templates for category {Category}", templates.Count, category);
                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates for category {Category}", category);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonTemplate>> GetTemplatesByTagAsync(string tag)
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
                        WHERE Tags LIKE @Tag
                        ORDER BY Name";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Tag", $"%\"{tag}\"%");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                templates.Add(MapTemplateFromReader(reader));
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} templates for tag {Tag}", templates.Count, tag);
                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates for tag {Tag}", tag);
                throw;
            }
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

                    using (var command = new SQLiteCommand(sql, connection))
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

                    using (var command = new SQLiteCommand(sql, connection))
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

                    using (var command = new SQLiteCommand(sql, connection))
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

                    using (var command = new SQLiteCommand(sql, connection))
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
