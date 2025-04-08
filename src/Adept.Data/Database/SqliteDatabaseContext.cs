using Adept.Common.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;
using System.Text.Json;

namespace Adept.Data.Database
{
    /// <summary>
    /// SQLite implementation of the database context
    /// </summary>
    public class SqliteDatabaseContext : IDatabaseContext
    {
        private readonly string _connectionString;
        private readonly ILogger<SqliteDatabaseContext> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteDatabaseContext"/> class
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        public SqliteDatabaseContext(IConfiguration configuration, ILogger<SqliteDatabaseContext> logger)
        {
            _connectionString = configuration["Database:ConnectionString"] ?? "Data Source=data/adept.db";
            _logger = logger;

            // Ensure the database directory exists
            var dbPath = _connectionString.Replace("Data Source=", "");
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Initialize the database
            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes a non-query SQL command
        /// </summary>
        /// <param name="sql">The SQL command to execute</param>
        /// <param name="parameters">Optional parameters for the SQL command</param>
        /// <returns>The number of rows affected</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, parameters);

                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing non-query: {Sql}", sql);
                throw;
            }
        }

        /// <summary>
        /// Executes a SQL query and returns a single value
        /// </summary>
        /// <typeparam name="T">The type of the return value</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>The query result</returns>
        public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, parameters);

                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    return default;
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scalar query: {Sql}", sql);
                throw;
            }
        }

        /// <summary>
        /// Executes a SQL query and returns a collection of results
        /// </summary>
        /// <typeparam name="T">The type of objects to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>A collection of query results</returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, parameters);

                using var reader = await command.ExecuteReaderAsync();
                var results = new List<T>();

                while (await reader.ReadAsync())
                {
                    var item = MapToObject<T>(reader);
                    if (item != null)
                    {
                        results.Add(item);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query: {Sql}", sql);
                throw;
            }
        }

        /// <summary>
        /// Executes a SQL query and returns a single result
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>A single query result or default if no results</returns>
        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, parameters);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToObject<T>(reader);
                }

                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing single query: {Sql}", sql);
                throw;
            }
        }

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        /// <returns>A transaction object</returns>
        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            try
            {
                var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var transaction = connection.BeginTransaction();
                return new SqliteDbTransaction(connection, transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error beginning transaction");
                throw;
            }
        }

        /// <summary>
        /// Initializes the database with required tables
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                // Check if database exists and create tables if needed
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Check database version
                var versionExists = await ExecuteScalarAsync<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DatabaseVersion'");
                if (versionExists == 0)
                {
                    // Create version table
                    await ExecuteNonQueryAsync("CREATE TABLE DatabaseVersion (Version INTEGER NOT NULL, AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)");
                    await ExecuteNonQueryAsync("INSERT INTO DatabaseVersion (Version) VALUES (0)");
                }

                // Get current version
                var currentVersion = await ExecuteScalarAsync<long>("SELECT Version FROM DatabaseVersion ORDER BY Version DESC LIMIT 1") ?? 0;

                // Apply migrations
                await ApplyMigrationsAsync(currentVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        /// <summary>
        /// Applies database migrations
        /// </summary>
        /// <param name="currentVersion">The current database version</param>
        private async Task ApplyMigrationsAsync(long currentVersion)
        {
            // Define migrations
            var migrations = new Dictionary<long, string>
            {
                {
                    1,
                    @"
                    CREATE TABLE Configuration (
                        key TEXT PRIMARY KEY,
                        value TEXT NOT NULL,
                        encrypted INTEGER NOT NULL DEFAULT 0,
                        last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE SecureStorage (
                        key TEXT PRIMARY KEY,
                        encrypted_value BLOB NOT NULL,
                        iv BLOB NOT NULL,
                        last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE Classes (
                        class_id TEXT PRIMARY KEY,
                        class_code TEXT NOT NULL,
                        education_level TEXT NOT NULL,
                        current_topic TEXT,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX idx_classes_code ON Classes(class_code);

                    CREATE TABLE Students (
                        student_id TEXT PRIMARY KEY,
                        class_id TEXT NOT NULL,
                        name TEXT NOT NULL,
                        fsm_status INTEGER,
                        sen_status INTEGER,
                        eal_status INTEGER,
                        ability_level TEXT,
                        reading_age TEXT,
                        target_grade TEXT,
                        notes TEXT,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE CASCADE
                    );
                    CREATE INDEX idx_students_class ON Students(class_id);

                    CREATE TABLE TeachingSchedule (
                        schedule_id TEXT PRIMARY KEY,
                        day_of_week INTEGER NOT NULL,
                        time_slot INTEGER NOT NULL,
                        class_id TEXT,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE SET NULL
                    );
                    CREATE UNIQUE INDEX idx_schedule_slot ON TeachingSchedule(day_of_week, time_slot);
                    CREATE INDEX idx_schedule_class ON TeachingSchedule(class_id);

                    CREATE TABLE LessonPlans (
                        lesson_id TEXT PRIMARY KEY,
                        class_id TEXT NOT NULL,
                        date TEXT NOT NULL,
                        time_slot INTEGER NOT NULL,
                        title TEXT NOT NULL,
                        learning_objectives TEXT,
                        calendar_event_id TEXT,
                        components_json TEXT NOT NULL,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX idx_lesson_class_date_slot ON LessonPlans(class_id, date, time_slot);
                    CREATE INDEX idx_lesson_date ON LessonPlans(date);
                    CREATE INDEX idx_lesson_calendar ON LessonPlans(calendar_event_id);

                    CREATE TABLE Conversations (
                        conversation_id TEXT PRIMARY KEY,
                        class_id TEXT,
                        date TEXT NOT NULL,
                        time_slot INTEGER,
                        history_json TEXT NOT NULL,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE SET NULL
                    );
                    CREATE INDEX idx_conversation_class ON Conversations(class_id);
                    CREATE INDEX idx_conversation_date ON Conversations(date);

                    CREATE TABLE SystemPrompts (
                        prompt_id TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        content TEXT NOT NULL,
                        is_default INTEGER NOT NULL DEFAULT 0,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX idx_prompts_default ON SystemPrompts(is_default);
                    "
                }
                // Add more migrations as needed
            };

            // Apply migrations in order
            using var transaction = await BeginTransactionAsync();
            try
            {
                foreach (var migration in migrations.OrderBy(m => m.Key))
                {
                    if (migration.Key > currentVersion)
                    {
                        _logger.LogInformation("Applying migration {Version}", migration.Key);
                        await ExecuteNonQueryAsync(migration.Value);
                        await ExecuteNonQueryAsync("INSERT INTO DatabaseVersion (Version) VALUES (@Version)", new { Version = migration.Key });
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error applying migrations");
                throw;
            }
        }

        /// <summary>
        /// Adds parameters to a SQL command
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="parameters">The parameters</param>
        private void AddParameters(SqliteCommand command, object? parameters)
        {
            if (parameters == null)
            {
                return;
            }

            var properties = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var value = property.GetValue(parameters);
                command.Parameters.AddWithValue($"@{property.Name}", value ?? DBNull.Value);
            }
        }

        /// <summary>
        /// Maps a data reader to an object
        /// </summary>
        /// <typeparam name="T">The type of object to create</typeparam>
        /// <param name="reader">The data reader</param>
        /// <returns>The mapped object</returns>
        private T? MapToObject<T>(IDataReader reader)
        {
            var obj = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var property = properties.FirstOrDefault(p => 
                    string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

                if (property != null && property.CanWrite)
                {
                    var value = reader.GetValue(i);
                    if (value != DBNull.Value)
                    {
                        if (property.PropertyType == typeof(DateTime) && value is string dateStr)
                        {
                            if (DateTime.TryParse(dateStr, out var dateValue))
                            {
                                property.SetValue(obj, dateValue);
                            }
                        }
                        else if (property.PropertyType.IsEnum && value is string enumStr)
                        {
                            if (Enum.TryParse(property.PropertyType, enumStr, true, out var enumValue))
                            {
                                property.SetValue(obj, enumValue);
                            }
                        }
                        else
                        {
                            try
                            {
                                property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
                            }
                            catch
                            {
                                // If conversion fails, try JSON deserialization for complex types
                                if (value is string jsonStr)
                                {
                                    try
                                    {
                                        var deserializedValue = JsonSerializer.Deserialize(jsonStr, property.PropertyType);
                                        property.SetValue(obj, deserializedValue);
                                    }
                                    catch
                                    {
                                        // Ignore JSON deserialization errors
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return obj;
        }
    }

    /// <summary>
    /// SQLite implementation of the database transaction
    /// </summary>
    public class SqliteDbTransaction : IDbTransaction
    {
        private readonly SqliteConnection _connection;
        private readonly SqliteTransaction _transaction;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteDbTransaction"/> class
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="transaction">The transaction</param>
        public SqliteDbTransaction(SqliteConnection connection, SqliteTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        /// <summary>
        /// Commits the transaction
        /// </summary>
        public Task CommitAsync()
        {
            _transaction.Commit();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Rolls back the transaction
        /// </summary>
        public Task RollbackAsync()
        {
            _transaction.Rollback();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the transaction
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the transaction asynchronously
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the transaction asynchronously
        /// </summary>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_disposed)
            {
                _transaction.Dispose();
                await _connection.DisposeAsync();
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes the transaction
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction.Dispose();
                    _connection.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
