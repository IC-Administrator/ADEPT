using Adept.Common.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using IDbTransaction = Adept.Common.Interfaces.IDbTransaction;

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
        /// Executes a SQL query and returns a scalar result
        /// </summary>
        /// <typeparam name="T">The type of result to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>The scalar result</returns>
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
                    results.Add(MapToObject<T>(reader));
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
        /// Executes a SQL query and returns a single result or default
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>The query result or default</returns>
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
            // Get migrations from the DatabaseMigrations class
            var migrations = DatabaseMigrations.Migrations;

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
        /// Adds parameters to a command
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
        /// <typeparam name="T">The type of object to map to</typeparam>
        /// <param name="reader">The data reader</param>
        /// <returns>The mapped object</returns>
        private T MapToObject<T>(SqliteDataReader reader)
        {
            var obj = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var property = properties.FirstOrDefault(p => p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

                if (property != null && reader[i] != DBNull.Value)
                {
                    var value = reader[i];
                    var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    // Handle special cases
                    if (propertyType == typeof(Guid) && value is string stringValue)
                    {
                        property.SetValue(obj, Guid.Parse(stringValue));
                    }
                    else if (propertyType.IsEnum && value is long longValue)
                    {
                        property.SetValue(obj, Enum.ToObject(propertyType, longValue));
                    }
                    else if (propertyType == typeof(DateTime) && value is string dateString)
                    {
                        property.SetValue(obj, DateTime.Parse(dateString));
                    }
                    else if (propertyType == typeof(bool) && value is long boolValue)
                    {
                        property.SetValue(obj, boolValue != 0);
                    }
                    else if (propertyType == typeof(string) && value is byte[] bytes)
                    {
                        property.SetValue(obj, System.Text.Encoding.UTF8.GetString(bytes));
                    }
                    else if (property.Name.EndsWith("Json") && value is string json)
                    {
                        // Handle JSON properties
                        var jsonPropertyName = property.Name.Substring(0, property.Name.Length - 4);
                        var jsonProperty = properties.FirstOrDefault(p => p.Name.Equals(jsonPropertyName, StringComparison.OrdinalIgnoreCase));

                        if (jsonProperty != null)
                        {
                            try
                            {
                                var deserializedValue = JsonSerializer.Deserialize(json, jsonProperty.PropertyType);
                                jsonProperty.SetValue(obj, deserializedValue);
                            }
                            catch (Exception ex)
                            {
                                // Log but continue
                                Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                            }
                        }
                        else
                        {
                            // Just set the JSON string
                            property.SetValue(obj, value);
                        }
                    }
                    else
                    {
                        // Standard conversion
                        try
                        {
                            var convertedValue = Convert.ChangeType(value, propertyType);
                            property.SetValue(obj, convertedValue);
                        }
                        catch (Exception)
                        {
                            // If conversion fails, try direct assignment
                            try
                            {
                                property.SetValue(obj, value);
                            }
                            catch (Exception ex)
                            {
                                // Log but continue
                                Console.WriteLine($"Error setting property {property.Name}: {ex.Message}");
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
        private bool _isDisposed;

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
        /// Commits the transaction asynchronously
        /// </summary>
        public async Task CommitAsync()
        {
            await Task.Run(() => _transaction.Commit());
        }

        /// <summary>
        /// Rolls back the transaction asynchronously
        /// </summary>
        public async Task RollbackAsync()
        {
            await Task.Run(() => _transaction.Rollback());
        }

        /// <summary>
        /// Disposes the transaction
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _transaction.Dispose();
            _connection.Dispose();
            _isDisposed = true;
        }

        /// <summary>
        /// Disposes the transaction asynchronously
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _transaction.Dispose();
            _connection.Dispose();
            _isDisposed = true;

            await Task.CompletedTask;
        }
    }
}
