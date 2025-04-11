using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        private readonly SqliteConnectionStringBuilder _connectionStringBuilder;
        private readonly object _connectionLock = new object();
        private readonly Dictionary<string, SqliteConnection> _connectionPool = new Dictionary<string, SqliteConnection>();
        private bool _isInitialized = false;

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
                ?? _configuration["Database:ConnectionString"]
                ?? "Data Source=data/adept.db";

            // Extract database path from connection string
            _connectionStringBuilder = new SqliteConnectionStringBuilder(_connectionString);
            _databasePath = _connectionStringBuilder.DataSource;

            // Configure connection pooling
            _connectionStringBuilder.Pooling = true;
            // Note: SQLite connection pooling is managed internally by Microsoft.Data.Sqlite
            // and doesn't expose MaxPoolSize configuration

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
            // Generate a unique connection ID for this thread
            string connectionId = $"conn_{Thread.CurrentThread.ManagedThreadId}";

            lock (_connectionLock)
            {
                // Check if we already have a connection for this thread
                if (_connectionPool.TryGetValue(connectionId, out var existingConnection))
                {
                    // If the connection is open, return it
                    if (existingConnection.State == System.Data.ConnectionState.Open)
                    {
                        return existingConnection;
                    }

                    // If the connection is closed, remove it from the pool
                    _connectionPool.Remove(connectionId);
                    existingConnection.Dispose();
                }

                // Create a new connection
                var connection = new SqliteConnection(_connectionStringBuilder.ConnectionString);
                connection.Open();

                // Add to the pool
                _connectionPool[connectionId] = connection;

                return connection;
            }
        }

        /// <inheritdoc/>
        public bool DatabaseExists()
        {
            return File.Exists(_databasePath);
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                if (!DatabaseExists())
                {
                    _logger.LogInformation("Database does not exist. Creating new database at {DatabasePath}", _databasePath);

                    // Create the database file
                    using var connection = CreateConnection();

                    // Create tables
                    await CreateTablesAsync(connection);
                }
                else
                {
                    _logger.LogInformation("Database exists at {DatabasePath}", _databasePath);

                    // Check for and apply any pending migrations
                    using var connection = CreateConnection();
                    await ApplyMigrationsAsync(connection);
                }

                // Enable foreign keys
                using (var connection = CreateConnection())
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = "PRAGMA foreign_keys = ON";
                    await command.ExecuteNonQueryAsync();
                }

                _isInitialized = true;
                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
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
                command.CommandText = "INSERT INTO DatabaseVersion (Version, AppliedAt) VALUES (1, datetime('now'))";
                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Database tables created successfully");
        }

        /// <summary>
        /// Applies database migrations
        /// </summary>
        /// <param name="connection">The database connection</param>
        private async Task ApplyMigrationsAsync(SqliteConnection connection)
        {
            try
            {
                // Check if the DatabaseVersion table exists
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DatabaseVersion'";
                    var result = await command.ExecuteScalarAsync();
                    if (Convert.ToInt64(result) == 0)
                    {
                        // Create the DatabaseVersion table if it doesn't exist
                        using var createCommand = connection.CreateCommand();
                        createCommand.CommandText = @"
                            CREATE TABLE IF NOT EXISTS DatabaseVersion (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Version INTEGER NOT NULL,
                                AppliedAt TEXT NOT NULL DEFAULT (datetime('now'))
                            )";
                        await createCommand.ExecuteNonQueryAsync();

                        // Insert initial version
                        using var insertCommand = connection.CreateCommand();
                        insertCommand.CommandText = "INSERT INTO DatabaseVersion (Version, AppliedAt) VALUES (1, datetime('now'))";
                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }

                // Get current database version
                long currentVersion;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT MAX(Version) FROM DatabaseVersion";
                    var result = await command.ExecuteScalarAsync();
                    currentVersion = result != DBNull.Value ? Convert.ToInt64(result) : 0;
                }

                _logger.LogInformation("Current database version: {Version}", currentVersion);

                // Get all migrations from the DatabaseMigrations class
                var migrations = DatabaseMigrations.Migrations
                    .Where(m => m.Key > currentVersion)
                    .OrderBy(m => m.Key);

                if (!migrations.Any())
                {
                    _logger.LogInformation("No database migrations to apply");
                    return;
                }

                _logger.LogInformation("Applying {Count} database migrations", migrations.Count());

                // Begin a transaction for all migrations
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var migration in migrations)
                    {
                        _logger.LogInformation("Applying migration {Version}", migration.Key);

                        // Execute the migration script
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = migration.Value;
                            await command.ExecuteNonQueryAsync();
                        }

                        // Update the database version
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = "INSERT INTO DatabaseVersion (Version, AppliedAt) VALUES (@Version, datetime('now'))";
                            command.Parameters.AddWithValue("@Version", migration.Key);
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    // Commit the transaction
                    transaction.Commit();
                    _logger.LogInformation("Database migrations applied successfully");
                }
                catch (Exception ex)
                {
                    // Rollback the transaction on error
                    transaction.Rollback();
                    _logger.LogError(ex, "Error applying database migrations");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking or applying database migrations");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<long> GetCurrentVersionAsync()
        {
            try
            {
                using var connection = CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT MAX(Version) FROM DatabaseVersion";
                var result = await command.ExecuteScalarAsync();
                return result != DBNull.Value ? Convert.ToInt64(result) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current database version");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DatabaseMigrationInfo>> GetAppliedMigrationsAsync()
        {
            try
            {
                var migrations = new List<DatabaseMigrationInfo>();

                using var connection = CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Version, AppliedAt FROM DatabaseVersion ORDER BY Version";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var migration = new DatabaseMigrationInfo
                    {
                        Version = reader.GetInt64(0),
                        AppliedAt = DateTime.Parse(reader.GetString(1))
                    };

                    migrations.Add(migration);
                }

                return migrations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applied migrations");
                return Enumerable.Empty<DatabaseMigrationInfo>();
            }
        }

        /// <inheritdoc/>
        public async Task ExecuteSqlScriptAsync(string sql)
        {
            try
            {
                using var connection = CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL script");
                throw;
            }
        }
    }
}
