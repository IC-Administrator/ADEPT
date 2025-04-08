using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;

namespace Adept.Services.Mcp
{
    /// <summary>
    /// Provider for web content fetching tools
    /// </summary>
    public class FetchToolProvider : IMcpToolProvider
    {
        private readonly ILogger<FetchToolProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly List<IMcpTool> _tools = new List<IMcpTool>();

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Fetch";

        /// <summary>
        /// Gets the available tools
        /// </summary>
        public IEnumerable<IMcpTool> Tools => _tools;

        /// <summary>
        /// Initializes a new instance of the <see cref="FetchToolProvider"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public FetchToolProvider(ILogger<FetchToolProvider> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Adept/1.0");

            // Initialize tools
            _tools.Add(new FetchUrlTool(_httpClient, _logger));
            _tools.Add(new ExtractTextTool(_logger));
            _tools.Add(new ExtractMetadataTool(_httpClient, _logger));
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public Task InitializeAsync()
        {
            _logger.LogInformation("Fetch tool provider initialized");
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
    /// Tool for fetching content from a URL
    /// </summary>
    public class FetchUrlTool : IMcpTool
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "fetch_url";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Fetches content from a URL";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["url"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The URL to fetch",
                    Required = true
                },
                ["timeout"] = new McpParameterSchema
                {
                    Type = "integer",
                    Description = "Timeout in seconds",
                    Required = false,
                    DefaultValue = 30
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Fetched content and metadata"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="FetchUrlTool"/> class
        /// </summary>
        /// <param name="httpClient">The HTTP client</param>
        /// <param name="logger">The logger</param>
        public FetchUrlTool(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
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
                // Get parameters
                if (!parameters.TryGetValue("url", out var urlObj) || urlObj == null)
                {
                    return McpToolResult.Error("URL parameter is required");
                }

                var url = urlObj.ToString() ?? "";
                var timeout = parameters.TryGetValue("timeout", out var timeoutObj) && timeoutObj != null
                    ? Convert.ToInt32(timeoutObj)
                    : 30;

                // Validate URL
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return McpToolResult.Error("Invalid URL");
                }

                // Set timeout
                _httpClient.Timeout = TimeSpan.FromSeconds(timeout);

                // Fetch the content
                var response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";
                var contentLength = response.Content.Headers.ContentLength ?? content.Length;

                var result = new
                {
                    url,
                    status_code = (int)response.StatusCode,
                    content_type = contentType,
                    content_length = contentLength,
                    content = content.Length > 1000000 ? content.Substring(0, 1000000) + "..." : content
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching URL");
                return McpToolResult.Error($"Error fetching URL: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for extracting text from HTML content
    /// </summary>
    public class ExtractTextTool : IMcpTool
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "extract_text";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Extracts text from HTML content";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["html"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The HTML content to extract text from",
                    Required = true
                },
                ["selector"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "CSS selector to extract specific elements (optional)",
                    Required = false
                },
                ["include_links"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to include links in the extracted text",
                    Required = false,
                    DefaultValue = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Extracted text"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractTextTool"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public ExtractTextTool(ILogger logger)
        {
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
                if (!parameters.TryGetValue("html", out var htmlObj) || htmlObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("HTML parameter is required"));
                }

                var html = htmlObj.ToString() ?? "";
                var selector = parameters.TryGetValue("selector", out var selectorObj) && selectorObj != null
                    ? selectorObj.ToString()
                    : null;
                var includeLinks = parameters.TryGetValue("include_links", out var includeLinksObj) && includeLinksObj != null
                    ? Convert.ToBoolean(includeLinksObj)
                    : false;

                // Parse the HTML
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Extract text
                string extractedText;
                var links = new List<Dictionary<string, string>>();

                if (string.IsNullOrEmpty(selector))
                {
                    // Extract all text
                    extractedText = doc.DocumentNode.InnerText;

                    // Extract links if requested
                    if (includeLinks)
                    {
                        var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
                        if (linkNodes != null)
                        {
                            foreach (var linkNode in linkNodes)
                            {
                                var href = linkNode.GetAttributeValue("href", "");
                                var text = linkNode.InnerText.Trim();
                                if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(text))
                                {
                                    links.Add(new Dictionary<string, string>
                                    {
                                        ["href"] = href,
                                        ["text"] = text
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Extract text from selected elements
                    var nodes = doc.DocumentNode.SelectNodes(selector);
                    if (nodes == null || nodes.Count == 0)
                    {
                        return Task.FromResult(McpToolResult.Error($"No elements found matching selector: {selector}"));
                    }

                    var sb = new StringBuilder();
                    foreach (var node in nodes)
                    {
                        sb.AppendLine(node.InnerText.Trim());

                        // Extract links if requested
                        if (includeLinks)
                        {
                            var linkNodes = node.SelectNodes(".//a[@href]");
                            if (linkNodes != null)
                            {
                                foreach (var linkNode in linkNodes)
                                {
                                    var href = linkNode.GetAttributeValue("href", "");
                                    var text = linkNode.InnerText.Trim();
                                    if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(text))
                                    {
                                        links.Add(new Dictionary<string, string>
                                        {
                                            ["href"] = href,
                                            ["text"] = text
                                        });
                                    }
                                }
                            }
                        }
                    }
                    extractedText = sb.ToString();
                }

                // Clean up the text
                extractedText = CleanText(extractedText);

                var result = new
                {
                    text = extractedText,
                    links = includeLinks ? links.ToArray() : null,
                    selector
                };

                return Task.FromResult(McpToolResult.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text");
                return Task.FromResult(McpToolResult.Error($"Error extracting text: {ex.Message}"));
            }
        }

        /// <summary>
        /// Cleans up extracted text
        /// </summary>
        /// <param name="text">The text to clean</param>
        /// <returns>The cleaned text</returns>
        private string CleanText(string text)
        {
            // Replace multiple whitespace with a single space
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            
            // Replace multiple newlines with a single newline
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n+", "\n");
            
            // Trim the text
            text = text.Trim();
            
            return text;
        }
    }

    /// <summary>
    /// Tool for extracting metadata from a web page
    /// </summary>
    public class ExtractMetadataTool : IMcpTool
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "extract_metadata";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Extracts metadata from a web page";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["url"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The URL of the web page",
                    Required = true
                },
                ["timeout"] = new McpParameterSchema
                {
                    Type = "integer",
                    Description = "Timeout in seconds",
                    Required = false,
                    DefaultValue = 30
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Extracted metadata"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractMetadataTool"/> class
        /// </summary>
        /// <param name="httpClient">The HTTP client</param>
        /// <param name="logger">The logger</param>
        public ExtractMetadataTool(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
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
                // Get parameters
                if (!parameters.TryGetValue("url", out var urlObj) || urlObj == null)
                {
                    return McpToolResult.Error("URL parameter is required");
                }

                var url = urlObj.ToString() ?? "";
                var timeout = parameters.TryGetValue("timeout", out var timeoutObj) && timeoutObj != null
                    ? Convert.ToInt32(timeoutObj)
                    : 30;

                // Validate URL
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return McpToolResult.Error("Invalid URL");
                }

                // Set timeout
                _httpClient.Timeout = TimeSpan.FromSeconds(timeout);

                // Fetch the content
                var response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync();

                // Parse the HTML
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Extract metadata
                var metadata = new Dictionary<string, string>();

                // Extract title
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                if (titleNode != null)
                {
                    metadata["title"] = titleNode.InnerText.Trim();
                }

                // Extract meta tags
                var metaNodes = doc.DocumentNode.SelectNodes("//meta");
                if (metaNodes != null)
                {
                    foreach (var metaNode in metaNodes)
                    {
                        var name = metaNode.GetAttributeValue("name", "");
                        var property = metaNode.GetAttributeValue("property", "");
                        var content = metaNode.GetAttributeValue("content", "");

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
                        {
                            metadata[$"meta_{name}"] = content;
                        }
                        else if (!string.IsNullOrEmpty(property) && !string.IsNullOrEmpty(content))
                        {
                            metadata[$"meta_{property}"] = content;
                        }
                    }
                }

                // Extract Open Graph tags
                var ogMetadata = new Dictionary<string, string>();
                var ogNodes = doc.DocumentNode.SelectNodes("//meta[starts-with(@property, 'og:')]");
                if (ogNodes != null)
                {
                    foreach (var ogNode in ogNodes)
                    {
                        var property = ogNode.GetAttributeValue("property", "");
                        var content = ogNode.GetAttributeValue("content", "");

                        if (!string.IsNullOrEmpty(property) && !string.IsNullOrEmpty(content))
                        {
                            ogMetadata[property.Substring(3)] = content;
                        }
                    }
                }

                // Extract Twitter Card tags
                var twitterMetadata = new Dictionary<string, string>();
                var twitterNodes = doc.DocumentNode.SelectNodes("//meta[starts-with(@name, 'twitter:')]");
                if (twitterNodes != null)
                {
                    foreach (var twitterNode in twitterNodes)
                    {
                        var name = twitterNode.GetAttributeValue("name", "");
                        var content = twitterNode.GetAttributeValue("content", "");

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
                        {
                            twitterMetadata[name.Substring(8)] = content;
                        }
                    }
                }

                // Extract links
                var links = new List<Dictionary<string, string>>();
                var linkNodes = doc.DocumentNode.SelectNodes("//link[@rel and @href]");
                if (linkNodes != null)
                {
                    foreach (var linkNode in linkNodes)
                    {
                        var rel = linkNode.GetAttributeValue("rel", "");
                        var href = linkNode.GetAttributeValue("href", "");

                        if (!string.IsNullOrEmpty(rel) && !string.IsNullOrEmpty(href))
                        {
                            links.Add(new Dictionary<string, string>
                            {
                                ["rel"] = rel,
                                ["href"] = href
                            });
                        }
                    }
                }

                var result = new
                {
                    url,
                    metadata,
                    open_graph = ogMetadata,
                    twitter_card = twitterMetadata,
                    links
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting metadata");
                return McpToolResult.Error($"Error extracting metadata: {ex.Message}");
            }
        }
    }
}
