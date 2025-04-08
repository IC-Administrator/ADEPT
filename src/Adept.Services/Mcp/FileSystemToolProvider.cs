using Adept.Core.Interfaces;
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
        public FileSystemToolProvider(IConfiguration configuration, ILogger<FileSystemToolProvider> logger)
        {
            _logger = logger;

            // Get the scratchpad folder from configuration
            var scratchpadFolder = configuration["ScratchpadFolder"];

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
            _logger.LogInformation("FileSystem tool provider initialized with root directory: {RootDirectory}", _rootDirectory);

            // Create the root directory if it doesn't exist
            if (!Directory.Exists(_rootDirectory))
            {
                Directory.CreateDirectory(_rootDirectory);
            }

            // Initialize tools
            _tools.Add(new ListFilesTool(_rootDirectory, _logger));
            _tools.Add(new ReadFileTool(_rootDirectory, _logger));
            _tools.Add(new WriteFileTool(_rootDirectory, _logger));
            _tools.Add(new DeleteFileTool(_rootDirectory, _logger));
            _tools.Add(new CreateDirectoryTool(_rootDirectory, _logger));
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
}
