using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Adept.Services.Llm
{
    /// <summary>
    /// Service for integrating LLM tools with the MCP server
    /// </summary>
    public class LlmToolIntegrationService : IDisposable
    {
        private readonly IMcpServerManager _mcpServerManager;
        private readonly ILogger<LlmToolIntegrationService> _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LlmToolIntegrationService"/> class
        /// </summary>
        /// <param name="mcpServerManager">The MCP server manager</param>
        /// <param name="logger">The logger</param>
        public LlmToolIntegrationService(IMcpServerManager mcpServerManager, ILogger<LlmToolIntegrationService> logger)
        {
            _mcpServerManager = mcpServerManager;
            _logger = logger;

            // Subscribe to MCP server status changes
            _mcpServerManager.ServerStatusChanged += OnServerStatusChanged;

            // Start the MCP server
            StartServerAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the MCP server
        /// </summary>
        private async Task StartServerAsync()
        {
            try
            {
                await _mcpServerManager.StartServerAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting MCP server");
            }
        }

        /// <summary>
        /// Gets the LLM tools for the MCP server
        /// </summary>
        /// <returns>The LLM tools</returns>
        public async Task<IEnumerable<LlmTool>> GetLlmToolsAsync()
        {
            try
            {
                var toolSchemas = await _mcpServerManager.GetToolSchemaAsync();
                var llmTools = new List<LlmTool>();

                foreach (var schema in toolSchemas)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = schema.Parameters.ToDictionary(
                            p => p.Key,
                            p => new
                            {
                                type = p.Value.Type.ToLower(),
                                description = p.Value.Description,
                                @enum = p.Value.EnumValues
                            }),
                        ["required"] = schema.Parameters
                            .Where(p => p.Value.Required)
                            .Select(p => p.Key)
                            .ToArray()
                    };

                    llmTools.Add(new LlmTool(schema.Name, schema.Description, parameters));
                }

                return llmTools;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LLM tools");
                return Enumerable.Empty<LlmTool>();
            }
        }

        /// <summary>
        /// Processes tool calls in an LLM response
        /// </summary>
        /// <param name="response">The LLM response</param>
        /// <returns>The processed response with tool results</returns>
        public async Task<LlmResponse> ProcessToolCallsAsync(LlmResponse response)
        {
            if (response.ToolCalls == null || !response.ToolCalls.Any())
            {
                return response;
            }

            try
            {
                foreach (var toolCall in response.ToolCalls)
                {
                    var toolResult = await ExecuteToolAsync(toolCall.ToolName, toolCall.Arguments);
                    
                    // Add the tool result to the response
                    response.Message.Content += $"\n\nTool: {toolCall.ToolName}\nResult: {toolResult}";
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tool calls");
                return response;
            }
        }

        /// <summary>
        /// Extracts and processes tool calls from a message
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <returns>The processed message with tool results</returns>
        public async Task<string> ProcessMessageToolCallsAsync(string message)
        {
            try
            {
                // Extract tool calls using regex
                var toolCallPattern = @"```tool\s+(\w+)\s+([\s\S]*?)```";
                var matches = Regex.Matches(message, toolCallPattern);

                if (matches.Count == 0)
                {
                    return message;
                }

                var processedMessage = message;

                foreach (Match match in matches)
                {
                    var toolName = match.Groups[1].Value.Trim();
                    var toolArgs = match.Groups[2].Value.Trim();
                    
                    // Parse the arguments
                    Dictionary<string, object> parameters;
                    try
                    {
                        parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(toolArgs) ?? new Dictionary<string, object>();
                    }
                    catch
                    {
                        // If JSON parsing fails, try to parse as key-value pairs
                        parameters = ParseKeyValuePairs(toolArgs);
                    }

                    // Execute the tool
                    var toolResult = await ExecuteToolAsync(toolName, parameters);
                    
                    // Replace the tool call with the result
                    var replacement = $"```tool {toolName}\n{toolArgs}\n```\n\n**Tool Result:**\n```json\n{toolResult}\n```";
                    processedMessage = processedMessage.Replace(match.Value, replacement);
                }

                return processedMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message tool calls");
                return message;
            }
        }

        /// <summary>
        /// Executes a tool
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="arguments">The arguments for the tool</param>
        /// <returns>The tool result as a string</returns>
        private async Task<string> ExecuteToolAsync(string toolName, string arguments)
        {
            try
            {
                // Parse the arguments
                Dictionary<string, object> parameters;
                try
                {
                    parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments) ?? new Dictionary<string, object>();
                }
                catch
                {
                    // If JSON parsing fails, try to parse as key-value pairs
                    parameters = ParseKeyValuePairs(arguments);
                }

                return await ExecuteToolAsync(toolName, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
                return $"Error executing tool: {ex.Message}";
            }
        }

        /// <summary>
        /// Executes a tool
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result as a string</returns>
        private async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
        {
            try
            {
                var result = await _mcpServerManager.ExecuteToolAsync(toolName, parameters);
                
                if (result.Success)
                {
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    return $"Error: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
                return $"Error executing tool: {ex.Message}";
            }
        }

        /// <summary>
        /// Parses key-value pairs from a string
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>A dictionary of key-value pairs</returns>
        private Dictionary<string, object> ParseKeyValuePairs(string input)
        {
            var parameters = new Dictionary<string, object>();
            var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    // Try to parse as number or boolean
                    if (int.TryParse(value, out var intValue))
                    {
                        parameters[key] = intValue;
                    }
                    else if (double.TryParse(value, out var doubleValue))
                    {
                        parameters[key] = doubleValue;
                    }
                    else if (bool.TryParse(value, out var boolValue))
                    {
                        parameters[key] = boolValue;
                    }
                    else
                    {
                        parameters[key] = value;
                    }
                }
            }
            
            return parameters;
        }

        /// <summary>
        /// Handles MCP server status changes
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void OnServerStatusChanged(object? sender, McpServerStatusChangedEventArgs e)
        {
            if (e.IsRunning)
            {
                _logger.LogInformation("MCP server is running at {ServerUrl}", e.ServerUrl);
            }
            else
            {
                _logger.LogInformation("MCP server is not running");
            }
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events
                    _mcpServerManager.ServerStatusChanged -= OnServerStatusChanged;
                    
                    // Stop the MCP server
                    _mcpServerManager.StopServerAsync().GetAwaiter().GetResult();
                }

                _disposed = true;
            }
        }
    }
}
