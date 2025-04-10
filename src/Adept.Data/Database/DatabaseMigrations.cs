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
            // Initial schema creation
            {
                1,
                @"
                -- Configuration table for application settings
                CREATE TABLE IF NOT EXISTS Configuration (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL,
                    encrypted INTEGER NOT NULL DEFAULT 0,
                    last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                -- Secure storage for API keys and sensitive data
                CREATE TABLE IF NOT EXISTS SecureStorage (
                    key TEXT PRIMARY KEY,
                    encrypted_value BLOB NOT NULL,
                    iv BLOB NOT NULL,
                    last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                -- Classes table for storing class information
                CREATE TABLE IF NOT EXISTS Classes (
                    class_id TEXT PRIMARY KEY,
                    class_code TEXT NOT NULL,
                    education_level TEXT,
                    current_topic TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS idx_classes_code ON Classes(class_code);

                -- Students table for storing student information
                CREATE TABLE IF NOT EXISTS Students (
                    student_id TEXT PRIMARY KEY,
                    class_id TEXT NOT NULL,
                    name TEXT NOT NULL,
                    fsm_status INTEGER,
                    sen_status INTEGER,
                    eal_status INTEGER,
                    ability_level INTEGER,
                    reading_age TEXT,
                    target_grade TEXT,
                    notes TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    FOREIGN KEY (class_id) REFERENCES Classes (class_id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_students_class ON Students(class_id);

                -- Lesson plans table for storing lesson information
                CREATE TABLE IF NOT EXISTS LessonPlans (
                    lesson_id TEXT PRIMARY KEY,
                    class_id TEXT NOT NULL,
                    date TEXT NOT NULL,
                    time_slot INTEGER NOT NULL,
                    title TEXT NOT NULL,
                    learning_objectives TEXT,
                    components_json TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    FOREIGN KEY (class_id) REFERENCES Classes (class_id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_lessons_class ON LessonPlans(class_id);
                CREATE INDEX IF NOT EXISTS idx_lessons_date ON LessonPlans(date);
                "
            },

            // Add Conversations and SystemPrompts tables
            {
                2,
                @"
                -- Conversations table for storing conversation history
                CREATE TABLE IF NOT EXISTS Conversations (
                    conversation_id TEXT PRIMARY KEY,
                    title TEXT NOT NULL,
                    messages_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );

                -- System prompts table for storing LLM system prompts
                CREATE TABLE IF NOT EXISTS SystemPrompts (
                    prompt_id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    content TEXT NOT NULL,
                    is_default INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_prompts_default ON SystemPrompts(is_default);
                "
            },

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
                    FOREIGN KEY (LessonId) REFERENCES LessonPlans (lesson_id) ON DELETE CASCADE
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
            },

            // Add CalendarEventId and SyncStatus to LessonPlans
            {
                4,
                @"
                -- Add calendar integration fields to LessonPlans
                ALTER TABLE LessonPlans ADD COLUMN calendar_event_id TEXT;
                ALTER TABLE LessonPlans ADD COLUMN sync_status INTEGER DEFAULT 0;
                CREATE INDEX IF NOT EXISTS idx_lessons_calendar ON LessonPlans(calendar_event_id);
                "
            }
        };
    }
}
