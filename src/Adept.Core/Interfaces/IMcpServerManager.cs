namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Manager for the Multi-Context Protocol (MCP) server
    /// </summary>
    public interface IMcpServerManager
    {
        /// <summary>
        /// Gets whether the MCP server is running
        /// </summary>
        bool IsServerRunning { get; }

        /// <summary>
        /// Gets the server URL
        /// </summary>
        string ServerUrl { get; }

        /// <summary>
        /// Gets the available tool providers
        /// </summary>
        IEnumerable<IMcpToolProvider> ToolProviders { get; }

        /// <summary>
        /// Event raised when the server status changes
        /// </summary>
        event EventHandler<McpServerStatusChangedEventArgs>? ServerStatusChanged;

        /// <summary>
        /// Starts the MCP server
        /// </summary>
        Task StartServerAsync();

        /// <summary>
        /// Stops the MCP server
        /// </summary>
        Task StopServerAsync();

        /// <summary>
        /// Restarts the MCP server
        /// </summary>
        Task RestartServerAsync();

        /// <summary>
        /// Registers a tool provider
        /// </summary>
        /// <param name="provider">The tool provider to register</param>
        void RegisterToolProvider(IMcpToolProvider provider);

        /// <summary>
        /// Gets a tool provider by name
        /// </summary>
        /// <param name="providerName">The name of the provider</param>
        /// <returns>The tool provider or null if not found</returns>
        IMcpToolProvider? GetToolProvider(string providerName);

        /// <summary>
        /// Executes a tool
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        Task<McpToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters);

        /// <summary>
        /// Gets the tool schema for all available tools
        /// </summary>
        /// <returns>The tool schema</returns>
        Task<IEnumerable<McpToolSchema>> GetToolSchemaAsync();
    }

    /// <summary>
    /// Event arguments for MCP server status changes
    /// </summary>
    public class McpServerStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether the server is running
        /// </summary>
        public bool IsRunning { get; }

        /// <summary>
        /// Gets the server URL
        /// </summary>
        public string ServerUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpServerStatusChangedEventArgs"/> class
        /// </summary>
        /// <param name="isRunning">Whether the server is running</param>
        /// <param name="serverUrl">The server URL</param>
        public McpServerStatusChangedEventArgs(bool isRunning, string serverUrl)
        {
            IsRunning = isRunning;
            ServerUrl = serverUrl;
        }
    }

    /// <summary>
    /// Result of a tool execution
    /// </summary>
    public class McpToolResult
    {
        /// <summary>
        /// Gets or sets whether the tool execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the result data
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Gets or sets the error message if the tool execution failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpToolResult"/> class
        /// </summary>
        public McpToolResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpToolResult"/> class
        /// </summary>
        /// <param name="success">Whether the tool execution was successful</param>
        /// <param name="data">The result data</param>
        public McpToolResult(bool success, object? data = null)
        {
            Success = success;
            Data = data;
        }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        /// <param name="data">The result data</param>
        /// <returns>A successful result</returns>
        public static McpToolResult Ok(object? data = null)
        {
            return new McpToolResult(true, data);
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <returns>A failed result</returns>
        public static McpToolResult Error(string errorMessage)
        {
            return new McpToolResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Schema for a tool
    /// </summary>
    public class McpToolSchema
    {
        /// <summary>
        /// Gets or sets the name of the tool
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the tool
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameters schema
        /// </summary>
        public Dictionary<string, McpParameterSchema> Parameters { get; set; } = new Dictionary<string, McpParameterSchema>();

        /// <summary>
        /// Gets or sets the return type schema
        /// </summary>
        public McpParameterSchema? ReturnType { get; set; }
    }

    /// <summary>
    /// Schema for a parameter
    /// </summary>
    public class McpParameterSchema
    {
        /// <summary>
        /// Gets or sets the type of the parameter
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the parameter
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the parameter is required
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the default value of the parameter
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the enum values if the parameter is an enum
        /// </summary>
        public string[]? EnumValues { get; set; }
    }
}
