using System.Collections.Generic;

namespace Adept.Data.Database
{
    /// <summary>
    /// Contains database migration scripts
    /// </summary>
    public static class DatabaseMigrations
    {
        /// <summary>
        /// Gets the database migrations
        /// </summary>
        public static Dictionary<long, string> Migrations => new Dictionary<long, string>
        {
            // Add LessonResources and LessonTemplates tables
            {
                3,
                @"
                -- LessonResources table for storing resources attached to lessons
                CREATE TABLE IF NOT EXISTS LessonResources (
                    ResourceId TEXT PRIMARY KEY,
                    LessonId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Type INTEGER NOT NULL,
                    Path TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (LessonId) REFERENCES LessonPlans (LessonId) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_resources_lesson ON LessonResources(LessonId);

                -- LessonTemplates table for storing lesson templates
                CREATE TABLE IF NOT EXISTS LessonTemplates (
                    TemplateId TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Category TEXT,
                    Tags TEXT,
                    Title TEXT,
                    LearningObjectives TEXT,
                    ComponentsJson TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_templates_category ON LessonTemplates(Category);
                "
            }
        };
    }
}
