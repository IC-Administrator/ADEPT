using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Mcp
{
    /// <summary>
    /// Manager for the Multi-Context Protocol (MCP) server
    /// </summary>
    public class McpServerManager : IMcpServerManager, IDisposable
    {
        private readonly ILogger<McpServerManager> _logger;
        private readonly HttpClient _httpClient;
        private readonly List<IMcpToolProvider> _toolProviders = new List<IMcpToolProvider>();
        private readonly string _serverPath;
        private readonly int _serverPort;
        private bool _isServerRunning;
        private System.Diagnostics.Process? _serverProcess;
        private bool _disposed;

        /// <summary>
        /// Gets whether the MCP server is running
        /// </summary>
        public bool IsServerRunning => _isServerRunning;

        /// <summary>
        /// Gets the server URL
        /// </summary>
        public string ServerUrl => $"http://localhost:{_serverPort}";

        /// <summary>
        /// Gets the available tool providers
        /// </summary>
        public IEnumerable<IMcpToolProvider> ToolProviders => _toolProviders;

        /// <summary>
        /// Event raised when the server status changes
        /// </summary>
        public event EventHandler<McpServerStatusChangedEventArgs>? ServerStatusChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="McpServerManager"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public McpServerManager(ILogger<McpServerManager> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _serverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mcp-server", "server.js");
            _serverPort = 3000;

            // Create the server directory if it doesn't exist
            var serverDir = Path.GetDirectoryName(_serverPath);
            if (!string.IsNullOrEmpty(serverDir) && !Directory.Exists(serverDir))
            {
                Directory.CreateDirectory(serverDir);
            }

            // Create a basic server.js file if it doesn't exist
            if (!File.Exists(_serverPath))
            {
                CreateBasicServerFile();
            }
        }

        /// <summary>
        /// Starts the MCP server
        /// </summary>
        public async Task StartServerAsync()
        {
            if (_isServerRunning)
            {
                _logger.LogInformation("MCP server is already running");
                return;
            }

            try
            {
                // Check if the server is already running
                try
                {
                    var response = await _httpClient.GetAsync($"{ServerUrl}/health");
                    if (response.IsSuccessStatusCode)
                    {
                        _isServerRunning = true;
                        OnServerStatusChanged();
                        _logger.LogInformation("MCP server is already running at {ServerUrl}", ServerUrl);
                        return;
                    }
                }
                catch
                {
                    // Server is not running, continue with startup
                }

                // Start the server process
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = _serverPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(_serverPath) ?? string.Empty
                };

                _serverProcess = new System.Diagnostics.Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                _serverProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogInformation("MCP server: {Output}", e.Data);
                    }
                };

                _serverProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogError("MCP server error: {Error}", e.Data);
                    }
                };

                _serverProcess.Exited += (sender, e) =>
                {
                    _isServerRunning = false;
                    OnServerStatusChanged();
                    _logger.LogInformation("MCP server has exited");
                };

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                // Wait for the server to start
                var maxRetries = 10;
                var retryDelay = 500;
                var retryCount = 0;
                var serverStarted = false;

                while (retryCount < maxRetries && !serverStarted)
                {
                    try
                    {
                        await Task.Delay(retryDelay);
                        var response = await _httpClient.GetAsync($"{ServerUrl}/health");
                        if (response.IsSuccessStatusCode)
                        {
                            serverStarted = true;
                        }
                    }
                    catch
                    {
                        retryCount++;
                    }
                }

                if (serverStarted)
                {
                    _isServerRunning = true;
                    OnServerStatusChanged();
                    _logger.LogInformation("MCP server started at {ServerUrl}", ServerUrl);

                    // Register tools with the server
                    await RegisterToolsWithServerAsync();
                }
                else
                {
                    _logger.LogError("Failed to start MCP server");
                    await StopServerAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting MCP server");
                await StopServerAsync();
            }
        }

        /// <summary>
        /// Stops the MCP server
        /// </summary>
        public async Task StopServerAsync()
        {
            if (!_isServerRunning)
            {
                return;
            }

            try
            {
                // Try to stop the server gracefully
                try
                {
                    await _httpClient.PostAsync($"{ServerUrl}/shutdown", null);
                }
                catch
                {
                    // Ignore errors
                }

                // Kill the process if it's still running
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(true);
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }

                _isServerRunning = false;
                OnServerStatusChanged();
                _logger.LogInformation("MCP server stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MCP server");
            }
        }

        /// <summary>
        /// Restarts the MCP server
        /// </summary>
        public async Task RestartServerAsync()
        {
            await StopServerAsync();
            await StartServerAsync();
        }

        /// <summary>
        /// Registers a tool provider
        /// </summary>
        /// <param name="provider">The tool provider to register</param>
        public void RegisterToolProvider(IMcpToolProvider provider)
        {
            if (_toolProviders.Any(p => p.ProviderName == provider.ProviderName))
            {
                _logger.LogWarning("Tool provider {ProviderName} is already registered", provider.ProviderName);
                return;
            }

            _toolProviders.Add(provider);
            _logger.LogInformation("Tool provider {ProviderName} registered", provider.ProviderName);

            // Initialize the provider
            provider.InitializeAsync().ConfigureAwait(false);

            // Register tools with the server if it's running
            if (_isServerRunning)
            {
                RegisterToolsWithServerAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a tool provider by name
        /// </summary>
        /// <param name="providerName">The name of the provider</param>
        /// <returns>The tool provider or null if not found</returns>
        public IMcpToolProvider? GetToolProvider(string providerName)
        {
            return _toolProviders.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Executes a tool
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
        {
            if (!_isServerRunning)
            {
                return McpToolResult.Error("MCP server is not running");
            }

            try
            {
                // Find the provider that has the tool
                foreach (var provider in _toolProviders)
                {
                    var tool = provider.GetTool(toolName);
                    if (tool != null)
                    {
                        return await provider.ExecuteToolAsync(toolName, parameters);
                    }
                }

                return McpToolResult.Error($"Tool {toolName} not found");
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
        public Task<IEnumerable<McpToolSchema>> GetToolSchemaAsync()
        {
            var schemas = new List<McpToolSchema>();

            foreach (var provider in _toolProviders)
            {
                schemas.AddRange(provider.GetToolSchema());
            }

            return Task.FromResult<IEnumerable<McpToolSchema>>(schemas);
        }

        /// <summary>
        /// Registers tools with the server
        /// </summary>
        private async Task RegisterToolsWithServerAsync()
        {
            if (!_isServerRunning)
            {
                return;
            }

            try
            {
                var schemas = await GetToolSchemaAsync();
                var content = new StringContent(
                    JsonSerializer.Serialize(schemas),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{ServerUrl}/register-tools", content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Tools registered with MCP server");
                }
                else
                {
                    _logger.LogError("Failed to register tools with MCP server: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering tools with MCP server");
            }
        }

        /// <summary>
        /// Creates a basic server.js file
        /// </summary>
        private void CreateBasicServerFile()
        {
            try
            {
                var serverContent = @"
const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');
const app = express();
const port = 3000;

// Middleware
app.use(cors());
app.use(bodyParser.json());

// Tools registry
const tools = {};

// Health check endpoint
app.get('/health', (req, res) => {
  res.status(200).json({ status: 'ok' });
});

// Register tools endpoint
app.post('/register-tools', (req, res) => {
  const schemas = req.body;
  schemas.forEach(schema => {
    tools[schema.name] = schema;
  });
  res.status(200).json({ message: 'Tools registered successfully', count: schemas.length });
});

// Execute tool endpoint
app.post('/execute-tool', (req, res) => {
  const { toolName, parameters } = req.body;
  
  if (!tools[toolName]) {
    return res.status(404).json({ success: false, errorMessage: `Tool ${toolName} not found` });
  }

  // In a real implementation, this would call back to the .NET application
  // For now, just return a mock response
  res.status(200).json({
    success: true,
    data: { result: `Executed ${toolName} with parameters: ${JSON.stringify(parameters)}` }
  });
});

// Get tool schema endpoint
app.get('/tool-schema', (req, res) => {
  res.status(200).json(Object.values(tools));
});

// Shutdown endpoint
app.post('/shutdown', (req, res) => {
  res.status(200).json({ message: 'Server shutting down' });
  setTimeout(() => process.exit(0), 500);
});

// Start the server
app.listen(port, () => {
  console.log(`MCP server listening at http://localhost:${port}`);
});
";

                // Create the directory if it doesn't exist
                var serverDir = Path.GetDirectoryName(_serverPath);
                if (!string.IsNullOrEmpty(serverDir) && !Directory.Exists(serverDir))
                {
                    Directory.CreateDirectory(serverDir);
                }

                // Write the server file
                File.WriteAllText(_serverPath, serverContent);

                // Create package.json
                var packageJsonPath = Path.Combine(Path.GetDirectoryName(_serverPath) ?? string.Empty, "package.json");
                var packageJsonContent = @"{
  ""name"": ""mcp-server"",
  ""version"": ""1.0.0"",
  ""description"": ""Multi-Context Protocol Server"",
  ""main"": ""server.js"",
  ""dependencies"": {
    ""express"": ""^4.17.1"",
    ""body-parser"": ""^1.19.0"",
    ""cors"": ""^2.8.5""
  }
}";
                File.WriteAllText(packageJsonPath, packageJsonContent);

                _logger.LogInformation("Created basic MCP server files");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating basic MCP server files");
            }
        }

        /// <summary>
        /// Raises the ServerStatusChanged event
        /// </summary>
        private void OnServerStatusChanged()
        {
            ServerStatusChanged?.Invoke(this, new McpServerStatusChangedEventArgs(_isServerRunning, ServerUrl));
        }

        /// <summary>
        /// Disposes the server manager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the server manager
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopServerAsync().GetAwaiter().GetResult();
                    _httpClient.Dispose();
                    _serverProcess?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
