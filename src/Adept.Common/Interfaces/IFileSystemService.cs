using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Service for file system operations
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Gets the scratchpad root directory
        /// </summary>
        string ScratchpadDirectory { get; }

        /// <summary>
        /// Ensures the standard folder structure exists
        /// </summary>
        Task EnsureStandardFoldersExistAsync();

        /// <summary>
        /// Lists files in a directory
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <param name="recursive">Whether to list files recursively</param>
        /// <returns>List of files and directories</returns>
        Task<(IEnumerable<FileInfo> Files, IEnumerable<DirectoryInfo> Directories)> ListFilesAsync(string path = "", bool recursive = false);

        /// <summary>
        /// Reads a file
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <returns>The file content</returns>
        Task<string> ReadFileAsync(string path);

        /// <summary>
        /// Writes to a file
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <param name="content">The content to write</param>
        /// <param name="append">Whether to append to the file</param>
        /// <returns>Information about the written file</returns>
        Task<FileInfo> WriteFileAsync(string path, string content, bool append = false);

        /// <summary>
        /// Deletes a file or directory
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <param name="recursive">Whether to delete directories recursively</param>
        /// <returns>True if the deletion was successful</returns>
        Task<bool> DeleteAsync(string path, bool recursive = false);

        /// <summary>
        /// Creates a directory
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <returns>Information about the created directory</returns>
        Task<DirectoryInfo> CreateDirectoryAsync(string path);

        /// <summary>
        /// Searches for files by content
        /// </summary>
        /// <param name="searchPattern">The content to search for</param>
        /// <param name="path">The relative path within the scratchpad to search in</param>
        /// <param name="fileExtensions">File extensions to include in the search</param>
        /// <returns>List of files containing the search pattern</returns>
        Task<IEnumerable<FileInfo>> SearchFilesByContentAsync(string searchPattern, string path = "", string[]? fileExtensions = null);

        /// <summary>
        /// Moves a file or directory
        /// </summary>
        /// <param name="sourcePath">The source path</param>
        /// <param name="destinationPath">The destination path</param>
        /// <returns>Information about the moved file or directory</returns>
        Task<(string Path, string Type)> MoveAsync(string sourcePath, string destinationPath);

        /// <summary>
        /// Copies a file or directory
        /// </summary>
        /// <param name="sourcePath">The source path</param>
        /// <param name="destinationPath">The destination path</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>Information about the copied file or directory</returns>
        Task<(string Path, string Type)> CopyAsync(string sourcePath, string destinationPath, bool overwrite = false);
    }
}
