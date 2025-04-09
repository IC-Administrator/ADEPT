using Adept.FileSystem.Tests.Fixtures;
using Adept.TestUtilities.Helpers;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Adept.FileSystem.Tests.Services
{
    /// <summary>
    /// Integration tests for the FileSystemService
    /// </summary>
    public class FileSystemServiceTests : IClassFixture<FileSystemTestFixture>
    {
        private readonly FileSystemTestFixture _fixture;

        public FileSystemServiceTests(FileSystemTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ReadFileAsync_ExistingFile_ReturnsContent()
        {
            // Arrange
            var filePath = "test.txt";

            // Act
            var content = await _fixture.FileSystemService.ReadFileAsync(filePath);

            // Assert
            Assert.Equal("This is a test text file.", content);
        }

        [Fact]
        public async Task WriteFileAsync_NewFile_CreatesFile()
        {
            // Arrange
            var filePath = "new_file.txt";
            var content = "This is a new file.";

            // Act
            await _fixture.FileSystemService.WriteFileAsync(filePath, content);

            // Assert
            var fullPath = Path.Combine(_fixture.TestDirectory, filePath);
            AssertExtensions.FileExists(fullPath);
            AssertExtensions.FileContains(fullPath, content);
        }

        [Fact]
        public async Task CreateDirectoryAsync_NewDirectory_CreatesDirectory()
        {
            // Arrange
            var directoryName = "new_directory";

            // Act
            var directoryInfo = await _fixture.FileSystemService.CreateDirectoryAsync(directoryName);

            // Assert
            Assert.Equal(directoryName, directoryInfo.Name);
            var fullPath = Path.Combine(_fixture.TestDirectory, directoryName);
            AssertExtensions.DirectoryExists(fullPath);
        }

        [Fact]
        public async Task DeleteAsync_ExistingFile_DeletesFile()
        {
            // Arrange
            var filePath = "file_to_delete.txt";
            await _fixture.FileSystemService.WriteFileAsync(filePath, "This file will be deleted.");
            var fullPath = Path.Combine(_fixture.TestDirectory, filePath);
            AssertExtensions.FileExists(fullPath);

            // Act
            var result = await _fixture.FileSystemService.DeleteAsync(filePath);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public async Task MoveAsync_ExistingFile_MovesFile()
        {
            // Arrange
            var sourceFilePath = "file_to_move.txt";
            var destinationFilePath = "moved_file.txt";
            var content = "This file will be moved.";
            await _fixture.FileSystemService.WriteFileAsync(sourceFilePath, content);
            var sourcePath = Path.Combine(_fixture.TestDirectory, sourceFilePath);
            AssertExtensions.FileExists(sourcePath);

            // Act
            var result = await _fixture.FileSystemService.MoveAsync(sourceFilePath, destinationFilePath);

            // Assert
            Assert.Equal(destinationFilePath, result.Path);
            var destinationPath = Path.Combine(_fixture.TestDirectory, destinationFilePath);
            AssertExtensions.FileExists(destinationPath);
            AssertExtensions.FileContains(destinationPath, content);
            Assert.False(File.Exists(sourcePath));
        }

        [Fact]
        public async Task CopyAsync_ExistingFile_CopiesFile()
        {
            // Arrange
            var sourceFilePath = "file_to_copy.txt";
            var destinationFilePath = "copied_file.txt";
            var content = "This file will be copied.";
            await _fixture.FileSystemService.WriteFileAsync(sourceFilePath, content);
            var sourcePath = Path.Combine(_fixture.TestDirectory, sourceFilePath);
            AssertExtensions.FileExists(sourcePath);

            // Act
            var result = await _fixture.FileSystemService.CopyAsync(sourceFilePath, destinationFilePath);

            // Assert
            Assert.Equal(destinationFilePath, result.Path);
            var destinationPath = Path.Combine(_fixture.TestDirectory, destinationFilePath);
            AssertExtensions.FileExists(destinationPath);
            AssertExtensions.FileContains(destinationPath, content);
            AssertExtensions.FileExists(sourcePath); // Source file should still exist
        }
    }
}
