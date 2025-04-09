using System;
using System.IO;
using Xunit;

namespace Adept.TestUtilities.Fixtures
{
    /// <summary>
    /// A fixture for file system tests that provides a temporary directory
    /// </summary>
    public class FileSystemFixture : IDisposable
    {
        public string TestDirectory { get; }

        public FileSystemFixture()
        {
            // Create a unique test directory
            TestDirectory = Path.Combine(
                Path.GetTempPath(), 
                $"adept_test_{Guid.NewGuid():N}");
            
            // Ensure the directory exists
            Directory.CreateDirectory(TestDirectory);
        }

        /// <summary>
        /// Create a test file with the specified content
        /// </summary>
        /// <param name="fileName">The name of the file to create</param>
        /// <param name="content">The content to write to the file</param>
        /// <returns>The full path to the created file</returns>
        public string CreateTestFile(string fileName, string content)
        {
            string filePath = Path.Combine(TestDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Create a test directory
        /// </summary>
        /// <param name="directoryName">The name of the directory to create</param>
        /// <returns>The full path to the created directory</returns>
        public string CreateTestDirectory(string directoryName)
        {
            string directoryPath = Path.Combine(TestDirectory, directoryName);
            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(TestDirectory))
                {
                    Directory.Delete(TestDirectory, true);
                }
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }
        }
    }
}
