using Adept.Common.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;
using System.Text.Json;
using DbTransaction = Adept.Common.Interfaces.IDbTransaction;

namespace Adept.Data.Database
{
    /// <summary>
    /// SQLite implementation of the database context
    /// </summary>
    public class SqliteDatabaseContext : IDatabaseContext
    {
        private readonly string _connectionString;
        private readonly ILogger<SqliteDatabaseContext> _logger;
        private readonly IDatabaseBackupService? _backupService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteDatabaseContext"/> class
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        /// <param name="backupService">The backup service (optional)</param>
        public SqliteDatabaseContext(IConfiguration configuration, ILogger<SqliteDatabaseContext> logger, IDatabaseBackupService? backupService = null)
        {
            _connectionString = configuration["Database:ConnectionString"] ?? "Data Source=data/adept.db";
            _logger = logger;
            _backupService = backupService;

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
        public async Task<DbTransaction> BeginTransactionAsync()
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
                var result = await ExecuteScalarAsync<long?>("SELECT Version FROM DatabaseVersion ORDER BY Version DESC LIMIT 1");
                var currentVersion = result.HasValue ? result.Value : 0L;

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
            var migrations = new Dictionary<long, string>(DatabaseMigrations.Migrations)
            {
                {
                    1,
                    @"
                    -- Configuration table for application settings
                    CREATE TABLE Configuration (
                        key TEXT PRIMARY KEY,
                        value TEXT NOT NULL,
                        encrypted INTEGER NOT NULL DEFAULT 0,
                        last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Secure storage for API keys and sensitive data
                    CREATE TABLE SecureStorage (
                        key TEXT PRIMARY KEY,
                        encrypted_value BLOB NOT NULL,
                        iv BLOB NOT NULL,
                        last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );

                    -- Classes table for storing class information
                    CREATE TABLE Classes (
                        class_id TEXT PRIMARY KEY,
                        class_code TEXT NOT NULL,
                        education_level TEXT NOT NULL,
                        current_topic TEXT,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT uq_class_code UNIQUE (class_code)
                    );
                    CREATE INDEX idx_classes_code ON Classes(class_code);

                    -- Students table for storing student information
                    CREATE TABLE Students (
                        student_id TEXT PRIMARY KEY,
                        class_id TEXT NOT NULL,
                        name TEXT NOT NULL,
                        fsm_status INTEGER CHECK (fsm_status IN (0, 1) OR fsm_status IS NULL),
                        sen_status INTEGER CHECK (sen_status IN (0, 1) OR sen_status IS NULL),
                        eal_status INTEGER CHECK (eal_status IN (0, 1) OR eal_status IS NULL),
                        ability_level TEXT,
                        reading_age TEXT,
                        target_grade TEXT,
                        notes TEXT,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE CASCADE
                    );
                    CREATE INDEX idx_students_class ON Students(class_id);

                    -- Teaching schedule table for storing weekly class schedules
                    CREATE TABLE TeachingSchedule (
                        schedule_id TEXT PRIMARY KEY,
                        day_of_week INTEGER NOT NULL CHECK (day_of_week BETWEEN 0 AND 6),
                        time_slot INTEGER NOT NULL CHECK (time_slot BETWEEN 0 AND 4),
                        class_id TEXT,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT uq_schedule_slot UNIQUE (day_of_week, time_slot),
                        FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE SET NULL
                    );
                    CREATE INDEX idx_schedule_class ON TeachingSchedule(class_id);

                    -- Lesson plans table for storing lesson information
                    CREATE TABLE LessonPlans (
                        lesson_id TEXT PRIMARY KEY,
                        class_id TEXT NOT NULL,
                        date TEXT NOT NULL,
                        time_slot INTEGER NOT NULL CHECK (time_slot BETWEEN 0 AND 4),
                        title TEXT NOT NULL,
                        learning_objectives TEXT,
                        calendar_event_id TEXT,
                        components_json TEXT NOT NULL DEFAULT '{}',
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT uq_lesson_class_date_slot UNIQUE (class_id, date, time_slot),
                        FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE CASCADE
                    );
                    CREATE INDEX idx_lesson_date ON LessonPlans(date);
                    CREATE INDEX idx_lesson_calendar ON LessonPlans(calendar_event_id);

                    -- Conversations table for storing chat history
                    CREATE TABLE Conversations (
                        conversation_id TEXT PRIMARY KEY,
                        class_id TEXT,
                        date TEXT NOT NULL,
                        time_slot INTEGER CHECK (time_slot BETWEEN 0 AND 4),
                        history_json TEXT NOT NULL DEFAULT '[]',
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE SET NULL
                    );
                    CREATE INDEX idx_conversation_class ON Conversations(class_id);
                    CREATE INDEX idx_conversation_date ON Conversations(date);

                    -- System prompts table for storing LLM system prompts
                    CREATE TABLE SystemPrompts (
                        prompt_id TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        content TEXT NOT NULL,
                        is_default INTEGER NOT NULL DEFAULT 0,
                        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX idx_prompts_default ON SystemPrompts(is_default);

                    -- Database version tracking table
                    CREATE TABLE DatabaseVersion (
                        Version INTEGER PRIMARY KEY,
                        AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    "
                },
                {
                    2,
                    @"
                    -- Add validation triggers for Classes table
                    CREATE TRIGGER IF NOT EXISTS validate_class_insert
                    BEFORE INSERT ON Classes
                    BEGIN
                        SELECT CASE
                            WHEN NEW.class_code IS NULL OR trim(NEW.class_code) = ''
                                THEN RAISE(ABORT, 'Class code cannot be empty')
                            WHEN NEW.education_level IS NULL OR trim(NEW.education_level) = ''
                                THEN RAISE(ABORT, 'Education level cannot be empty')
                        END;
                    END;

                    CREATE TRIGGER IF NOT EXISTS validate_class_update
                    BEFORE UPDATE ON Classes
                    BEGIN
                        SELECT CASE
                            WHEN NEW.class_code IS NULL OR trim(NEW.class_code) = ''
                                THEN RAISE(ABORT, 'Class code cannot be empty')
                            WHEN NEW.education_level IS NULL OR trim(NEW.education_level) = ''
                                THEN RAISE(ABORT, 'Education level cannot be empty')
                        END;
                        UPDATE NEW SET updated_at = CURRENT_TIMESTAMP;
                    END;

                    -- Add validation triggers for Students table
                    CREATE TRIGGER IF NOT EXISTS validate_student_insert
                    BEFORE INSERT ON Students
                    BEGIN
                        SELECT CASE
                            WHEN NEW.name IS NULL OR trim(NEW.name) = ''
                                THEN RAISE(ABORT, 'Student name cannot be empty')
                            WHEN NOT EXISTS (SELECT 1 FROM Classes WHERE class_id = NEW.class_id)
                                THEN RAISE(ABORT, 'Referenced class does not exist')
                        END;
                    END;

                    CREATE TRIGGER IF NOT EXISTS validate_student_update
                    BEFORE UPDATE ON Students
                    BEGIN
                        SELECT CASE
                            WHEN NEW.name IS NULL OR trim(NEW.name) = ''
                                THEN RAISE(ABORT, 'Student name cannot be empty')
                            WHEN NOT EXISTS (SELECT 1 FROM Classes WHERE class_id = NEW.class_id)
                                THEN RAISE(ABORT, 'Referenced class does not exist')
                        END;
                        UPDATE NEW SET updated_at = CURRENT_TIMESTAMP;
                    END;

                    -- Add validation triggers for LessonPlans table
                    CREATE TRIGGER IF NOT EXISTS validate_lesson_insert
                    BEFORE INSERT ON LessonPlans
                    BEGIN
                        SELECT CASE
                            WHEN NEW.title IS NULL OR trim(NEW.title) = ''
                                THEN RAISE(ABORT, 'Lesson title cannot be empty')
                            WHEN NEW.date IS NULL OR trim(NEW.date) = ''
                                THEN RAISE(ABORT, 'Lesson date cannot be empty')
                            WHEN NEW.time_slot < 0 OR NEW.time_slot > 4
                                THEN RAISE(ABORT, 'Time slot must be between 0 and 4')
                            WHEN NOT EXISTS (SELECT 1 FROM Classes WHERE class_id = NEW.class_id)
                                THEN RAISE(ABORT, 'Referenced class does not exist')
                        END;
                    END;

                    CREATE TRIGGER IF NOT EXISTS validate_lesson_update
                    BEFORE UPDATE ON LessonPlans
                    BEGIN
                        SELECT CASE
                            WHEN NEW.title IS NULL OR trim(NEW.title) = ''
                                THEN RAISE(ABORT, 'Lesson title cannot be empty')
                            WHEN NEW.date IS NULL OR trim(NEW.date) = ''
                                THEN RAISE(ABORT, 'Lesson date cannot be empty')
                            WHEN NEW.time_slot < 0 OR NEW.time_slot > 4
                                THEN RAISE(ABORT, 'Time slot must be between 0 and 4')
                            WHEN NOT EXISTS (SELECT 1 FROM Classes WHERE class_id = NEW.class_id)
                                THEN RAISE(ABORT, 'Referenced class does not exist')
                        END;
                        UPDATE NEW SET updated_at = CURRENT_TIMESTAMP;
                    END;
                    "
                },
                {
                    3,
                    @"
                    -- Add validation triggers for Conversations table
                    CREATE TRIGGER IF NOT EXISTS validate_conversation_insert
                    BEFORE INSERT ON Conversations
                    BEGIN
                        SELECT CASE
                            WHEN NEW.date IS NULL OR trim(NEW.date) = ''
                                THEN RAISE(ABORT, 'Conversation date cannot be empty')
                            WHEN NEW.class_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Classes WHERE class_id = NEW.class_id)
                                THEN RAISE(ABORT, 'Referenced class does not exist')
                            WHEN NEW.time_slot IS NOT NULL AND (NEW.time_slot < 0 OR NEW.time_slot > 4)
                                THEN RAISE(ABORT, 'Time slot must be between 0 and 4')
                        END;
                    END;

                    CREATE TRIGGER IF NOT EXISTS validate_conversation_update
                    BEFORE UPDATE ON Conversations
                    BEGIN
                        SELECT CASE
                            WHEN NEW.date IS NULL OR trim(NEW.date) = ''
                                THEN RAISE(ABORT, 'Conversation date cannot be empty')
                            WHEN NEW.class_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Classes WHERE class_id = NEW.class_id)
                                THEN RAISE(ABORT, 'Referenced class does not exist')
                            WHEN NEW.time_slot IS NOT NULL AND (NEW.time_slot < 0 OR NEW.time_slot > 4)
                                THEN RAISE(ABORT, 'Time slot must be between 0 and 4')
                        END;
                        UPDATE NEW SET updated_at = CURRENT_TIMESTAMP;
                    END;

                    -- Add validation triggers for SystemPrompts table
                    CREATE TRIGGER IF NOT EXISTS validate_prompt_insert
                    BEFORE INSERT ON SystemPrompts
                    BEGIN
                        SELECT CASE
                            WHEN NEW.name IS NULL OR trim(NEW.name) = ''
                                THEN RAISE(ABORT, 'Prompt name cannot be empty')
                            WHEN NEW.content IS NULL OR trim(NEW.content) = ''
                                THEN RAISE(ABORT, 'Prompt content cannot be empty')
                        END;
                    END;

                    CREATE TRIGGER IF NOT EXISTS validate_prompt_update
                    BEFORE UPDATE ON SystemPrompts
                    BEGIN
                        SELECT CASE
                            WHEN NEW.name IS NULL OR trim(NEW.name) = ''
                                THEN RAISE(ABORT, 'Prompt name cannot be empty')
                            WHEN NEW.content IS NULL OR trim(NEW.content) = ''
                                THEN RAISE(ABORT, 'Prompt content cannot be empty')
                        END;
                        UPDATE NEW SET updated_at = CURRENT_TIMESTAMP;
                    END;
                    "
                }
                // Add more migrations as needed
            };

            // Create a backup before applying migrations if backup service is available
            if (_backupService != null && currentVersion < migrations.Keys.Max())
            {
                try
                {
                    _logger.LogInformation("Creating database backup before applying migrations");
                    string backupPath = await _backupService.CreateMigrationBackupAsync();
                    _logger.LogInformation("Database backup created at {BackupPath}", backupPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create database backup before migrations");
                    // Continue with migrations even if backup fails
                }
            }

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
    public class SqliteDbTransaction : DbTransaction
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
