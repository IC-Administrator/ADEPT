using Adept.Common.Interfaces;
using Adept.Common.Models.FileSystem;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Adept.Services.FileSystem
{
    /// <summary>
    /// Service for organizing files in the scratchpad
    /// </summary>
    public class FileOrganizer
    {
        private readonly ILogger<FileOrganizer> _logger;
        private readonly IFileSystemService _fileSystemService;
        private readonly MarkdownProcessor _markdownProcessor;

        // File categories based on extensions
        private static readonly Dictionary<string, string> FileCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".md", "Document" },
            { ".txt", "Document" },
            { ".pdf", "Document" },
            { ".docx", "Document" },
            { ".xlsx", "Spreadsheet" },
            { ".csv", "Data" },
            { ".json", "Data" },
            { ".xml", "Data" },
            { ".jpg", "Image" },
            { ".jpeg", "Image" },
            { ".png", "Image" },
            { ".gif", "Image" },
            { ".mp3", "Audio" },
            { ".wav", "Audio" },
            { ".mp4", "Video" },
            { ".mov", "Video" },
            { ".zip", "Archive" },
            { ".rar", "Archive" },
            { ".7z", "Archive" },
            { ".exe", "Application" },
            { ".dll", "Application" },
            { ".html", "Web" },
            { ".css", "Web" },
            { ".js", "Web" },
            { ".py", "Code" },
            { ".cs", "Code" },
            { ".java", "Code" },
            { ".cpp", "Code" },
            { ".h", "Code" },
            { ".c", "Code" },
            { ".sql", "Code" }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="FileOrganizer"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="fileSystemService">The file system service</param>
        /// <param name="markdownProcessor">The markdown processor</param>
        public FileOrganizer(
            ILogger<FileOrganizer> logger,
            IFileSystemService fileSystemService,
            MarkdownProcessor markdownProcessor)
        {
            _logger = logger;
            _fileSystemService = fileSystemService;
            _markdownProcessor = markdownProcessor;
        }

        /// <summary>
        /// Gets metadata for a file
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>The file metadata</returns>
        public async Task<FileMetadata> GetFileMetadataAsync(string filePath)
        {
            try
            {
                // Get the full path
                var fullPath = Path.Combine(_fileSystemService.ScratchpadDirectory, filePath);

                // Check if the file exists
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var fileInfo = new FileInfo(fullPath);
                var extension = fileInfo.Extension.ToLowerInvariant();

                // For markdown files, use the markdown processor
                if (extension == ".md")
                {
                    return await _markdownProcessor.ExtractMetadataAsync(fullPath);
                }

                // For other files, create basic metadata
                var metadata = new FileMetadata
                {
                    FileName = fileInfo.Name,
                    RelativePath = filePath,
                    FullPath = fullPath,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    Extension = extension,
                    FileType = GetFileType(extension),
                    Category = GetFileCategory(extension)
                };

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file metadata: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Organizes files by moving them to appropriate folders based on their type
        /// </summary>
        /// <param name="sourcePath">The source path to organize</param>
        /// <param name="organizationType">The organization type (category, extension, date)</param>
        /// <returns>Number of files organized</returns>
        public async Task<int> OrganizeFilesByTypeAsync(string sourcePath, string organizationType = "category")
        {
            try
            {
                // Get the full source path
                var fullSourcePath = Path.Combine(_fileSystemService.ScratchpadDirectory, sourcePath);

                // Check if the source exists
                if (!Directory.Exists(fullSourcePath))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {sourcePath}");
                }

                // Get all files
                var files = Directory.GetFiles(fullSourcePath, "*", SearchOption.TopDirectoryOnly);
                int organizedCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var extension = fileInfo.Extension.ToLowerInvariant();
                        string targetFolder;

                        switch (organizationType.ToLowerInvariant())
                        {
                            case "category":
                                targetFolder = GetFileCategory(extension);
                                break;
                            case "extension":
                                targetFolder = extension.TrimStart('.');
                                break;
                            case "date":
                                targetFolder = fileInfo.LastWriteTime.ToString("yyyy-MM");
                                break;
                            default:
                                targetFolder = "Other";
                                break;
                        }

                        // Create target directory if it doesn't exist
                        var targetPath = Path.Combine(fullSourcePath, targetFolder);
                        if (!Directory.Exists(targetPath))
                        {
                            Directory.CreateDirectory(targetPath);
                        }

                        // Move the file
                        var targetFilePath = Path.Combine(targetPath, fileInfo.Name);
                        if (File.Exists(targetFilePath))
                        {
                            // Add a timestamp to avoid overwriting
                            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                            var newFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                            targetFilePath = Path.Combine(targetPath, newFileName);
                        }

                        File.Move(file, targetFilePath);
                        organizedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error organizing file: {File}", file);
                    }
                }

                return organizedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error organizing files by type: {SourcePath}", sourcePath);
                throw;
            }
        }

        /// <summary>
        /// Tags a file with keywords
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <param name="tags">The tags to add</param>
        /// <returns>True if the tagging was successful</returns>
        public async Task<bool> TagFileAsync(string filePath, IEnumerable<string> tags)
        {
            try
            {
                // Get the full path
                var fullPath = Path.Combine(_fileSystemService.ScratchpadDirectory, filePath);

                // Check if the file exists
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var extension = Path.GetExtension(fullPath).ToLowerInvariant();

                // For markdown files, update the front matter
                if (extension == ".md")
                {
                    var content = await File.ReadAllTextAsync(fullPath);
                    var tagString = string.Join(", ", tags);
                    
                    var updatedContent = _markdownProcessor.UpdateFrontMatter(content, new Dictionary<string, string>
                    {
                        { "tags", tagString }
                    });

                    await File.WriteAllTextAsync(fullPath, updatedContent);
                    return true;
                }

                // For other files, we could implement a separate tagging system
                // This would typically involve a database or metadata file
                _logger.LogWarning("Tagging non-markdown files is not yet implemented");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tagging file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Gets the file type based on extension
        /// </summary>
        /// <param name="extension">The file extension</param>
        /// <returns>The file type</returns>
        private static string GetFileType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return "Unknown";
            }

            switch (extension.ToLowerInvariant())
            {
                case ".md":
                    return "Markdown";
                case ".txt":
                    return "Text";
                case ".pdf":
                    return "PDF";
                case ".docx":
                case ".doc":
                    return "Word";
                case ".xlsx":
                case ".xls":
                    return "Excel";
                case ".pptx":
                case ".ppt":
                    return "PowerPoint";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    return "Image";
                case ".mp3":
                case ".wav":
                case ".ogg":
                case ".flac":
                    return "Audio";
                case ".mp4":
                case ".avi":
                case ".mov":
                case ".wmv":
                    return "Video";
                case ".zip":
                case ".rar":
                case ".7z":
                    return "Archive";
                case ".html":
                case ".htm":
                    return "HTML";
                case ".css":
                    return "CSS";
                case ".js":
                    return "JavaScript";
                case ".json":
                    return "JSON";
                case ".xml":
                    return "XML";
                case ".csv":
                    return "CSV";
                case ".py":
                    return "Python";
                case ".cs":
                    return "CSharp";
                case ".java":
                    return "Java";
                case ".cpp":
                case ".c":
                case ".h":
                    return "C/C++";
                default:
                    return "Other";
            }
        }

        /// <summary>
        /// Gets the file category based on extension
        /// </summary>
        /// <param name="extension">The file extension</param>
        /// <returns>The file category</returns>
        private static string GetFileCategory(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return "Other";
            }

            if (FileCategories.TryGetValue(extension, out var category))
            {
                return category;
            }

            return "Other";
        }
    }
}
