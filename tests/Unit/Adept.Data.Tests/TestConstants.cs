using System;

namespace Adept.Data.Tests
{
    /// <summary>
    /// Constants used in data tests
    /// </summary>
    public static class TestConstants
    {
        /// <summary>
        /// Sample database paths for testing
        /// </summary>
        public static class DatabasePaths
        {
            public static string TestDatabasePath => System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), 
                "adept_test.db");
            
            public static string TestBackupDirectory => System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), 
                "adept_test_backups");
        }

        /// <summary>
        /// Sample entity IDs for testing
        /// </summary>
        public static class EntityIds
        {
            public static string ConversationId => Guid.NewGuid().ToString();
            public static string MessageId => Guid.NewGuid().ToString();
            public static string ClassId => Guid.NewGuid().ToString();
            public static string StudentId => Guid.NewGuid().ToString();
            public static string LessonId => Guid.NewGuid().ToString();
            public static string AssignmentId => Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Sample entity data for testing
        /// </summary>
        public static class EntityData
        {
            public const string ClassCode = "CS101";
            public const string ClassName = "Introduction to Computer Science";
            public const string StudentName = "John Doe";
            public const string StudentEmail = "john.doe@example.com";
            public const string LessonTitle = "Variables and Data Types";
            public const string AssignmentTitle = "Programming Exercise 1";
            public const string SystemPrompt = "You are a helpful assistant.";
            public const string UserMessage = "Hello, world!";
            public const string AssistantMessage = "Hello! How can I help you today?";
        }

        /// <summary>
        /// Sample database queries for testing
        /// </summary>
        public static class DatabaseQueries
        {
            public const string IntegrityCheck = "PRAGMA integrity_check";
            public const string ForeignKeyCheck = "PRAGMA foreign_key_check";
            public const string Vacuum = "VACUUM";
            public const string Analyze = "ANALYZE";
            public const string Checkpoint = "PRAGMA wal_checkpoint(FULL)";
        }

        /// <summary>
        /// Sample backup names for testing
        /// </summary>
        public static class BackupNames
        {
            public const string ManualBackup = "manual_backup";
            public const string AutoBackup = "auto_backup";
            public const string MigrationBackup = "pre_migration";
            public const string TestBackup = "test_backup";
        }
    }
}
