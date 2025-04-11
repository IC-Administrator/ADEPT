using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Repository for lesson resources
    /// </summary>
    public class LessonResourceRepository : BaseRepository<LessonResource>, ILessonResourceRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LessonResourceRepository"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        public LessonResourceRepository(IDatabaseContext databaseContext, ILogger<LessonResourceRepository> logger)
            : base(databaseContext, logger)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LessonResource>> GetResourcesByLessonIdAsync(Guid lessonId)
        {
            if (lessonId == Guid.Empty)
            {
                throw new ArgumentException("Lesson ID cannot be empty", nameof(lessonId));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QueryAsync<LessonResource>(
                    @"SELECT
                        resource_id AS ResourceId,
                        lesson_id AS LessonId,
                        name AS Name,
                        type AS Type,
                        path AS Path,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM LessonResources
                      WHERE lesson_id = @LessonId
                      ORDER BY name",
                    new { LessonId = lessonId.ToString() }),
                $"Error retrieving resources for lesson {lessonId}",
                Enumerable.Empty<LessonResource>());
        }

        /// <inheritdoc/>
        public async Task<LessonResource> GetResourceByIdAsync(Guid resourceId)
        {
            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource ID cannot be empty", nameof(resourceId));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () => await DatabaseContext.QuerySingleOrDefaultAsync<LessonResource>(
                    @"SELECT
                        resource_id AS ResourceId,
                        lesson_id AS LessonId,
                        name AS Name,
                        type AS Type,
                        path AS Path,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                      FROM LessonResources
                      WHERE resource_id = @ResourceId",
                    new { ResourceId = resourceId.ToString() }),
                $"Error retrieving resource {resourceId}");
        }

        /// <inheritdoc/>
        public async Task<LessonResource> AddResourceAsync(LessonResource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource), "The resource cannot be null");
            }

            if (resource.ResourceId == Guid.Empty)
            {
                resource.ResourceId = Guid.NewGuid();
            }

            if (resource.LessonId == Guid.Empty)
            {
                throw new ArgumentException("Lesson ID cannot be empty", nameof(resource));
            }

            ValidateStringNotNullOrEmpty(resource.Name, "Name");
            ValidateStringNotNullOrEmpty(resource.Path, "Path");

            return await ExecuteWithErrorHandlingAndThrowAsync(
                async () =>
                {
                    resource.CreatedAt = DateTime.UtcNow;
                    resource.UpdatedAt = DateTime.UtcNow;

                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"INSERT INTO LessonResources (
                            resource_id,
                            lesson_id,
                            name,
                            type,
                            path,
                            created_at,
                            updated_at
                          ) VALUES (
                            @ResourceId,
                            @LessonId,
                            @Name,
                            @Type,
                            @Path,
                            @CreatedAt,
                            @UpdatedAt
                          )",
                        new
                        {
                            ResourceId = resource.ResourceId.ToString(),
                            LessonId = resource.LessonId.ToString(),
                            Name = resource.Name,
                            Type = (int)resource.Type,
                            Path = resource.Path,
                            CreatedAt = resource.CreatedAt.ToString("o"),
                            UpdatedAt = resource.UpdatedAt.ToString("o")
                        });

                    Logger.LogInformation("Added resource {ResourceId} for lesson {LessonId}", resource.ResourceId, resource.LessonId);
                    return resource;
                },
                $"Error adding resource for lesson {resource.LessonId}");
        }

        /// <inheritdoc/>
        public async Task<LessonResource> UpdateResourceAsync(LessonResource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource), "The resource cannot be null");
            }

            if (resource.ResourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource ID cannot be empty", nameof(resource));
            }

            ValidateStringNotNullOrEmpty(resource.Name, "Name");
            ValidateStringNotNullOrEmpty(resource.Path, "Path");

            return await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Check if resource exists
                    var existingResource = await GetResourceByIdAsync(resource.ResourceId);
                    if (existingResource == null)
                    {
                        Logger.LogWarning("Resource with ID {ResourceId} not found", resource.ResourceId);
                        return null;
                    }

                    resource.CreatedAt = existingResource.CreatedAt; // Preserve original creation date
                    resource.UpdatedAt = DateTime.UtcNow;

                    await DatabaseContext.ExecuteNonQueryAsync(
                        @"UPDATE LessonResources
                          SET
                            name = @Name,
                            type = @Type,
                            path = @Path,
                            updated_at = @UpdatedAt
                          WHERE resource_id = @ResourceId",
                        new
                        {
                            ResourceId = resource.ResourceId.ToString(),
                            Name = resource.Name,
                            Type = (int)resource.Type,
                            Path = resource.Path,
                            UpdatedAt = resource.UpdatedAt.ToString("o")
                        });

                    Logger.LogInformation("Updated resource {ResourceId}", resource.ResourceId);
                    return resource;
                },
                $"Error updating resource {resource.ResourceId}");
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteResourceAsync(Guid resourceId)
        {
            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource ID cannot be empty", nameof(resourceId));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Check if resource exists
                    var existingResource = await GetResourceByIdAsync(resourceId);
                    if (existingResource == null)
                    {
                        Logger.LogWarning("Resource {ResourceId} not found for deletion", resourceId);
                        return false;
                    }

                    int rowsAffected = await DatabaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM LessonResources WHERE resource_id = @ResourceId",
                        new { ResourceId = resourceId.ToString() });

                    if (rowsAffected > 0)
                    {
                        Logger.LogInformation("Deleted resource {ResourceId}", resourceId);
                        return true;
                    }

                    return false;
                },
                $"Error deleting resource {resourceId}",
                false);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteResourcesByLessonIdAsync(Guid lessonId)
        {
            if (lessonId == Guid.Empty)
            {
                throw new ArgumentException("Lesson ID cannot be empty", nameof(lessonId));
            }

            return await ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    int rowsAffected = await DatabaseContext.ExecuteNonQueryAsync(
                        "DELETE FROM LessonResources WHERE lesson_id = @LessonId",
                        new { LessonId = lessonId.ToString() });

                    Logger.LogInformation("Deleted {Count} resources for lesson {LessonId}", rowsAffected, lessonId);
                    return rowsAffected > 0;
                },
                $"Error deleting resources for lesson {lessonId}",
                false);
        }
    }
}
