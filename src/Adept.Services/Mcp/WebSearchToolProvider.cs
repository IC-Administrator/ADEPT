using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Adept.Services.Mcp
{
    /// <summary>
    /// Provider for web search tools
    /// </summary>
    public class WebSearchToolProvider : IMcpToolProvider
    {
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<WebSearchToolProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly List<IMcpTool> _tools = new List<IMcpTool>();
        private string? _braveApiKey;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "WebSearch";

        /// <summary>
        /// Gets the available tools
        /// </summary>
        public IEnumerable<IMcpTool> Tools => _tools;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSearchToolProvider"/> class
        /// </summary>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public WebSearchToolProvider(ISecureStorageService secureStorageService, ILogger<WebSearchToolProvider> logger)
        {
            _secureStorageService = secureStorageService;
            _logger = logger;
            _httpClient = new HttpClient();

            // Initialize tools
            _tools.Add(new BraveSearchTool(this, _logger));
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Get the Brave API key
                _braveApiKey = await _secureStorageService.RetrieveSecureValueAsync("brave_api_key");
                
                if (string.IsNullOrEmpty(_braveApiKey))
                {
                    _logger.LogWarning("Brave API key not found in secure storage");
                }
                else
                {
                    _logger.LogInformation("Brave API key retrieved from secure storage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing WebSearch tool provider");
            }
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

        /// <summary>
        /// Sets the Brave API key
        /// </summary>
        /// <param name="apiKey">The API key</param>
        public async Task SetBraveApiKeyAsync(string apiKey)
        {
            try
            {
                await _secureStorageService.StoreSecureValueAsync("brave_api_key", apiKey);
                _braveApiKey = apiKey;
                _logger.LogInformation("Brave API key stored in secure storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing Brave API key");
                throw;
            }
        }

        /// <summary>
        /// Gets the Brave API key
        /// </summary>
        /// <returns>The API key</returns>
        public string? GetBraveApiKey()
        {
            return _braveApiKey;
        }

        /// <summary>
        /// Performs a Brave search
        /// </summary>
        /// <param name="query">The search query</param>
        /// <param name="count">The number of results to return</param>
        /// <returns>The search results</returns>
        public async Task<object> BraveSearchAsync(string query, int count)
        {
            if (string.IsNullOrEmpty(_braveApiKey))
            {
                throw new InvalidOperationException("Brave API key not set");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}&count={count}");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("X-Subscription-Token", _braveApiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<JsonElement>(content);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing Brave search for query: {Query}", query);
                throw;
            }
        }
    }

    /// <summary>
    /// Tool for performing Brave searches
    /// </summary>
    public class BraveSearchTool : IMcpTool
    {
        private readonly WebSearchToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "web_search";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Searches the web using Brave Search";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["query"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The search query",
                    Required = true
                },
                ["count"] = new McpParameterSchema
                {
                    Type = "integer",
                    Description = "The number of results to return",
                    Required = false,
                    DefaultValue = 5
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Search results"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BraveSearchTool"/> class
        /// </summary>
        /// <param name="provider">The web search tool provider</param>
        /// <param name="logger">The logger</param>
        public BraveSearchTool(WebSearchToolProvider provider, ILogger logger)
        {
            _provider = provider;
            _logger = logger;
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
                // Check if the API key is set
                var apiKey = _provider.GetBraveApiKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return McpToolResult.Error("Brave API key not set. Please set the API key first.");
                }

                // Get parameters
                if (!parameters.TryGetValue("query", out var queryObj) || queryObj == null)
                {
                    return McpToolResult.Error("Query parameter is required");
                }

                var query = queryObj.ToString() ?? "";
                var count = parameters.TryGetValue("count", out var countObj) && countObj != null
                    ? Convert.ToInt32(countObj)
                    : 5;

                // Validate parameters
                if (string.IsNullOrWhiteSpace(query))
                {
                    return McpToolResult.Error("Query cannot be empty");
                }

                if (count < 1 || count > 20)
                {
                    return McpToolResult.Error("Count must be between 1 and 20");
                }

                // Perform the search
                var results = await _provider.BraveSearchAsync(query, count);
                return McpToolResult.Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Brave search");
                return McpToolResult.Error($"Error executing Brave search: {ex.Message}");
            }
        }
    }
}
