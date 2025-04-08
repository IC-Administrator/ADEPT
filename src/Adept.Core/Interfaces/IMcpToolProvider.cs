namespace Adept.Core.Interfaces
{
    /// <summary>
    /// Provider for MCP tools
    /// </summary>
    public interface IMcpToolProvider
    {
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the available tools
        /// </summary>
        IEnumerable<IMcpTool> Tools { get; }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Gets a tool by name
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <returns>The tool or null if not found</returns>
        IMcpTool? GetTool(string toolName);

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
        IEnumerable<McpToolSchema> GetToolSchema();
    }

    /// <summary>
    /// MCP tool
    /// </summary>
    public interface IMcpTool
    {
        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        McpToolSchema Schema { get; }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters);
    }
}
