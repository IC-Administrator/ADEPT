using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Adept.TestUtilities.TestBase
{
    /// <summary>
    /// Base class for database tests that provides common setup and teardown functionality
    /// </summary>
    public abstract class DatabaseTestBase : IntegrationTestBase
    {
        protected readonly string TestDatabasePath;

        protected DatabaseTestBase()
        {
            // Create a unique test database path
            TestDatabasePath = Path.Combine(
                Path.GetTempPath(), 
                $"adept_test_{Guid.NewGuid():N}.db");
        }

        /// <summary>
        /// Initialize the test database
        /// </summary>
        protected abstract Task InitializeDatabaseAsync();

        /// <summary>
        /// Cleanup the test database
        /// </summary>
        protected virtual void CleanupDatabase()
        {
            try
            {
                if (File.Exists(TestDatabasePath))
                {
                    File.Delete(TestDatabasePath);
                }
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Dispose()
        {
            CleanupDatabase();
            base.Dispose();
        }
    }
}
