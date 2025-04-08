using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Mcp
{
    /// <summary>
    /// Provider for Puppeteer browser automation tools
    /// </summary>
    public class PuppeteerToolProvider : IMcpToolProvider
    {
        private readonly ILogger<PuppeteerToolProvider> _logger;
        private readonly List<IMcpTool> _tools = new List<IMcpTool>();
        private IBrowser? _browser;
        private bool _isInitialized = false;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Puppeteer";

        /// <summary>
        /// Gets the available tools
        /// </summary>
        public IEnumerable<IMcpTool> Tools => _tools;

        /// <summary>
        /// Initializes a new instance of the <see cref="PuppeteerToolProvider"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public PuppeteerToolProvider(ILogger<PuppeteerToolProvider> logger)
        {
            _logger = logger;

            // Initialize tools
            _tools.Add(new NavigateToUrlTool(this, _logger));
            _tools.Add(new ScreenshotTool(this, _logger));
            _tools.Add(new ExtractContentTool(this, _logger));
            _tools.Add(new ClickElementTool(this, _logger));
            _tools.Add(new TypeTextTool(this, _logger));
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Initializing Puppeteer tool provider");

                // Download the Chromium browser if not already installed
                await new BrowserFetcher().DownloadAsync();

                // Launch the browser
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });

                _isInitialized = true;
                _logger.LogInformation("Puppeteer tool provider initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Puppeteer tool provider");
                throw;
            }
        }

        /// <summary>
        /// Gets a tool by name
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <returns>The tool or null if not found</returns>
        public IMcpTool? GetTool(string toolName)
        {
            return _tools.FirstOrDefault(t => t.Name == toolName);
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
                // Ensure the browser is initialized
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }

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
        /// Gets the browser instance
        /// </summary>
        /// <returns>The browser instance</returns>
        public IBrowser? GetBrowser()
        {
            return _browser;
        }

        /// <summary>
        /// Creates a new page
        /// </summary>
        /// <returns>A new page</returns>
        public async Task<IPage> CreatePageAsync()
        {
            if (_browser == null)
            {
                throw new InvalidOperationException("Browser is not initialized");
            }

            return await _browser.NewPageAsync();
        }
    }

    /// <summary>
    /// Tool for navigating to a URL
    /// </summary>
    public class NavigateToUrlTool : IMcpTool
    {
        private readonly PuppeteerToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigateToUrlTool"/> class
        /// </summary>
        /// <param name="provider">The Puppeteer tool provider</param>
        /// <param name="logger">The logger</param>
        public NavigateToUrlTool(PuppeteerToolProvider provider, ILogger logger)
        {
            _provider = provider;
            _logger = logger;
        }

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "puppeteer_navigate";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Navigates to a URL in a headless browser";

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
                    Description = "The URL to navigate to",
                    Required = true
                },
                ["wait_until"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "When to consider navigation succeeded: load, domcontentloaded, networkidle0, networkidle2",
                    Required = false,
                    DefaultValue = "load"
                },
                ["timeout"] = new McpParameterSchema
                {
                    Type = "integer",
                    Description = "Maximum navigation time in milliseconds",
                    Required = false,
                    DefaultValue = 30000
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Navigation result"
            }
        };

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
                var waitUntil = parameters.TryGetValue("wait_until", out var waitUntilObj) && waitUntilObj != null
                    ? waitUntilObj.ToString()
                    : "load";
                var timeout = parameters.TryGetValue("timeout", out var timeoutObj) && timeoutObj != null
                    ? Convert.ToInt32(timeoutObj)
                    : 30000;

                // Validate parameters
                if (string.IsNullOrWhiteSpace(url))
                {
                    return McpToolResult.Error("URL cannot be empty");
                }

                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                // Create a new page
                var page = await _provider.CreatePageAsync();

                // Set viewport
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 1280,
                    Height = 800
                });

                // Navigate to the URL
                var waitUntilOption = waitUntil switch
                {
                    "domcontentloaded" => WaitUntilNavigation.DOMContentLoaded,
                    "networkidle0" => WaitUntilNavigation.Load,
                    "networkidle2" => WaitUntilNavigation.Load,
                    _ => WaitUntilNavigation.Load
                };

                var response = await page.GoToAsync(url, new NavigationOptions
                {
                    WaitUntil = new[] { waitUntilOption },
                    Timeout = timeout
                });

                var title = await page.GetTitleAsync();
                var status = response?.Status ?? 0;
                var statusText = response?.StatusText ?? "Unknown";

                var result = new
                {
                    url,
                    title,
                    status,
                    status_text = statusText,
                    success = response?.Ok ?? false
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to URL");
                return McpToolResult.Error($"Error navigating to URL: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for taking a screenshot
    /// </summary>
    public class ScreenshotTool : IMcpTool
    {
        private readonly PuppeteerToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenshotTool"/> class
        /// </summary>
        /// <param name="provider">The Puppeteer tool provider</param>
        /// <param name="logger">The logger</param>
        public ScreenshotTool(PuppeteerToolProvider provider, ILogger logger)
        {
            _provider = provider;
            _logger = logger;
        }

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "puppeteer_screenshot";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Takes a screenshot of the current page";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["output_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path where to save the screenshot",
                    Required = true
                },
                ["full_page"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to take a screenshot of the full scrollable page",
                    Required = false,
                    DefaultValue = false
                },
                ["selector"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "CSS selector to capture a specific element",
                    Required = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Screenshot result"
            }
        };

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
                if (!parameters.TryGetValue("output_path", out var outputPathObj) || outputPathObj == null)
                {
                    return McpToolResult.Error("Output path parameter is required");
                }

                var outputPath = outputPathObj.ToString() ?? "";
                var fullPage = parameters.TryGetValue("full_page", out var fullPageObj) && fullPageObj != null
                    ? Convert.ToBoolean(fullPageObj)
                    : false;
                var selector = parameters.TryGetValue("selector", out var selectorObj) && selectorObj != null
                    ? selectorObj.ToString()
                    : null;

                // Validate parameters
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    return McpToolResult.Error("Output path cannot be empty");
                }

                // Ensure the output directory exists
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create a new page if browser has no pages
                var browser = _provider.GetBrowser();
                if (browser == null)
                {
                    return McpToolResult.Error("Browser is not initialized");
                }

                var pages = await browser.PagesAsync();
                var page = pages.LastOrDefault();
                if (page == null)
                {
                    return McpToolResult.Error("No active page. Navigate to a URL first.");
                }

                // Take the screenshot
                if (!string.IsNullOrEmpty(selector))
                {
                    // Wait for the element to be visible
                    await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
                    {
                        Visible = true,
                        Timeout = 5000
                    });

                    // Get the element
                    var element = await page.QuerySelectorAsync(selector);
                    if (element == null)
                    {
                        return McpToolResult.Error($"Element with selector '{selector}' not found");
                    }

                    // Take screenshot of the element
                    await element.ScreenshotAsync(outputPath);
                }
                else
                {
                    // Take screenshot of the page
                    await page.ScreenshotAsync(outputPath, new ScreenshotOptions
                    {
                        FullPage = fullPage
                    });
                }

                var result = new
                {
                    output_path = outputPath,
                    full_page = fullPage,
                    selector = selector
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking screenshot");
                return McpToolResult.Error($"Error taking screenshot: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for extracting content from a page
    /// </summary>
    public class ExtractContentTool : IMcpTool
    {
        private readonly PuppeteerToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractContentTool"/> class
        /// </summary>
        /// <param name="provider">The Puppeteer tool provider</param>
        /// <param name="logger">The logger</param>
        public ExtractContentTool(PuppeteerToolProvider provider, ILogger logger)
        {
            _provider = provider;
            _logger = logger;
        }

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "puppeteer_extract_content";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Extracts content from the current page";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["selector"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "CSS selector to extract content from (optional, defaults to body)",
                    Required = false,
                    DefaultValue = "body"
                },
                ["extract_type"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "Type of content to extract: text, html, or innerText",
                    Required = false,
                    DefaultValue = "text"
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Extracted content"
            }
        };

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
                var selector = parameters.TryGetValue("selector", out var selectorObj) && selectorObj != null
                    ? selectorObj.ToString()
                    : "body";
                var extractType = parameters.TryGetValue("extract_type", out var extractTypeObj) && extractTypeObj != null
                    ? extractTypeObj.ToString()?.ToLower()
                    : "text";

                // Create a new page if browser has no pages
                var browser = _provider.GetBrowser();
                if (browser == null)
                {
                    return McpToolResult.Error("Browser is not initialized");
                }

                var pages = await browser.PagesAsync();
                var page = pages.LastOrDefault();
                if (page == null)
                {
                    return McpToolResult.Error("No active page. Navigate to a URL first.");
                }

                // Wait for the selector to be available
                await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
                {
                    Timeout = 5000
                });

                // Extract the content based on the extract type
                string content;
                switch (extractType)
                {
                    case "html":
                        content = await page.EvaluateFunctionAsync<string>(@"(selector) => {
                            const element = document.querySelector(selector);
                            return element ? element.outerHTML : '';
                        }", selector);
                        break;
                    case "innertext":
                        content = await page.EvaluateFunctionAsync<string>(@"(selector) => {
                            const element = document.querySelector(selector);
                            return element ? element.innerText : '';
                        }", selector);
                        break;
                    case "text":
                    default:
                        content = await page.EvaluateFunctionAsync<string>(@"(selector) => {
                            const element = document.querySelector(selector);
                            return element ? element.textContent : '';
                        }", selector);
                        break;
                }

                var title = await page.GetTitleAsync();
                var url = page.Url;

                var result = new
                {
                    url,
                    title,
                    selector,
                    extract_type = extractType,
                    content
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting content");
                return McpToolResult.Error($"Error extracting content: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for clicking an element on the page
    /// </summary>
    public class ClickElementTool : IMcpTool
    {
        private readonly PuppeteerToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickElementTool"/> class
        /// </summary>
        /// <param name="provider">The Puppeteer tool provider</param>
        /// <param name="logger">The logger</param>
        public ClickElementTool(PuppeteerToolProvider provider, ILogger logger)
        {
            _provider = provider;
            _logger = logger;
        }

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "puppeteer_click";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Clicks an element on the current page";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["selector"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "CSS selector of the element to click",
                    Required = true
                },
                ["wait_for_navigation"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to wait for navigation after clicking",
                    Required = false,
                    DefaultValue = true
                },
                ["timeout"] = new McpParameterSchema
                {
                    Type = "integer",
                    Description = "Timeout in milliseconds",
                    Required = false,
                    DefaultValue = 30000
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Click result"
            }
        };

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
                if (!parameters.TryGetValue("selector", out var selectorObj) || selectorObj == null)
                {
                    return McpToolResult.Error("Selector parameter is required");
                }

                var selector = selectorObj.ToString() ?? "";
                var waitForNavigation = parameters.TryGetValue("wait_for_navigation", out var waitForNavigationObj) && waitForNavigationObj != null
                    ? Convert.ToBoolean(waitForNavigationObj)
                    : true;
                var timeout = parameters.TryGetValue("timeout", out var timeoutObj) && timeoutObj != null
                    ? Convert.ToInt32(timeoutObj)
                    : 30000;

                // Validate parameters
                if (string.IsNullOrWhiteSpace(selector))
                {
                    return McpToolResult.Error("Selector cannot be empty");
                }

                // Create a new page if browser has no pages
                var browser = _provider.GetBrowser();
                if (browser == null)
                {
                    return McpToolResult.Error("Browser is not initialized");
                }

                var pages = await browser.PagesAsync();
                var page = pages.LastOrDefault();
                if (page == null)
                {
                    return McpToolResult.Error("No active page. Navigate to a URL first.");
                }

                // Wait for the element to be visible
                await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
                {
                    Visible = true,
                    Timeout = timeout
                });

                // Click the element
                if (waitForNavigation)
                {
                    // Click and wait for navigation
                    await Task.WhenAll(
                        page.WaitForNavigationAsync(new NavigationOptions
                        {
                            WaitUntil = new[] { WaitUntilNavigation.Load },
                            Timeout = timeout
                        }),
                        page.ClickAsync(selector)
                    );
                }
                else
                {
                    // Just click without waiting for navigation
                    await page.ClickAsync(selector);
                }

                var title = await page.GetTitleAsync();
                var url = page.Url;

                var result = new
                {
                    url,
                    title,
                    selector,
                    wait_for_navigation = waitForNavigation
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clicking element");
                return McpToolResult.Error($"Error clicking element: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for typing text into an input field
    /// </summary>
    public class TypeTextTool : IMcpTool
    {
        private readonly PuppeteerToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeTextTool"/> class
        /// </summary>
        /// <param name="provider">The Puppeteer tool provider</param>
        /// <param name="logger">The logger</param>
        public TypeTextTool(PuppeteerToolProvider provider, ILogger logger)
        {
            _provider = provider;
            _logger = logger;
        }

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "puppeteer_type";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Types text into an input field";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["selector"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "CSS selector of the input field",
                    Required = true
                },
                ["text"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "Text to type",
                    Required = true
                },
                ["delay"] = new McpParameterSchema
                {
                    Type = "integer",
                    Description = "Delay between keystrokes in milliseconds",
                    Required = false,
                    DefaultValue = 0
                },
                ["clear_first"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to clear the input field before typing",
                    Required = false,
                    DefaultValue = true
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Type result"
            }
        };

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
                if (!parameters.TryGetValue("selector", out var selectorObj) || selectorObj == null)
                {
                    return McpToolResult.Error("Selector parameter is required");
                }

                if (!parameters.TryGetValue("text", out var textObj) || textObj == null)
                {
                    return McpToolResult.Error("Text parameter is required");
                }

                var selector = selectorObj.ToString() ?? "";
                var text = textObj.ToString() ?? "";
                var delay = parameters.TryGetValue("delay", out var delayObj) && delayObj != null
                    ? Convert.ToInt32(delayObj)
                    : 0;
                var clearFirst = parameters.TryGetValue("clear_first", out var clearFirstObj) && clearFirstObj != null
                    ? Convert.ToBoolean(clearFirstObj)
                    : true;

                // Validate parameters
                if (string.IsNullOrWhiteSpace(selector))
                {
                    return McpToolResult.Error("Selector cannot be empty");
                }

                // Create a new page if browser has no pages
                var browser = _provider.GetBrowser();
                if (browser == null)
                {
                    return McpToolResult.Error("Browser is not initialized");
                }

                var pages = await browser.PagesAsync();
                var page = pages.LastOrDefault();
                if (page == null)
                {
                    return McpToolResult.Error("No active page. Navigate to a URL first.");
                }

                // Wait for the element to be visible
                await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
                {
                    Visible = true,
                    Timeout = 5000
                });

                // Clear the input field if requested
                if (clearFirst)
                {
                    await page.EvaluateFunctionAsync(@"(selector) => {
                        const element = document.querySelector(selector);
                        if (element) {
                            element.value = '';
                        }
                    }", selector);
                }

                // Type the text
                await page.TypeAsync(selector, text);

                var result = new
                {
                    selector,
                    text,
                    delay,
                    clear_first = clearFirst
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error typing text");
                return McpToolResult.Error($"Error typing text: {ex.Message}");
            }
        }
    }
}
