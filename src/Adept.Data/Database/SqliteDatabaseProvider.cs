using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Adept.Data.Database
{
    /// <summary>
    /// SQLite implementation of the database provider
    /// </summary>
    public class SqliteDatabaseProvider : IDatabaseProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SqliteDatabaseProvider> _logger;
        private readonly string _connectionString;
        private readonly string _databasePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteDatabaseProvider"/> class
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        public SqliteDatabaseProvider(IConfiguration configuration, ILogger<SqliteDatabaseProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Get connection string from configuration
            _connectionString = _configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=data/adept.db";

            // Extract database path from connection string
            var connectionStringBuilder = new SqliteConnectionStringBuilder(_connectionString);
            _databasePath = connectionStringBuilder.DataSource;

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <inheritdoc/>
        public string ConnectionString => _connectionString;

        /// <inheritdoc/>
        public string DatabasePath => _databasePath;

        /// <inheritdoc/>
        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <inheritdoc/>
        public bool DatabaseExists()
        {
            return File.Exists(_databasePath);
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            if (!DatabaseExists())
            {
                _logger.LogInformation("Database does not exist. Creating new database at {DatabasePath}", _databasePath);
                
                // Create the database file
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Create tables
                await CreateTablesAsync(connection);
            }
            else
            {
                _logger.LogInformation("Database exists at {DatabasePath}", _databasePath);
            }
        }

        /// <summary>
        /// Creates the database tables
        /// </summary>
        /// <param name="connection">The database connection</param>
        private async Task CreateTablesAsync(SqliteConnection connection)
        {
            // Create database version table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS DatabaseVersion (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Version INTEGER NOT NULL,
                        AppliedAt TEXT NOT NULL DEFAULT (datetime('now'))
                    )";
                await command.ExecuteNonQueryAsync();
            }

            // Create lesson resources table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS LessonResources (
                        ResourceId TEXT PRIMARY KEY,
                        LessonId TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        Type INTEGER NOT NULL,
                        Path TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    )";
                await command.ExecuteNonQueryAsync();
            }

            // Create lesson templates table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS LessonTemplates (
                        TemplateId TEXT PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        Tags TEXT NOT NULL,
                        Title TEXT NOT NULL,
                        LearningObjectives TEXT NOT NULL,
                        ComponentsJson TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    )";
                await command.ExecuteNonQueryAsync();
            }

            // Insert initial database version
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO DatabaseVersion (Version) VALUES (1)";
                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Database tables created successfully");
        }
    }
}
