using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Adept.TestUtilities.Fixtures
{
    /// <summary>
    /// A fixture for database tests that provides a shared database context
    /// </summary>
    public class DatabaseFixture : IAsyncLifetime, IDisposable
    {
        public string DatabasePath { get; }

        public DatabaseFixture()
        {
            // Create a unique test database path
            DatabasePath = Path.Combine(
                Path.GetTempPath(), 
                $"adept_test_fixture_{Guid.NewGuid():N}.db");
        }

        /// <summary>
        /// Initialize the database
        /// </summary>
        public virtual Task InitializeAsync()
        {
            // Override in derived classes to initialize the database
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup the database
        /// </summary>
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (File.Exists(DatabasePath))
                {
                    File.Delete(DatabasePath);
                }
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }
        }
    }
}
