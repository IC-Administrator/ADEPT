using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Data.Database;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for lesson resources
    /// </summary>
    public class LessonResourceRepository : ILessonResourceRepository
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly ILogger<LessonResourceRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LessonResourceRepository"/> class
        /// </summary>
        /// <param name="databaseProvider">The database provider</param>
        /// <param name="logger">The logger</param>
        public LessonResourceRepository(IDatabaseProvider databaseProvider, ILogger<LessonResourceRepository> logger)
        {
            _databaseProvider = databaseProvider;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonResource>> GetResourcesByLessonIdAsync(Guid lessonId)
        {
            var resources = new List<LessonResource>();

            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        SELECT ResourceId, LessonId, Name, Type, Path, CreatedAt, UpdatedAt
                        FROM LessonResources
                        WHERE LessonId = @LessonId
                        ORDER BY Name";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@LessonId", lessonId.ToString());

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                resources.Add(MapResourceFromReader(reader));
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} resources for lesson {LessonId}", resources.Count, lessonId);
                return resources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resources for lesson {LessonId}", lessonId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LessonResource> GetResourceByIdAsync(Guid resourceId)
        {
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        SELECT ResourceId, LessonId, Name, Type, Path, CreatedAt, UpdatedAt
                        FROM LessonResources
                        WHERE ResourceId = @ResourceId";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ResourceId", resourceId.ToString());

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var resource = MapResourceFromReader(reader);
                                _logger.LogInformation("Retrieved resource {ResourceId}", resourceId);
                                return resource;
                            }
                        }
                    }
                }

                _logger.LogWarning("Resource {ResourceId} not found", resourceId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resource {ResourceId}", resourceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LessonResource> AddResourceAsync(LessonResource resource)
        {
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        INSERT INTO LessonResources (ResourceId, LessonId, Name, Type, Path, CreatedAt, UpdatedAt)
                        VALUES (@ResourceId, @LessonId, @Name, @Type, @Path, @CreatedAt, @UpdatedAt)";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ResourceId", resource.ResourceId.ToString());
                        command.Parameters.AddWithValue("@LessonId", resource.LessonId.ToString());
                        command.Parameters.AddWithValue("@Name", resource.Name);
                        command.Parameters.AddWithValue("@Type", (int)resource.Type);
                        command.Parameters.AddWithValue("@Path", resource.Path);
                        command.Parameters.AddWithValue("@CreatedAt", resource.CreatedAt.ToString("o"));
                        command.Parameters.AddWithValue("@UpdatedAt", resource.UpdatedAt.ToString("o"));

                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Added resource {ResourceId} for lesson {LessonId}", resource.ResourceId, resource.LessonId);
                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding resource for lesson {LessonId}", resource.LessonId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LessonResource> UpdateResourceAsync(LessonResource resource)
        {
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        UPDATE LessonResources
                        SET Name = @Name, Type = @Type, Path = @Path, UpdatedAt = @UpdatedAt
                        WHERE ResourceId = @ResourceId";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ResourceId", resource.ResourceId.ToString());
                        command.Parameters.AddWithValue("@Name", resource.Name);
                        command.Parameters.AddWithValue("@Type", (int)resource.Type);
                        command.Parameters.AddWithValue("@Path", resource.Path);
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            _logger.LogWarning("Resource {ResourceId} not found for update", resource.ResourceId);
                            return null;
                        }
                    }
                }

                _logger.LogInformation("Updated resource {ResourceId}", resource.ResourceId);
                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating resource {ResourceId}", resource.ResourceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteResourceAsync(Guid resourceId)
        {
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = "DELETE FROM LessonResources WHERE ResourceId = @ResourceId";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ResourceId", resourceId.ToString());

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            _logger.LogWarning("Resource {ResourceId} not found for deletion", resourceId);
                            return false;
                        }
                    }
                }

                _logger.LogInformation("Deleted resource {ResourceId}", resourceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resource {ResourceId}", resourceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteResourcesByLessonIdAsync(Guid lessonId)
        {
            try
            {
                using (var connection = _databaseProvider.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = "DELETE FROM LessonResources WHERE LessonId = @LessonId";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@LessonId", lessonId.ToString());

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("Deleted {Count} resources for lesson {LessonId}", rowsAffected, lessonId);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resources for lesson {LessonId}", lessonId);
                throw;
            }
        }

        /// <summary>
        /// Maps a resource from a data reader
        /// </summary>
        /// <param name="reader">The data reader</param>
        /// <returns>The mapped resource</returns>
        private LessonResource MapResourceFromReader(IDataReader reader)
        {
            return new LessonResource
            {
                ResourceId = Guid.Parse(reader["ResourceId"].ToString()),
                LessonId = Guid.Parse(reader["LessonId"].ToString()),
                Name = reader["Name"].ToString(),
                Type = (ResourceType)Convert.ToInt32(reader["Type"]),
                Path = reader["Path"].ToString(),
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
