namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Interface for database context operations
    /// </summary>
    public interface IDatabaseContext
    {
        /// <summary>
        /// Executes a non-query SQL command
        /// </summary>
        /// <param name="sql">The SQL command to execute</param>
        /// <param name="parameters">Optional parameters for the SQL command</param>
        /// <returns>The number of rows affected</returns>
        Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null);

        /// <summary>
        /// Executes a SQL query and returns a single value
        /// </summary>
        /// <typeparam name="T">The type of the return value</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>The query result</returns>
        Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);

        /// <summary>
        /// Executes a SQL query and returns a collection of results
        /// </summary>
        /// <typeparam name="T">The type of objects to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>A collection of query results</returns>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null);

        /// <summary>
        /// Executes a SQL query and returns a single result
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the SQL query</param>
        /// <returns>A single query result or default if no results</returns>
        Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null);

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        /// <returns>A transaction object</returns>
        Task<IDbTransaction> BeginTransactionAsync();
    }

    /// <summary>
    /// Interface for database transactions
    /// </summary>
    public interface IDbTransaction : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Commits the transaction
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Rolls back the transaction
        /// </summary>
        Task RollbackAsync();
    }
}
