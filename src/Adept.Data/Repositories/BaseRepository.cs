using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Data.Repositories
{
    /// <summary>
    /// Base repository class with common functionality for all repositories
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public abstract class BaseRepository<T> where T : class
    {
        /// <summary>
        /// The database context
        /// </summary>
        protected readonly IDatabaseContext DatabaseContext;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository{T}"/> class
        /// </summary>
        /// <param name="databaseContext">The database context</param>
        /// <param name="logger">The logger</param>
        protected BaseRepository(IDatabaseContext databaseContext, ILogger logger)
        {
            DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a query with error handling
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="errorMessage">The error message</param>
        /// <param name="defaultValue">The default value to return on error</param>
        /// <returns>The result of the operation or the default value on error</returns>
        protected async Task<TResult> ExecuteWithErrorHandlingAsync<TResult>(
            Func<Task<TResult>> operation,
            string errorMessage,
            TResult defaultValue = default)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, errorMessage);
                return defaultValue;
            }
        }

        /// <summary>
        /// Executes a query with error handling and throws the exception
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="errorMessage">The error message</param>
        /// <returns>The result of the operation</returns>
        protected async Task<TResult> ExecuteWithErrorHandlingAndThrowAsync<TResult>(
            Func<Task<TResult>> operation,
            string errorMessage)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, errorMessage);
                throw;
            }
        }

        /// <summary>
        /// Executes a command with error handling
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="errorMessage">The error message</param>
        protected async Task ExecuteWithErrorHandlingAsync(
            Func<Task> operation,
            string errorMessage)
        {
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, errorMessage);
                throw;
            }
        }

        /// <summary>
        /// Executes a command with transaction and error handling
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="errorMessage">The error message</param>
        /// <returns>The result of the operation</returns>
        protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<IDbTransaction, Task<TResult>> operation,
            string errorMessage)
        {
            if (DatabaseContext == null)
            {
                throw new InvalidOperationException("Database context is null");
            }

            using var transaction = await DatabaseContext.BeginTransactionAsync();
            if (transaction == null)
            {
                throw new InvalidOperationException("Failed to begin transaction");
            }

            try
            {
                var result = await operation(transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Logger.LogError(ex, errorMessage);
                throw;
            }
        }

        /// <summary>
        /// Executes a command with transaction and error handling
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="errorMessage">The error message</param>
        protected async Task ExecuteInTransactionAsync(
            Func<IDbTransaction, Task> operation,
            string errorMessage)
        {
            if (DatabaseContext == null)
            {
                throw new InvalidOperationException("Database context is null");
            }

            using var transaction = await DatabaseContext.BeginTransactionAsync();
            if (transaction == null)
            {
                throw new InvalidOperationException("Failed to begin transaction");
            }

            try
            {
                await operation(transaction);
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Logger.LogError(ex, errorMessage);
                throw;
            }
        }

        /// <summary>
        /// Validates that an entity is not null
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <param name="entityName">The name of the entity</param>
        protected void ValidateEntityNotNull(T entity, string entityName)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(entityName, $"The {entityName} cannot be null");
            }
        }

        /// <summary>
        /// Validates that a string property is not null or empty
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="propertyName">The name of the property</param>
        protected void ValidateStringNotNullOrEmpty(string value, string propertyName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"The {propertyName} cannot be null or empty", propertyName);
            }
        }

        /// <summary>
        /// Validates that an ID is not null, empty, or whitespace
        /// </summary>
        /// <param name="id">The ID to validate</param>
        /// <param name="entityName">The name of the entity</param>
        protected void ValidateId(string id, string entityName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"The {entityName} ID cannot be null or empty", nameof(id));
            }
        }
    }
}
