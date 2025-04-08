using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System.Data;
using System.Text;
using ClosedXML.Excel;

namespace Adept.Services.Mcp
{
    /// <summary>
    /// Provider for Excel tools
    /// </summary>
    public class ExcelToolProvider : IMcpToolProvider
    {
        private readonly ILogger<ExcelToolProvider> _logger;
        private readonly List<IMcpTool> _tools = new List<IMcpTool>();

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Excel";

        /// <summary>
        /// Gets the available tools
        /// </summary>
        public IEnumerable<IMcpTool> Tools => _tools;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelToolProvider"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public ExcelToolProvider(ILogger<ExcelToolProvider> logger)
        {
            _logger = logger;

            // Initialize tools
            _tools.Add(new ReadExcelTool(_logger));
            _tools.Add(new ExcelToJsonTool(_logger));
            _tools.Add(new GetExcelSheetsTool(_logger));
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public Task InitializeAsync()
        {
            _logger.LogInformation("Excel tool provider initialized");
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
    /// Tool for reading Excel files
    /// </summary>
    public class ReadExcelTool : IMcpTool
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "excel_read";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Reads data from an Excel file";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["file_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path to the Excel file",
                    Required = true
                },
                ["sheet_name"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The name of the sheet to read (default: first sheet)",
                    Required = false
                },
                ["use_headers"] = new McpParameterSchema
                {
                    Type = "boolean",
                    Description = "Whether to use the first row as headers",
                    Required = false,
                    DefaultValue = true
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Excel data as a table"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadExcelTool"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public ReadExcelTool(ILogger logger)
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
                if (!parameters.TryGetValue("file_path", out var filePathObj) || filePathObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("File path parameter is required"));
                }

                var filePath = filePathObj.ToString() ?? "";
                var sheetName = parameters.TryGetValue("sheet_name", out var sheetNameObj) && sheetNameObj != null
                    ? sheetNameObj.ToString()
                    : null;
                var useHeaders = parameters.TryGetValue("use_headers", out var useHeadersObj) && useHeadersObj != null
                    ? Convert.ToBoolean(useHeadersObj)
                    : true;

                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    return Task.FromResult(McpToolResult.Error($"File not found: {filePath}"));
                }

                // Read the Excel file
                using var workbook = new XLWorkbook(filePath);
                
                // Get the worksheet
                IXLWorksheet worksheet;
                if (string.IsNullOrEmpty(sheetName))
                {
                    worksheet = workbook.Worksheet(1);
                }
                else
                {
                    if (!workbook.TryGetWorksheet(sheetName, out worksheet))
                    {
                        return Task.FromResult(McpToolResult.Error($"Sheet not found: {sheetName}"));
                    }
                }

                // Get the used range
                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    return Task.FromResult(McpToolResult.Ok(new { rows = new object[0] }));
                }

                // Convert to a list of rows
                var rows = new List<object>();
                var headerRow = useHeaders ? range.FirstRow().Cells().Select(c => c.Value.ToString()).ToArray() : null;
                
                var dataRows = useHeaders ? range.RowsUsed().Skip(1) : range.RowsUsed();
                foreach (var row in dataRows)
                {
                    if (useHeaders)
                    {
                        var rowData = new Dictionary<string, object>();
                        for (int i = 0; i < headerRow.Length; i++)
                        {
                            var cell = row.Cell(i + 1);
                            rowData[headerRow[i]] = cell.Value.ToString();
                        }
                        rows.Add(rowData);
                    }
                    else
                    {
                        var rowData = row.Cells().Select(c => c.Value.ToString()).ToArray();
                        rows.Add(rowData);
                    }
                }

                var result = new
                {
                    file_path = filePath,
                    sheet_name = worksheet.Name,
                    headers = headerRow,
                    rows
                };

                return Task.FromResult(McpToolResult.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading Excel file");
                return Task.FromResult(McpToolResult.Error($"Error reading Excel file: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Tool for converting Excel files to JSON
    /// </summary>
    public class ExcelToJsonTool : IMcpTool
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "excel_to_json";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Converts an Excel file to JSON";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["file_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path to the Excel file",
                    Required = true
                },
                ["sheet_name"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The name of the sheet to convert (default: first sheet)",
                    Required = false
                },
                ["output_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path to save the JSON file (default: same as Excel file with .json extension)",
                    Required = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Result of the conversion"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelToJsonTool"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public ExcelToJsonTool(ILogger logger)
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
                if (!parameters.TryGetValue("file_path", out var filePathObj) || filePathObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("File path parameter is required"));
                }

                var filePath = filePathObj.ToString() ?? "";
                var sheetName = parameters.TryGetValue("sheet_name", out var sheetNameObj) && sheetNameObj != null
                    ? sheetNameObj.ToString()
                    : null;
                var outputPath = parameters.TryGetValue("output_path", out var outputPathObj) && outputPathObj != null
                    ? outputPathObj.ToString()
                    : Path.ChangeExtension(filePath, ".json");

                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    return Task.FromResult(McpToolResult.Error($"File not found: {filePath}"));
                }

                // Read the Excel file
                using var workbook = new XLWorkbook(filePath);
                
                // Get the worksheet
                IXLWorksheet worksheet;
                if (string.IsNullOrEmpty(sheetName))
                {
                    worksheet = workbook.Worksheet(1);
                }
                else
                {
                    if (!workbook.TryGetWorksheet(sheetName, out worksheet))
                    {
                        return Task.FromResult(McpToolResult.Error($"Sheet not found: {sheetName}"));
                    }
                }

                // Get the used range
                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    return Task.FromResult(McpToolResult.Error("No data found in the worksheet"));
                }

                // Convert to a list of rows
                var rows = new List<Dictionary<string, string>>();
                var headerRow = range.FirstRow().Cells().Select(c => c.Value.ToString()).ToArray();
                
                foreach (var row in range.RowsUsed().Skip(1))
                {
                    var rowData = new Dictionary<string, string>();
                    for (int i = 0; i < headerRow.Length; i++)
                    {
                        var cell = row.Cell(i + 1);
                        rowData[headerRow[i]] = cell.Value.ToString();
                    }
                    rows.Add(rowData);
                }

                // Convert to JSON
                var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
                
                // Save to file
                File.WriteAllText(outputPath, json);

                var result = new
                {
                    file_path = filePath,
                    output_path = outputPath,
                    sheet_name = worksheet.Name,
                    row_count = rows.Count
                };

                return Task.FromResult(McpToolResult.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting Excel to JSON");
                return Task.FromResult(McpToolResult.Error($"Error converting Excel to JSON: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Tool for getting the list of sheets in an Excel file
    /// </summary>
    public class GetExcelSheetsTool : IMcpTool
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "excel_get_sheets";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Gets the list of sheets in an Excel file";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["file_path"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The path to the Excel file",
                    Required = true
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "List of sheets in the Excel file"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="GetExcelSheetsTool"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public GetExcelSheetsTool(ILogger logger)
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
                if (!parameters.TryGetValue("file_path", out var filePathObj) || filePathObj == null)
                {
                    return Task.FromResult(McpToolResult.Error("File path parameter is required"));
                }

                var filePath = filePathObj.ToString() ?? "";

                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    return Task.FromResult(McpToolResult.Error($"File not found: {filePath}"));
                }

                // Read the Excel file
                using var workbook = new XLWorkbook(filePath);
                
                // Get the list of sheets
                var sheets = workbook.Worksheets.Select(ws => new
                {
                    name = ws.Name,
                    visible = ws.Visibility == XLWorksheetVisibility.Visible,
                    row_count = ws.LastRowUsed()?.RowNumber() ?? 0,
                    column_count = ws.LastColumnUsed()?.ColumnNumber() ?? 0
                }).ToArray();

                var result = new
                {
                    file_path = filePath,
                    sheets
                };

                return Task.FromResult(McpToolResult.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Excel sheets");
                return Task.FromResult(McpToolResult.Error($"Error getting Excel sheets: {ex.Message}"));
            }
        }
    }
}
