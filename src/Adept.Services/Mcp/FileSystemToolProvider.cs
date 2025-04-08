using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Services.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Adept.Services.Mcp
{
    /// <summary>
    /// Provider for file system tools
    /// </summary>
    public class FileSystemToolProvider : IMcpToolProvider
    {
        private readonly ILogger<FileSystemToolProvider> _logger;
        private readonly List<IMcpTool> _tools = new List<IMcpTool>();
        private readonly string _rootDirectory;
        private readonly IFileSystemService _fileSystemService;
        private readonly MarkdownProcessor _markdownProcessor;
        private readonly FileOrganizer _fileOrganizer;
        private readonly FileSearchService _fileSearchService;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "FileSystem";

        /// <summary>
        /// Gets the available tools
        /// </summary>
        public IEnumerable<IMcpTool> Tools => _tools;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemToolProvider"/> class
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        /// <param name="fileSystemService">The file system service</param>
        /// <param name="markdownProcessor">The markdown processor</param>
        /// <param name="fileOrganizer">The file organizer</param>
        /// <param name="fileSearchService">The file search service</param>
        public FileSystemToolProvider(
            IConfiguration configuration,
            ILogger<FileSystemToolProvider> logger,
            IFileSystemService fileSystemService,
            MarkdownProcessor markdownProcessor,
            FileOrganizer fileOrganizer,
            FileSearchService fileSearchService)
        {
            _logger = logger;
            _fileSystemService = fileSystemService;
            _markdownProcessor = markdownProcessor;
            _fileOrganizer = fileOrganizer;
            _fileSearchService = fileSearchService;

            _rootDirectory = _fileSystemService.ScratchpadDirectory;
            _logger.LogInformation("FileSystem tool provider initialized with root directory: {RootDirectory}", _rootDirectory);

            // Initialize tools
            _tools.Add(new ListFilesTool(_rootDirectory, _logger));
            _tools.Add(new ReadFileTool(_rootDirectory, _logger));
            _tools.Add(new WriteFileTool(_rootDirectory, _logger));
            _tools.Add(new DeleteFileTool(_rootDirectory, _logger));
            _tools.Add(new CreateDirectoryTool(_rootDirectory, _logger));

            // Add new tools
            _tools.Add(new SearchFilesTool(_rootDirectory, _logger, _fileSearchService));
            _tools.Add(new MoveFileTool(_rootDirectory, _logger, _fileSystemService));
            _tools.Add(new CopyFileTool(_rootDirectory, _logger, _fileSystemService));
            _tools.Add(new GetMarkdownMetadataTool(_rootDirectory, _logger, _markdownProcessor));
            _tools.Add(new OrganizeFilesTool(_rootDirectory, _logger, _fileOrganizer));
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public Task InitializeAsync()
        {
            _logger.LogInformation("FileSystem tool provider initialized with root directory: {RootDirectory}", _rootDirectory);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a tool by name
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <returns>The tool or null if not found</returns>
        public IMcpTool? GetTool(string toolName)
        {
            return _tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Executes a tool
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
        {
            var tool = GetTool(toolName);
            if (tool == null)
            {
                return McpToolResult.Error($"Tool {toolName} not found");
            }

            try
            {
                return await tool.ExecuteAsync(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
                return McpToolResult.Error($"Error executing tool: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the tool schema for all available tools
        /// </summary>
        /// <returns>The tool schema</returns>
        public IEnumerable<McpToolSchema> GetToolSchema()
        {
            return _tools.Select(t => t.Schema);
        }
    }

    /// <summary>
    /// Tool for listing files in a directory
    /// </summary>
    public class ListFilesTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_list_files";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Lists files and directories in a specified directory";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path to list files from (relative to the root directory)",
                    Required = false,
                    DefaultValue = ""
                },
                ["recursive"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to list files recursively",
                    Required = false,
                    DefaultValue = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "List of files and directories"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ListFilesTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        public ListFilesTool(string rootDirectory, ILogger logger)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                var path = parameters.TryGetValue("path", out var pathObj) && pathObj != null
                    ? pathObj.ToString() ?? ""
                    : "";
                var recursive = parameters.TryGetValue("recursive", out var recursiveObj) && recursiveObj != null
                    ? Convert.ToBoolean(recursiveObj)
                    : false;

                // Sanitize path
                path = path.Replace("..", "").TrimStart('/').TrimStart('\\');
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the directory exists
                if (!Directory.Exists(fullPath))
                {
                    return Task.FromResult(McpToolResult.Error($"Directory not found: {path}"));
                }

                // Get files and directories
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(fullPath, "*", searchOption)
                    .Select(f => new
                    {
                        name = Path.GetFileName(f),
                        path = f.Replace(_rootDirectory, "").Replace("\\", "/").TrimStart('/'),
                        type = "file",
                        size = new FileInfo(f).Length,
                        lastModified = File.GetLastWriteTime(f)
                    });

                var directories = Directory.GetDirectories(fullPath, "*", searchOption)
                    .Select(d => new
                    {
                        name = Path.GetFileName(d),
                        path = d.Replace(_rootDirectory, "").Replace("\\", "/").TrimStart('/'),
                        type = "directory",
                        lastModified = Directory.GetLastWriteTime(d)
                    });

                var result = new
                {
                    path,
                    files = files.ToArray(),
                    directories = directories.ToArray()
                };

                return Task.FromResult(McpToolResult.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                return Task.FromResult(McpToolResult.Error($"Error listing files: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Tool for reading a file
    /// </summary>
    public class ReadFileTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_read_file";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Reads the content of a file";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path of the file to read (relative to the root directory)",
                    Required = true
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "File content and metadata"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadFileTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        public ReadFileTool(string rootDirectory, ILogger logger)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("path", out var pathObj) || pathObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("Path parameter is required"));
                }

                var path = pathObj.ToString() ?? "";

                // Sanitize path
                path = path.Replace("..", "").TrimStart('/').TrimStart('\\');
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the file exists
                if (!File.Exists(fullPath))
                {
                    return Task.FromResult(McpToolResult.Error($"File not found: {path}"));
                }

                // Read the file
                var content = File.ReadAllText(fullPath);
                var fileInfo = new FileInfo(fullPath);

                var result = new
                {
                    path,
                    content,
                    size = fileInfo.Length,
                    lastModified = fileInfo.LastWriteTime
                };

                return Task.FromResult(McpToolResult.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file");
                return Task.FromResult(McpToolResult.Error($"Error reading file: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Tool for writing to a file
    /// </summary>
    public class WriteFileTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_write_file";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Writes content to a file";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path of the file to write (relative to the root directory)",
                    Required = true
                },
                ["content"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The content to write to the file",
                    Required = true
                },
                ["append"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to append to the file instead of overwriting it",
                    Required = false,
                    DefaultValue = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Result of the write operation"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteFileTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        public WriteFileTool(string rootDirectory, ILogger logger)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("path", out var pathObj) || pathObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("Path parameter is required"));
                }

                if (!parameters.TryGetValue("content", out var contentObj) || contentObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("Content parameter is required"));
                }

                var path = pathObj.ToString() ?? "";
                var content = contentObj.ToString() ?? "";
                var append = parameters.TryGetValue("append", out var appendObj) && appendObj != null
                    ? Convert.ToBoolean(appendObj)
                    : false;

                // Sanitize path
                path = path.Replace("..", "").TrimStart('/').TrimStart('\\');
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
                    File.AppendAllText(fullPath, content);
                }
                else
                {
                    File.WriteAllText(fullPath, content);
                }

                var fileInfo = new FileInfo(fullPath);
                var result = new
                {
                    path,
                    size = fileInfo.Length,
                    lastModified = fileInfo.LastWriteTime,
                    append
                };

                return Task.FromResult(McpToolResult.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file");
                return Task.FromResult(McpToolResult.Error($"Error writing file: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Tool for deleting a file
    /// </summary>
    public class DeleteFileTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_delete_file";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Deletes a file or directory";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path of the file or directory to delete (relative to the root directory)",
                    Required = true
                },
                ["recursive"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to delete directories recursively",
                    Required = false,
                    DefaultValue = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Result of the delete operation"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteFileTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        public DeleteFileTool(string rootDirectory, ILogger logger)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("path", out var pathObj) || pathObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("Path parameter is required"));
                }

                var path = pathObj.ToString() ?? "";
                var recursive = parameters.TryGetValue("recursive", out var recursiveObj) && recursiveObj != null
                    ? Convert.ToBoolean(recursiveObj)
                    : false;

                // Sanitize path
                path = path.Replace("..", "").TrimStart('/').TrimStart('\\');
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the path exists
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    return Task.FromResult(McpToolResult.Error($"File or directory not found: {path}"));
                }

                // Delete the file or directory
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return Task.FromResult(McpToolResult.Ok(new { path, type = "file", deleted = true }));
                }
                else
                {
                    Directory.Delete(fullPath, recursive);
                    return Task.FromResult(McpToolResult.Ok(new { path, type = "directory", deleted = true, recursive }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file or directory");
                return Task.FromResult(McpToolResult.Error($"Error deleting file or directory: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Tool for creating a directory
    /// </summary>
    public class CreateDirectoryTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_create_directory";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Creates a directory";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path of the directory to create (relative to the root directory)",
                    Required = true
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Result of the create directory operation"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDirectoryTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        public CreateDirectoryTool(string rootDirectory, ILogger logger)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("path", out var pathObj) || pathObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("Path parameter is required"));
                }

                var path = pathObj.ToString() ?? "";

                // Sanitize path
                path = path.Replace("..", "").TrimStart('/').TrimStart('\\');
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the directory already exists
                if (Directory.Exists(fullPath))
                {
                    return Task.FromResult(McpToolResult.Ok(new { path, created = false, alreadyExists = true }));
                }

                // Create the directory
                Directory.CreateDirectory(fullPath);

                return Task.FromResult(McpToolResult.Ok(new { path, created = true }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directory");
                return Task.FromResult(McpToolResult.Error($"Error creating directory: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Tool for searching files
    /// </summary>
    public class SearchFilesTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;
        private readonly FileSearchService _fileSearchService;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_search_files";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Searches for files by name, content, or metadata";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["search_type"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The type of search to perform (name, content, tag, metadata)",
                    Required = true
                },
                ["search_text"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The text to search for",
                    Required = true
                },
                ["path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path to search in (relative to the root directory)",
                    Required = false,
                    DefaultValue = ""
                },
                ["recursive"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to search recursively (for name search)",
                    Required = false,
                    DefaultValue = true
                },
                ["metadata_key"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The metadata key to search for (for metadata search)",
                    Required = false
                },
                ["file_extensions"] = new McpParameterSchema
                {
                    Type = "array",
                    Description = "File extensions to include in the search (for content search)",
                    Required = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Search results"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchFilesTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        /// <param name="fileSearchService">The file search service</param>
        public SearchFilesTool(string rootDirectory, ILogger logger, FileSearchService fileSearchService)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
            _fileSearchService = fileSearchService;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("search_type", out var searchTypeObj) || searchTypeObj == null)
                {
                    return McpToolResult.Error("Search type parameter is required");
                }

                if (!parameters.TryGetValue("search_text", out var searchTextObj) || searchTextObj == null)
                {
                    return McpToolResult.Error("Search text parameter is required");
                }

                var searchType = searchTypeObj.ToString() ?? "";
                var searchText = searchTextObj.ToString() ?? "";
                var path = parameters.TryGetValue("path", out var pathObj) && pathObj != null
                    ? pathObj.ToString() ?? ""
                    : "";
                var recursive = parameters.TryGetValue("recursive", out var recursiveObj) && recursiveObj != null
                    ? Convert.ToBoolean(recursiveObj)
                    : true;

                // Perform the search based on the search type
                switch (searchType.ToLowerInvariant())
                {
                    case "name":
                        var filesByName = await _fileSearchService.SearchFilesByNameAsync(searchText, path, recursive);
                        var fileResults = filesByName.Select(f => new
                        {
                            name = f.Name,
                            path = f.FullName.Replace(_rootDirectory, "").Replace("\\", "/").TrimStart('/'),
                            size = f.Length,
                            lastModified = f.LastWriteTime
                        });
                        return McpToolResult.Ok(new { search_type = "name", search_text = searchText, results = fileResults.ToArray() });

                    case "content":
                        string[] fileExtensions = null;
                        if (parameters.TryGetValue("file_extensions", out var extensionsObj) && extensionsObj != null)
                        {
                            if (extensionsObj is IEnumerable<object> extensions)
                            {
                                fileExtensions = extensions.Select(e => e.ToString()).ToArray();
                            }
                        }

                        var filesByContent = await _fileSearchService.SearchFilesByContentWithContextAsync(searchText, path, fileExtensions);
                        var contentResults = filesByContent.Select(f => new
                        {
                            name = f.File.Name,
                            path = f.File.FullName.Replace(_rootDirectory, "").Replace("\\", "/").TrimStart('/'),
                            size = f.File.Length,
                            lastModified = f.File.LastWriteTime,
                            context = f.Context
                        });
                        return McpToolResult.Ok(new { search_type = "content", search_text = searchText, results = contentResults.ToArray() });

                    case "tag":
                        var filesByTag = await _fileSearchService.SearchFilesByTagAsync(searchText, path);
                        var tagResults = filesByTag.Select(f => new
                        {
                            name = Path.GetFileName(f.FullPath),
                            path = f.RelativePath,
                            size = f.Size,
                            lastModified = f.LastModified,
                            tags = f.Tags
                        });
                        return McpToolResult.Ok(new { search_type = "tag", search_text = searchText, results = tagResults.ToArray() });

                    case "metadata":
                        if (!parameters.TryGetValue("metadata_key", out var metadataKeyObj) || metadataKeyObj == null)
                        {
                            return McpToolResult.Error("Metadata key parameter is required for metadata search");
                        }

                        var metadataKey = metadataKeyObj.ToString() ?? "";
                        var filesByMetadata = await _fileSearchService.SearchMarkdownByMetadataAsync(metadataKey, searchText, path);
                        var metadataResults = filesByMetadata.Select(f => new
                        {
                            name = Path.GetFileName(f.FullPath),
                            path = f.RelativePath,
                            size = f.Size,
                            lastModified = f.LastModified,
                            title = f.Title,
                            description = f.Description,
                            author = f.Author,
                            keywords = f.Keywords
                        });
                        return McpToolResult.Ok(new { search_type = "metadata", metadata_key = metadataKey, search_text = searchText, results = metadataResults.ToArray() });

                    default:
                        return McpToolResult.Error($"Invalid search type: {searchType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files");
                return McpToolResult.Error($"Error searching files: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for moving files
    /// </summary>
    public class MoveFileTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;
        private readonly IFileSystemService _fileSystemService;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_move_file";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Moves a file or directory";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["source_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The source path (relative to the root directory)",
                    Required = true
                },
                ["destination_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The destination path (relative to the root directory)",
                    Required = true
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Result of the move operation"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveFileTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        /// <param name="fileSystemService">The file system service</param>
        public MoveFileTool(string rootDirectory, ILogger logger, IFileSystemService fileSystemService)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
            _fileSystemService = fileSystemService;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("source_path", out var sourcePathObj) || sourcePathObj == null)
                {
                    return McpToolResult.Error("Source path parameter is required");
                }

                if (!parameters.TryGetValue("destination_path", out var destinationPathObj) || destinationPathObj == null)
                {
                    return McpToolResult.Error("Destination path parameter is required");
                }

                var sourcePath = sourcePathObj.ToString() ?? "";
                var destinationPath = destinationPathObj.ToString() ?? "";

                // Move the file or directory
                var result = await _fileSystemService.MoveAsync(sourcePath, destinationPath);

                return McpToolResult.Ok(new
                {
                    source_path = sourcePath,
                    destination_path = destinationPath,
                    type = result.Type,
                    moved = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file or directory");
                return McpToolResult.Error($"Error moving file or directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for copying files
    /// </summary>
    public class CopyFileTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;
        private readonly IFileSystemService _fileSystemService;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_copy_file";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Copies a file or directory";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["source_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The source path (relative to the root directory)",
                    Required = true
                },
                ["destination_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The destination path (relative to the root directory)",
                    Required = true
                },
                ["overwrite"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to overwrite existing files",
                    Required = false,
                    DefaultValue = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Result of the copy operation"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyFileTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        /// <param name="fileSystemService">The file system service</param>
        public CopyFileTool(string rootDirectory, ILogger logger, IFileSystemService fileSystemService)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
            _fileSystemService = fileSystemService;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("source_path", out var sourcePathObj) || sourcePathObj == null)
                {
                    return McpToolResult.Error("Source path parameter is required");
                }

                if (!parameters.TryGetValue("destination_path", out var destinationPathObj) || destinationPathObj == null)
                {
                    return McpToolResult.Error("Destination path parameter is required");
                }

                var sourcePath = sourcePathObj.ToString() ?? "";
                var destinationPath = destinationPathObj.ToString() ?? "";
                var overwrite = parameters.TryGetValue("overwrite", out var overwriteObj) && overwriteObj != null
                    ? Convert.ToBoolean(overwriteObj)
                    : false;

                // Copy the file or directory
                var result = await _fileSystemService.CopyAsync(sourcePath, destinationPath, overwrite);

                return McpToolResult.Ok(new
                {
                    source_path = sourcePath,
                    destination_path = destinationPath,
                    type = result.Type,
                    copied = true,
                    overwrite
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file or directory");
                return McpToolResult.Error($"Error copying file or directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for getting markdown metadata
    /// </summary>
    public class GetMarkdownMetadataTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;
        private readonly MarkdownProcessor _markdownProcessor;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_get_markdown_metadata";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Gets metadata from a markdown file";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path of the markdown file (relative to the root directory)",
                    Required = true
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Markdown metadata"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMarkdownMetadataTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        /// <param name="markdownProcessor">The markdown processor</param>
        public GetMarkdownMetadataTool(string rootDirectory, ILogger logger, MarkdownProcessor markdownProcessor)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
            _markdownProcessor = markdownProcessor;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("path", out var pathObj) || pathObj == null)
                {
                    return McpToolResult.Error("Path parameter is required");
                }

                var path = pathObj.ToString() ?? "";

                // Sanitize path
                path = path.Replace("..", "").TrimStart('/', '\\');
                var fullPath = Path.Combine(_rootDirectory, path);

                // Check if the file exists
                if (!File.Exists(fullPath))
                {
                    return McpToolResult.Error($"File not found: {path}");
                }

                // Check if the file is a markdown file
                if (!Path.GetExtension(fullPath).Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    return McpToolResult.Error($"File is not a markdown file: {path}");
                }

                // Get the metadata
                var metadata = await _markdownProcessor.ExtractMetadataAsync(fullPath);

                return McpToolResult.Ok(new
                {
                    path,
                    title = metadata.Title,
                    description = metadata.Description,
                    author = metadata.Author,
                    creation_date = metadata.CreationDate,
                    keywords = metadata.Keywords,
                    headings = metadata.Headings,
                    properties = metadata.Properties
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting markdown metadata");
                return McpToolResult.Error($"Error getting markdown metadata: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for organizing files
    /// </summary>
    public class OrganizeFilesTool : IMcpTool
    {
        private readonly string _rootDirectory;
        private readonly ILogger _logger;
        private readonly FileOrganizer _fileOrganizer;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fs_organize_files";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Organizes files by moving them to appropriate folders based on their type";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path to organize (relative to the root directory)",
                    Required = true
                },
                ["organization_type"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The organization type (category, extension, date)",
                    Required = false,
                    DefaultValue = "category"
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Result of the organize operation"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizeFilesTool"/> class
        /// </summary>
        /// <param name="rootDirectory">The root directory</param>
        /// <param name="logger">The logger</param>
        /// <param name="fileOrganizer">The file organizer</param>
        public OrganizeFilesTool(string rootDirectory, ILogger logger, FileOrganizer fileOrganizer)
        {
            _rootDirectory = rootDirectory;
            _logger = logger;
            _fileOrganizer = fileOrganizer;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("path", out var pathObj) || pathObj == null)
                {
                    return McpToolResult.Error("Path parameter is required");
                }

                var path = pathObj.ToString() ?? "";
                var organizationType = parameters.TryGetValue("organization_type", out var organizationTypeObj) && organizationTypeObj != null
                    ? organizationTypeObj.ToString() ?? "category"
                    : "category";

                // Sanitize path
                path = path.Replace("..", "").TrimStart('/', '\\');

                // Organize the files
                var count = await _fileOrganizer.OrganizeFilesByTypeAsync(path, organizationType);

                return McpToolResult.Ok(new
                {
                    path,
                    organization_type = organizationType,
                    files_organized = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error organizing files");
                return McpToolResult.Error($"Error organizing files: {ex.Message}");
            }
        }
    }
}
