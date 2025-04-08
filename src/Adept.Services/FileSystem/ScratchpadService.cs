using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Services.FileSystem
{
    /// <summary>
    /// Service for managing the scratchpad folder
    /// </summary>
    public class ScratchpadService : IFileSystemService
    {
        private readonly ILogger<ScratchpadService> _logger;
        private readonly IConfigurationService _configService;
        private readonly string _rootDirectory;

        // Standard folder structure
        private static readonly string[] StandardFolders = new[]
        {
            "lessons",
            "generated",
            "temp",
            "notes",
            "projects",
            "resources"
        };

        /// <summary>
        /// Gets the scratchpad root directory
        /// </summary>
        public string ScratchpadDirectory => _rootDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScratchpadService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="configService">The configuration service</param>
        public ScratchpadService(ILogger<ScratchpadService> logger, IConfigurationService configService)
        {
            _logger = logger;
            _configService = configService;

            // Get the scratchpad folder from configuration
            var scratchpadFolder = _configService.GetConfigurationValueAsync("scratchpad_folder", null).GetAwaiter().GetResult();

            // Replace environment variables in the path
            if (!string.IsNullOrEmpty(scratchpadFolder))
            {
                scratchpadFolder = Environment.ExpandEnvironmentVariables(scratchpadFolder);
            }
            else
            {
                // Default to Documents\Adept\Scratchpad if not configured
                scratchpadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Adept", "Scratchpad");
            }

            _rootDirectory = scratchpadFolder;
            _logger.LogInformation("Scratchpad service initialized with root directory: {RootDirectory}", _rootDirectory);

            // Create the root directory if it doesn't exist
            if (!Directory.Exists(_rootDirectory))
            {
                Directory.CreateDirectory(_rootDirectory);
            }

            // Ensure standard folders exist
            EnsureStandardFoldersExistAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ensures the standard folder structure exists
        /// </summary>
        public Task EnsureStandardFoldersExistAsync()
        {
            try
            {
                foreach (var folder in StandardFolders)
                {
                    var folderPath = Path.Combine(_rootDirectory, folder);
                    if (!Directory.Exists(folderPath))
                    {
                        _logger.LogInformation("Creating standard folder: {Folder}", folder);
                        Directory.CreateDirectory(folderPath);
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring standard folders exist");
                throw;
            }
        }

        /// <summary>
        /// Lists files in a directory
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <param name="recursive">Whether to list files recursively</param>
        /// <returns>List of files and directories</returns>
        public Task<(IEnumerable<FileInfo> Files, IEnumerable<DirectoryInfo> Directories)> ListFilesAsync(string path = "", bool recursive = false)
        {
            try
            {
                // Sanitize path
                path = SanitizePath(path);
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the directory exists
                if (!Directory.Exists(fullPath))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {path}");
                }

                // Get files and directories
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(fullPath, "*", searchOption)
                    .Select(f => new FileInfo(f));

                var directories = Directory.GetDirectories(fullPath, "*", searchOption)
                    .Select(d => new DirectoryInfo(d));

                return Task.FromResult((files, directories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                throw;
            }
        }

        /// <summary>
        /// Reads a file
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <returns>The file content</returns>
        public async Task<string> ReadFileAsync(string path)
        {
            try
            {
                // Sanitize path
                path = SanitizePath(path);
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the file exists
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {path}");
                }

                // Read the file
                return await File.ReadAllTextAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file");
                throw;
            }
        }

        /// <summary>
        /// Writes to a file
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <param name="content">The content to write</param>
        /// <param name="append">Whether to append to the file</param>
        /// <returns>Information about the written file</returns>
        public async Task<FileInfo> WriteFileAsync(string path, string content, bool append = false)
        {
            try
            {
                // Sanitize path
                path = SanitizePath(path);
                var fullPath = Path.Combine(_rootDirectory, path);

                // Create the directory if it doesn't exist
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the file
                if (append && File.Exists(fullPath))
                {
                    await File.AppendAllTextAsync(fullPath, content);
                }
                else
                {
                    await File.WriteAllTextAsync(fullPath, content);
                }

                return new FileInfo(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file");
                throw;
            }
        }

        /// <summary>
        /// Deletes a file or directory
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <param name="recursive">Whether to delete directories recursively</param>
        /// <returns>True if the deletion was successful</returns>
        public Task<bool> DeleteAsync(string path, bool recursive = false)
        {
            try
            {
                // Sanitize path
                path = SanitizePath(path);
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the path exists
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File or directory not found: {path}");
                }

                // Delete the file or directory
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                else
                {
                    Directory.Delete(fullPath, recursive);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file or directory");
                throw;
            }
        }

        /// <summary>
        /// Creates a directory
        /// </summary>
        /// <param name="path">The relative path within the scratchpad</param>
        /// <returns>Information about the created directory</returns>
        public Task<DirectoryInfo> CreateDirectoryAsync(string path)
        {
            try
            {
                // Sanitize path
                path = SanitizePath(path);
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the directory already exists
                if (Directory.Exists(fullPath))
                {
                    return Task.FromResult(new DirectoryInfo(fullPath));
                }

                // Create the directory
                var directoryInfo = Directory.CreateDirectory(fullPath);
                return Task.FromResult(directoryInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directory");
                throw;
            }
        }

        /// <summary>
        /// Searches for files by content
        /// </summary>
        /// <param name="searchPattern">The content to search for</param>
        /// <param name="path">The relative path within the scratchpad to search in</param>
        /// <param name="fileExtensions">File extensions to include in the search</param>
        /// <returns>List of files containing the search pattern</returns>
        public async Task<IEnumerable<FileInfo>> SearchFilesByContentAsync(string searchPattern, string path = "", string[]? fileExtensions = null)
        {
            try
            {
                // Sanitize path
                path = SanitizePath(path);
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the directory exists
                if (!Directory.Exists(fullPath))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {path}");
                }

                // Get all files
                var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);

                // Filter by extension if specified
                if (fileExtensions != null && fileExtensions.Length > 0)
                {
                    files = files.Where(f => fileExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)).ToArray();
                }

                // Search for content
                var result = new List<FileInfo>();
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        if (content.Contains(searchPattern, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(new FileInfo(file));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error reading file during content search: {File}", file);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files by content");
                throw;
            }
        }

        /// <summary>
        /// Moves a file or directory
        /// </summary>
        /// <param name="sourcePath">The source path</param>
        /// <param name="destinationPath">The destination path</param>
        /// <returns>Information about the moved file or directory</returns>
        public Task<(string Path, string Type)> MoveAsync(string sourcePath, string destinationPath)
        {
            try
            {
                // Sanitize paths
                sourcePath = SanitizePath(sourcePath);
                destinationPath = SanitizePath(destinationPath);
                var fullSourcePath = Path.Combine(_rootDirectory, sourcePath);
                var fullDestinationPath = Path.Combine(_rootDirectory, destinationPath);

                // Check if the source exists
                if (!File.Exists(fullSourcePath) && !Directory.Exists(fullSourcePath))
                {
                    throw new FileNotFoundException($"Source file or directory not found: {sourcePath}");
                }

                // Create the destination directory if it doesn't exist
                var destinationDir = Path.GetDirectoryName(fullDestinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Move the file or directory
                if (File.Exists(fullSourcePath))
                {
                    File.Move(fullSourcePath, fullDestinationPath, true);
                    return Task.FromResult((destinationPath, "file"));
                }
                else
                {
                    Directory.Move(fullSourcePath, fullDestinationPath);
                    return Task.FromResult((destinationPath, "directory"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file or directory");
                throw;
            }
        }

        /// <summary>
        /// Copies a file or directory
        /// </summary>
        /// <param name="sourcePath">The source path</param>
        /// <param name="destinationPath">The destination path</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>Information about the copied file or directory</returns>
        public Task<(string Path, string Type)> CopyAsync(string sourcePath, string destinationPath, bool overwrite = false)
        {
            try
            {
                // Sanitize paths
                sourcePath = SanitizePath(sourcePath);
                destinationPath = SanitizePath(destinationPath);
                var fullSourcePath = Path.Combine(_rootDirectory, sourcePath);
                var fullDestinationPath = Path.Combine(_rootDirectory, destinationPath);

                // Check if the source exists
                if (!File.Exists(fullSourcePath) && !Directory.Exists(fullSourcePath))
                {
                    throw new FileNotFoundException($"Source file or directory not found: {sourcePath}");
                }

                // Create the destination directory if it doesn't exist
                var destinationDir = Path.GetDirectoryName(fullDestinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Copy the file or directory
                if (File.Exists(fullSourcePath))
                {
                    File.Copy(fullSourcePath, fullDestinationPath, overwrite);
                    return Task.FromResult((destinationPath, "file"));
                }
                else
                {
                    CopyDirectory(fullSourcePath, fullDestinationPath, overwrite);
                    return Task.FromResult((destinationPath, "directory"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file or directory");
                throw;
            }
        }

        /// <summary>
        /// Sanitizes a path
        /// </summary>
        /// <param name="path">The path to sanitize</param>
        /// <returns>The sanitized path</returns>
        private static string SanitizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            // Remove potentially dangerous path traversal
            path = path.Replace("..", "");

            // Remove leading slashes
            path = path.TrimStart('/', '\\');

            return path;
        }

        /// <summary>
        /// Copies a directory
        /// </summary>
        /// <param name="sourceDir">The source directory</param>
        /// <param name="destinationDir">The destination directory</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        private static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite)
        {
            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Copy all files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destinationDir, fileName);
                File.Copy(file, destFile, overwrite);
            }

            // Copy all subdirectories
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(directory);
                var destDir = Path.Combine(destinationDir, dirName);
                CopyDirectory(directory, destDir, overwrite);
            }
        }
    }
}
