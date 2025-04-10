using Adept.Common.Interfaces;
using Adept.Data.Database;
using Adept.TestUtilities.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Database.Tests.Fixtures
{
    /// <summary>
    /// Fixture for SQLite database integration tests
    /// </summary>
    public class SqliteDatabaseFixture : IAsyncLifetime, IDisposable
    {
        public string DatabasePath { get; }
        public IServiceProvider ServiceProvider { get; }
        public IDatabaseContext DatabaseContext => ServiceProvider.GetRequiredService<IDatabaseContext>();
        public DatabaseBackupService BackupService => ServiceProvider.GetRequiredService<DatabaseBackupService>();
        public DatabaseIntegrityService IntegrityService => ServiceProvider.GetRequiredService<DatabaseIntegrityService>();

        public SqliteDatabaseFixture()
        {
            // Create a unique test database path
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"adept_test_{Guid.NewGuid():N}.db");

            // Set up services
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add configuration
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Database:ConnectionString", $"Data Source={DatabasePath}" }
                })
                .Build();
            services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configuration);

            // Add database services
            services.AddSingleton<IDatabaseContext>(provider =>
                new SqliteDatabaseContext(
                    provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                    provider.GetRequiredService<ILogger<SqliteDatabaseContext>>()));

            // Add backup service
            services.AddSingleton<DatabaseBackupService>();

            // Add integrity service
            services.AddSingleton<DatabaseIntegrityService>();

            ServiceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Initialize the database
        /// </summary>
        public async Task InitializeAsync()
        {
            // Create the database schema
            await CreateDatabaseSchemaAsync();

            // Insert test data
            await InsertTestDataAsync();
        }

        /// <summary>
        /// Create the database schema
        /// </summary>
        private async Task CreateDatabaseSchemaAsync()
        {
            // Create tables
            await DatabaseContext.ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS Conversations (
                    ConversationId TEXT PRIMARY KEY,
                    Title TEXT NOT NULL,
                    SystemPrompt TEXT,
                    CreatedDate TEXT NOT NULL,
                    LastUpdatedDate TEXT
                );

                CREATE TABLE IF NOT EXISTS Messages (
                    MessageId TEXT PRIMARY KEY,
                    ConversationId TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    FOREIGN KEY (ConversationId) REFERENCES Conversations(ConversationId)
                );

                CREATE TABLE IF NOT EXISTS Classes (
                    ClassId TEXT PRIMARY KEY,
                    ClassCode TEXT NOT NULL,
                    ClassName TEXT,
                    EducationLevel TEXT,
                    CurrentTopic TEXT,
                    CreatedDate TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Students (
                    StudentId TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Email TEXT,
                    EnrollmentDate TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Lessons (
                    LessonId TEXT PRIMARY KEY,
                    ClassId TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Content TEXT,
                    CreatedDate TEXT NOT NULL,
                    FOREIGN KEY (ClassId) REFERENCES Classes(ClassId)
                );

                CREATE TABLE IF NOT EXISTS Assignments (
                    AssignmentId TEXT PRIMARY KEY,
                    LessonId TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Description TEXT,
                    DueDate TEXT,
                    FOREIGN KEY (LessonId) REFERENCES Lessons(LessonId)
                );
            ", null);
        }

        /// <summary>
        /// Insert test data
        /// </summary>
        private async Task InsertTestDataAsync()
        {
            // Insert a test conversation
            await DatabaseContext.ExecuteNonQueryAsync(@"
                INSERT INTO Conversations (ConversationId, Title, SystemPrompt, CreatedDate)
                VALUES (@ConversationId, @Title, @SystemPrompt, @CreatedDate);
            ", new
            {
                ConversationId = Guid.NewGuid().ToString(),
                Title = "Test Conversation",
                SystemPrompt = "You are a helpful assistant.",
                CreatedDate = DateTime.Now.ToString("o")
            });

            // Insert a test class
            string classId = Guid.NewGuid().ToString();
            await DatabaseContext.ExecuteNonQueryAsync(@"
                INSERT INTO Classes (ClassId, ClassCode, ClassName, EducationLevel, CurrentTopic, CreatedDate)
                VALUES (@ClassId, @ClassCode, @ClassName, @EducationLevel, @CurrentTopic, @CreatedDate);
            ", new
            {
                ClassId = classId,
                ClassCode = "CS101",
                ClassName = "Introduction to Computer Science",
                EducationLevel = "Undergraduate",
                CurrentTopic = "Variables and Data Types",
                CreatedDate = DateTime.Now.ToString("o")
            });

            // Insert a test student
            await DatabaseContext.ExecuteNonQueryAsync(@"
                INSERT INTO Students (StudentId, Name, Email, EnrollmentDate)
                VALUES (@StudentId, @Name, @Email, @EnrollmentDate);
            ", new
            {
                StudentId = Guid.NewGuid().ToString(),
                Name = "John Doe",
                Email = "john.doe@example.com",
                EnrollmentDate = DateTime.Now.AddDays(-30).ToString("o")
            });

            // Insert a test lesson
            string lessonId = Guid.NewGuid().ToString();
            await DatabaseContext.ExecuteNonQueryAsync(@"
                INSERT INTO Lessons (LessonId, ClassId, Title, Content, CreatedDate)
                VALUES (@LessonId, @ClassId, @Title, @Content, @CreatedDate);
            ", new
            {
                LessonId = lessonId,
                ClassId = classId,
                Title = "Variables and Data Types",
                Content = "This lesson covers variables and data types in programming.",
                CreatedDate = DateTime.Now.AddDays(-5).ToString("o")
            });

            // Insert a test assignment
            await DatabaseContext.ExecuteNonQueryAsync(@"
                INSERT INTO Assignments (AssignmentId, LessonId, Title, Description, DueDate)
                VALUES (@AssignmentId, @LessonId, @Title, @Description, @DueDate);
            ", new
            {
                AssignmentId = Guid.NewGuid().ToString(),
                LessonId = lessonId,
                Title = "Programming Exercise 1",
                Description = "Complete the programming exercises in the textbook.",
                DueDate = DateTime.Now.AddDays(7).ToString("o")
            });
        }

        /// <summary>
        /// Cleanup after tests
        /// </summary>
        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Close any open connections
                SqliteConnection.ClearAllPools();

                // Delete the database file
                if (File.Exists(DatabasePath))
                {
                    File.Delete(DatabasePath);
                }
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }

            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
