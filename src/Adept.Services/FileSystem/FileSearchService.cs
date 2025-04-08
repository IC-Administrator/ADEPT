using Adept.Common.Interfaces;
using Adept.Common.Models.FileSystem;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Adept.Services.FileSystem
{
    /// <summary>
    /// Service for searching files in the scratchpad
    /// </summary>
    public class FileSearchService
    {
        private readonly ILogger<FileSearchService> _logger;
        private readonly IFileSystemService _fileSystemService;
        private readonly FileOrganizer _fileOrganizer;
        private readonly MarkdownProcessor _markdownProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearchService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="fileSystemService">The file system service</param>
        /// <param name="fileOrganizer">The file organizer</param>
        /// <param name="markdownProcessor">The markdown processor</param>
        public FileSearchService(
            ILogger<FileSearchService> logger,
            IFileSystemService fileSystemService,
            FileOrganizer fileOrganizer,
            MarkdownProcessor markdownProcessor)
        {
            _logger = logger;
            _fileSystemService = fileSystemService;
            _fileOrganizer = fileOrganizer;
            _markdownProcessor = markdownProcessor;
        }

        /// <summary>
        /// Searches for files by name
        /// </summary>
        /// <param name="searchPattern">The search pattern</param>
        /// <param name="path">The path to search in</param>
        /// <param name="recursive">Whether to search recursively</param>
        /// <returns>List of matching files</returns>
        public async Task<IEnumerable<FileInfo>> SearchFilesByNameAsync(string searchPattern, string path = "", bool recursive = true)
        {
            try
            {
                // Get the full path
                var fullPath = Path.Combine(_fileSystemService.ScratchpadDirectory, path);

                // Check if the directory exists
                if (!Directory.Exists(fullPath))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {path}");
                }

                // Get all files
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(fullPath, $"*{searchPattern}*", searchOption)
                    .Select(f => new FileInfo(f));

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files by name: {SearchPattern}", searchPattern);
                throw;
            }
        }

        /// <summary>
        /// Searches for files by content
        /// </summary>
        /// <param name="searchText">The text to search for</param>
        /// <param name="path">The path to search in</param>
        /// <param name="fileExtensions">File extensions to include in the search</param>
        /// <returns>List of matching files with context</returns>
        public async Task<IEnumerable<(FileInfo File, string Context)>> SearchFilesByContentWithContextAsync(
            string searchText, string path = "", string[] fileExtensions = null)
        {
            try
            {
                // Get matching files
                var matchingFiles = await _fileSystemService.SearchFilesByContentAsync(searchText, path, fileExtensions);
                var result = new List<(FileInfo, string)>();

                // Extract context for each file
                foreach (var file in matchingFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file.FullName);
                        var context = ExtractSearchContext(content, searchText);
                        result.Add((file, context));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error extracting context from file: {File}", file.FullName);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files by content: {SearchText}", searchText);
                throw;
            }
        }

        /// <summary>
        /// Searches for markdown files by metadata
        /// </summary>
        /// <param name="metadataKey">The metadata key to search for</param>
        /// <param name="metadataValue">The metadata value to search for</param>
        /// <param name="path">The path to search in</param>
        /// <returns>List of matching markdown files</returns>
        public async Task<IEnumerable<MarkdownMetadata>> SearchMarkdownByMetadataAsync(
            string metadataKey, string metadataValue, string path = "")
        {
            try
            {
                // Get the full path
                var fullPath = Path.Combine(_fileSystemService.ScratchpadDirectory, path);

                // Check if the directory exists
                if (!Directory.Exists(fullPath))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {path}");
                }

                // Get all markdown files
                var files = Directory.GetFiles(fullPath, "*.md", SearchOption.AllDirectories);
                var result = new List<MarkdownMetadata>();

                foreach (var file in files)
                {
                    try
                    {
                        var metadata = await _markdownProcessor.ExtractMetadataAsync(file);

                        // Check if the metadata matches
                        bool isMatch = false;
                        if (string.Equals(metadataKey, "title", StringComparison.OrdinalIgnoreCase) && 
                            !string.IsNullOrEmpty(metadata.Title) && 
                            metadata.Title.Contains(metadataValue, StringComparison.OrdinalIgnoreCase))
                        {
                            isMatch = true;
                        }
                        else if (string.Equals(metadataKey, "description", StringComparison.OrdinalIgnoreCase) && 
                                !string.IsNullOrEmpty(metadata.Description) && 
                                metadata.Description.Contains(metadataValue, StringComparison.OrdinalIgnoreCase))
                        {
                            isMatch = true;
                        }
                        else if (string.Equals(metadataKey, "author", StringComparison.OrdinalIgnoreCase) && 
                                !string.IsNullOrEmpty(metadata.Author) && 
                                metadata.Author.Contains(metadataValue, StringComparison.OrdinalIgnoreCase))
                        {
                            isMatch = true;
                        }
                        else if (string.Equals(metadataKey, "tags", StringComparison.OrdinalIgnoreCase) || 
                                string.Equals(metadataKey, "keywords", StringComparison.OrdinalIgnoreCase))
                        {
                            isMatch = metadata.Keywords.Any(k => k.Contains(metadataValue, StringComparison.OrdinalIgnoreCase));
                        }
                        else if (metadata.Properties.TryGetValue(metadataKey, out var value) && 
                                value.Contains(metadataValue, StringComparison.OrdinalIgnoreCase))
                        {
                            isMatch = true;
                        }

                        if (isMatch)
                        {
                            result.Add(metadata);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing markdown file: {File}", file);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching markdown by metadata: {MetadataKey}={MetadataValue}", metadataKey, metadataValue);
                throw;
            }
        }

        /// <summary>
        /// Searches for files by tag
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        /// <param name="path">The path to search in</param>
        /// <returns>List of matching files</returns>
        public async Task<IEnumerable<FileMetadata>> SearchFilesByTagAsync(string tag, string path = "")
        {
            try
            {
                // For now, this is just a wrapper around SearchMarkdownByMetadataAsync
                // In a more complete implementation, this would search a tag database
                var markdownFiles = await SearchMarkdownByMetadataAsync("tags", tag, path);
                return markdownFiles.Cast<FileMetadata>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files by tag: {Tag}", tag);
                throw;
            }
        }

        /// <summary>
        /// Extracts context around search matches
        /// </summary>
        /// <param name="content">The file content</param>
        /// <param name="searchText">The search text</param>
        /// <param name="contextLength">The number of characters to include before and after the match</param>
        /// <returns>The extracted context</returns>
        private string ExtractSearchContext(string content, string searchText, int contextLength = 100)
        {
            try
            {
                var matches = Regex.Matches(content, Regex.Escape(searchText), RegexOptions.IgnoreCase);
                if (matches.Count == 0)
                {
                    return string.Empty;
                }

                var contexts = new List<string>();
                foreach (Match match in matches.Take(3)) // Limit to 3 matches
                {
                    var startIndex = Math.Max(0, match.Index - contextLength);
                    var endIndex = Math.Min(content.Length, match.Index + match.Length + contextLength);
                    var length = endIndex - startIndex;

                    var context = content.Substring(startIndex, length);
                    
                    // Add ellipsis if we're not at the beginning or end
                    if (startIndex > 0)
                    {
                        context = "..." + context;
                    }
                    
                    if (endIndex < content.Length)
                    {
                        context = context + "...";
                    }

                    contexts.Add(context);
                }

                return string.Join("\n\n", contexts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting search context");
                return string.Empty;
            }
        }
    }
}
