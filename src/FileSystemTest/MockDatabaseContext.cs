using Adept.Common.Interfaces;
using System.Data;
using DbTransaction = Adept.Common.Interfaces.IDbTransaction;

namespace FileSystemTest
{
    /// <summary>
    /// Mock implementation of the database context for testing
    /// </summary>
    public class MockDatabaseContext : IDatabaseContext
    {
        /// <summary>
        /// Gets a database connection
        /// </summary>
        /// <returns>The database connection</returns>
        public IDbConnection GetConnection()
        {
            return null!;
        }

        /// <summary>
        /// Initializes the database
        /// </summary>
        public void InitializeDatabase()
        {
            // Do nothing
        }

        /// <summary>
        /// Executes a non-query SQL command
        /// </summary>
        /// <param name="sql">The SQL command to execute</param>
        /// <param name="parameters">Optional parameters for the SQL command</param>
        /// <returns>The number of rows affected</returns>
        public Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null)
        {
            return Task.FromResult(0);
        }

        /// <summary>
        /// Executes a SQL query and returns a scalar result
        /// </summary>
        /// <typeparam name="T">The type of result to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>The scalar result</returns>
        public Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            return Task.FromResult<T?>(default);
        }

        /// <summary>
        /// Executes a SQL query and returns a collection of results
        /// </summary>
        /// <typeparam name="T">The type of objects to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>A collection of query results</returns>
        public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            return Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
        }

        /// <summary>
        /// Executes a SQL query and returns a single result
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>A single query result or default if no results</returns>
        public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null)
        {
            return Task.FromResult<T?>(default);
        }

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        /// <returns>A transaction object</returns>
        public Task<DbTransaction> BeginTransactionAsync()
        {
            return Task.FromResult<DbTransaction>(new MockDbTransaction());
        }
    }

    /// <summary>
    /// Mock implementation of a database transaction for testing
    /// </summary>
    public class MockDbTransaction : DbTransaction
    {
        /// <summary>
        /// Commits the transaction
        /// </summary>
        public Task CommitAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Rolls back the transaction
        /// </summary>
        public Task RollbackAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the transaction
        /// </summary>
        public void Dispose()
        {
            // Do nothing
        }

        /// <summary>
        /// Disposes the transaction asynchronously
        /// </summary>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
