using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;

namespace Adept.Data.Database
{
    /// <summary>
    /// Interface for database connection provider
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Creates a new SQLite connection
        /// </summary>
        /// <returns>A SQLite connection</returns>
        SqliteConnection CreateConnection();

        /// <summary>
        /// Gets the connection string
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Initializes the database
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Checks if the database exists
        /// </summary>
        /// <returns>True if the database exists, false otherwise</returns>
        bool DatabaseExists();

        /// <summary>
        /// Gets the database file path
        /// </summary>
        string DatabasePath { get; }
    }
}
