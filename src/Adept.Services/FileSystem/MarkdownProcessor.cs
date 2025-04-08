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
    /// Processor for markdown files
    /// </summary>
    public class MarkdownProcessor
    {
        private readonly ILogger<MarkdownProcessor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownProcessor"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public MarkdownProcessor(ILogger<MarkdownProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Extracts metadata from a markdown file
        /// </summary>
        /// <param name="filePath">The path to the markdown file</param>
        /// <returns>The extracted metadata</returns>
        public async Task<MarkdownMetadata> ExtractMetadataAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var content = await File.ReadAllTextAsync(filePath);
                var fileInfo = new FileInfo(filePath);

                var metadata = new MarkdownMetadata
                {
                    FileName = fileInfo.Name,
                    RelativePath = GetRelativePath(filePath),
                    FullPath = filePath,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    Extension = fileInfo.Extension,
                    FileType = "markdown"
                };

                // Extract front matter if present
                var frontMatter = ExtractFrontMatter(content);
                if (frontMatter.Count > 0)
                {
                    if (frontMatter.TryGetValue("title", out var title))
                    {
                        metadata.Title = title;
                    }

                    if (frontMatter.TryGetValue("description", out var description))
                    {
                        metadata.Description = description;
                    }

                    if (frontMatter.TryGetValue("author", out var author))
                    {
                        metadata.Author = author;
                    }

                    if (frontMatter.TryGetValue("date", out var dateStr))
                    {
                        if (DateTime.TryParse(dateStr, out var date))
                        {
                            metadata.CreationDate = date;
                        }
                    }

                    if (frontMatter.TryGetValue("tags", out var tagsStr) || frontMatter.TryGetValue("keywords", out tagsStr))
                    {
                        metadata.Keywords = tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .ToList();
                        metadata.Tags = new List<string>(metadata.Keywords);
                    }

                    // Add all other front matter properties
                    foreach (var kvp in frontMatter)
                    {
                        if (!metadata.Properties.ContainsKey(kvp.Key))
                        {
                            metadata.Properties[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // If no title in front matter, try to extract from first heading
                if (string.IsNullOrEmpty(metadata.Title))
                {
                    metadata.Title = ExtractTitleFromContent(content);
                }

                // Extract headings
                metadata.Headings = ExtractHeadings(content);

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting metadata from markdown file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Extracts front matter from markdown content
        /// </summary>
        /// <param name="content">The markdown content</param>
        /// <returns>Dictionary of front matter properties</returns>
        public Dictionary<string, string> ExtractFrontMatter(string content)
        {
            var frontMatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Check for YAML front matter (between --- delimiters)
                var yamlMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
                if (yamlMatch.Success)
                {
                    var yamlContent = yamlMatch.Groups[1].Value;
                    var lines = yamlContent.Split('\n');

                    foreach (var line in lines)
                    {
                        var keyValueMatch = Regex.Match(line.Trim(), @"^([^:]+):\s*(.*)$");
                        if (keyValueMatch.Success)
                        {
                            var key = keyValueMatch.Groups[1].Value.Trim().ToLowerInvariant();
                            var value = keyValueMatch.Groups[2].Value.Trim();

                            // Remove quotes if present
                            if (value.StartsWith("\"") && value.EndsWith("\""))
                            {
                                value = value.Substring(1, value.Length - 2);
                            }

                            frontMatter[key] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting front matter from markdown content");
            }

            return frontMatter;
        }

        /// <summary>
        /// Extracts the title from markdown content
        /// </summary>
        /// <param name="content">The markdown content</param>
        /// <returns>The extracted title</returns>
        public string ExtractTitleFromContent(string content)
        {
            try
            {
                // Look for the first heading
                var headingMatch = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
                if (headingMatch.Success)
                {
                    return headingMatch.Groups[1].Value.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting title from markdown content");
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts headings from markdown content
        /// </summary>
        /// <param name="content">The markdown content</param>
        /// <returns>List of headings</returns>
        public List<string> ExtractHeadings(string content)
        {
            var headings = new List<string>();

            try
            {
                // Match all headings (# Heading)
                var matches = Regex.Matches(content, @"^(#{1,6})\s+(.+)$", RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    var level = match.Groups[1].Value.Length;
                    var text = match.Groups[2].Value.Trim();
                    headings.Add($"H{level}: {text}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting headings from markdown content");
            }

            return headings;
        }

        /// <summary>
        /// Gets the relative path from a full path
        /// </summary>
        /// <param name="fullPath">The full path</param>
        /// <returns>The relative path</returns>
        private string GetRelativePath(string fullPath)
        {
            try
            {
                // This is a simplified implementation
                // In a real implementation, this would use Path.GetRelativePath
                // relative to the scratchpad directory
                return Path.GetFileName(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting relative path");
                return Path.GetFileName(fullPath);
            }
        }

        /// <summary>
        /// Adds or updates front matter in markdown content
        /// </summary>
        /// <param name="content">The original markdown content</param>
        /// <param name="properties">The properties to add or update</param>
        /// <returns>The updated markdown content</returns>
        public string UpdateFrontMatter(string content, Dictionary<string, string> properties)
        {
            try
            {
                // Check if front matter exists
                var yamlMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
                if (yamlMatch.Success)
                {
                    // Extract existing front matter
                    var existingFrontMatter = ExtractFrontMatter(content);
                    
                    // Update with new properties
                    foreach (var property in properties)
                    {
                        existingFrontMatter[property.Key] = property.Value;
                    }

                    // Build new front matter
                    var newFrontMatter = "---\n";
                    foreach (var property in existingFrontMatter)
                    {
                        newFrontMatter += $"{property.Key}: {property.Value}\n";
                    }
                    newFrontMatter += "---\n\n";

                    // Replace old front matter with new one
                    return content.Replace(yamlMatch.Value, newFrontMatter);
                }
                else
                {
                    // Create new front matter
                    var newFrontMatter = "---\n";
                    foreach (var property in properties)
                    {
                        newFrontMatter += $"{property.Key}: {property.Value}\n";
                    }
                    newFrontMatter += "---\n\n";

                    // Add to the beginning of the content
                    return newFrontMatter + content;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating front matter");
                return content;
            }
        }
    }
}
