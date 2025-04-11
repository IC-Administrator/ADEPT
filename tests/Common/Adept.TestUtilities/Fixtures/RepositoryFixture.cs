using Adept.Common.Interfaces;
using Adept.Core.Models;
using Adept.TestUtilities.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

// Use alias to avoid ambiguity with Moq.MockFactory
using TestMockFactory = Adept.TestUtilities.Helpers.MockFactory;

namespace Adept.TestUtilities.Fixtures
{
    /// <summary>
    /// A fixture for repository tests that provides a mock database context
    /// </summary>
    public class RepositoryFixture : IAsyncLifetime, IDisposable
    {
        public Mock<IDatabaseContext> MockDatabaseContext { get; }
        public Mock<ILogger<object>> MockLogger { get; }

        // Test data
        public List<SystemPrompt> SystemPrompts { get; }
        public List<LessonResource> LessonResources { get; }
        public List<LessonTemplate> LessonTemplates { get; }

        public RepositoryFixture()
        {
            // Create mocks
            MockDatabaseContext = TestMockFactory.CreateMockDatabaseContext();
            MockLogger = new Mock<ILogger<object>>();

            // Create test data
            SystemPrompts = TestDataGenerator.GenerateRandomSystemPrompts(5);
            LessonResources = TestDataGenerator.GenerateRandomLessonResources(5);
            LessonTemplates = TestDataGenerator.GenerateRandomLessonTemplates(5);
        }

        /// <summary>
        /// Create a mock logger for the specified type
        /// </summary>
        /// <typeparam name="T">The type that the logger is for</typeparam>
        /// <returns>A mock logger</returns>
        public Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return TestMockFactory.CreateMockLogger<T>();
        }

        /// <summary>
        /// Setup the mock database context to return the specified results for a query
        /// </summary>
        /// <typeparam name="T">The type of the results</typeparam>
        /// <param name="sql">The SQL query to match</param>
        /// <param name="results">The results to return</param>
        public void SetupQueryAsync<T>(string sql, IEnumerable<T> results)
        {
            MockDatabaseContext.Setup(db => db.QueryAsync<T>(
                    It.Is<string>(s => s.Contains(sql)),
                    It.IsAny<object>()))
                .ReturnsAsync(results);
        }

        /// <summary>
        /// Setup the mock database context to return the specified result for a single query
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="sql">The SQL query to match</param>
        /// <param name="result">The result to return</param>
        public void SetupQuerySingleOrDefaultAsync<T>(string sql, T result)
        {
            MockDatabaseContext.Setup(db => db.QuerySingleOrDefaultAsync<T>(
                    It.Is<string>(s => s.Contains(sql)),
                    It.IsAny<object>()))
                .ReturnsAsync(result);
        }

        /// <summary>
        /// Setup the mock database context to return the specified result for an execute
        /// </summary>
        /// <param name="sql">The SQL query to match</param>
        /// <param name="result">The result to return</param>
        public void SetupExecuteAsync(string sql, int result)
        {
            MockDatabaseContext.Setup(db => db.ExecuteNonQueryAsync(
                    It.Is<string>(s => s.Contains(sql)),
                    It.IsAny<object>()))
                .ReturnsAsync(result);
        }

        /// <summary>
        /// Setup the mock database context to return the specified result for a scalar query
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="sql">The SQL query to match</param>
        /// <param name="result">The result to return</param>
        public void SetupExecuteScalarAsync<T>(string sql, T result)
        {
            MockDatabaseContext.Setup(db => db.ExecuteScalarAsync<T>(
                    It.Is<string>(s => s.Contains(sql)),
                    It.IsAny<object>()))
                .ReturnsAsync(result);
        }

        /// <summary>
        /// Initialize the fixture
        /// </summary>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup the fixture
        /// </summary>
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
