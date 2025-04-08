using Adept.Common.Interfaces;
using Adept.Core.Interfaces;
using Adept.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Adept.Services.Mcp
{
    /// <summary>
    /// Provider for calendar tools
    /// </summary>
    public class CalendarToolProvider : IMcpToolProvider
    {
        private readonly ISecureStorageService _secureStorageService;
        private readonly ILogger<CalendarToolProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly List<IMcpTool> _tools = new List<IMcpTool>();
        private string? _googleCalendarApiKey;
        private string? _googleCalendarClientId;
        private string? _googleCalendarClientSecret;
        private string? _googleCalendarRefreshToken;
        private string? _googleCalendarAccessToken;
        private DateTime _googleCalendarTokenExpiry = DateTime.MinValue;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Calendar";

        /// <summary>
        /// Gets the available tools
        /// </summary>
        public IEnumerable<IMcpTool> Tools => _tools;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarToolProvider"/> class
        /// </summary>
        /// <param name="secureStorageService">The secure storage service</param>
        /// <param name="logger">The logger</param>
        public CalendarToolProvider(ISecureStorageService secureStorageService, ILogger<CalendarToolProvider> logger)
        {
            _secureStorageService = secureStorageService;
            _logger = logger;
            _httpClient = new HttpClient();

            // Initialize tools
            _tools.Add(new ListCalendarEventsTool(this, _logger));
            _tools.Add(new CreateCalendarEventTool(this, _logger));
            _tools.Add(new UpdateCalendarEventTool(this, _logger));
            _tools.Add(new DeleteCalendarEventTool(this, _logger));
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Get the Google Calendar API credentials
                _googleCalendarApiKey = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_api_key");
                _googleCalendarClientId = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_client_id");
                _googleCalendarClientSecret = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_client_secret");
                _googleCalendarRefreshToken = await _secureStorageService.RetrieveSecureValueAsync("google_calendar_refresh_token");
                
                if (string.IsNullOrEmpty(_googleCalendarRefreshToken))
                {
                    _logger.LogWarning("Google Calendar refresh token not found in secure storage");
                }
                else
                {
                    _logger.LogInformation("Google Calendar credentials retrieved from secure storage");
                    
                    // Refresh the access token
                    await RefreshAccessTokenAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Calendar tool provider");
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
                // Ensure we have a valid access token
                if (NeedsTokenRefresh())
                {
                    await RefreshAccessTokenAsync();
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
        /// Sets the Google Calendar API credentials
        /// </summary>
        /// <param name="apiKey">The API key</param>
        /// <param name="clientId">The client ID</param>
        /// <param name="clientSecret">The client secret</param>
        public async Task SetGoogleCalendarCredentialsAsync(string apiKey, string clientId, string clientSecret)
        {
            try
            {
                await _secureStorageService.StoreSecureValueAsync("google_calendar_api_key", apiKey);
                await _secureStorageService.StoreSecureValueAsync("google_calendar_client_id", clientId);
                await _secureStorageService.StoreSecureValueAsync("google_calendar_client_secret", clientSecret);
                
                _googleCalendarApiKey = apiKey;
                _googleCalendarClientId = clientId;
                _googleCalendarClientSecret = clientSecret;
                
                _logger.LogInformation("Google Calendar credentials stored in secure storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing Google Calendar credentials");
                throw;
            }
        }

        /// <summary>
        /// Sets the Google Calendar refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token</param>
        public async Task SetGoogleCalendarRefreshTokenAsync(string refreshToken)
        {
            try
            {
                await _secureStorageService.StoreSecureValueAsync("google_calendar_refresh_token", refreshToken);
                _googleCalendarRefreshToken = refreshToken;
                
                // Refresh the access token
                await RefreshAccessTokenAsync();
                
                _logger.LogInformation("Google Calendar refresh token stored in secure storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing Google Calendar refresh token");
                throw;
            }
        }

        /// <summary>
        /// Gets the Google Calendar access token
        /// </summary>
        /// <returns>The access token</returns>
        public string? GetAccessToken()
        {
            return _googleCalendarAccessToken;
        }

        /// <summary>
        /// Checks if the access token needs to be refreshed
        /// </summary>
        /// <returns>True if the token needs to be refreshed, false otherwise</returns>
        private bool NeedsTokenRefresh()
        {
            // Refresh if we don't have an access token or it's about to expire
            return string.IsNullOrEmpty(_googleCalendarAccessToken) || 
                   DateTime.UtcNow.AddMinutes(5) >= _googleCalendarTokenExpiry;
        }

        /// <summary>
        /// Refreshes the access token
        /// </summary>
        private async Task RefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_googleCalendarClientId) || 
                string.IsNullOrEmpty(_googleCalendarClientSecret) || 
                string.IsNullOrEmpty(_googleCalendarRefreshToken))
            {
                _logger.LogWarning("Cannot refresh Google Calendar access token: missing credentials");
                return;
            }

            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _googleCalendarClientId,
                    ["client_secret"] = _googleCalendarClientSecret,
                    ["refresh_token"] = _googleCalendarRefreshToken,
                    ["grant_type"] = "refresh_token"
                });

                var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                _googleCalendarAccessToken = tokenResponse.GetProperty("access_token").GetString();
                var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
                _googleCalendarTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

                _logger.LogInformation("Google Calendar access token refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Google Calendar access token");
                throw;
            }
        }

        /// <summary>
        /// Performs a Google Calendar API request
        /// </summary>
        /// <param name="method">The HTTP method</param>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="content">The request content</param>
        /// <returns>The API response</returns>
        public async Task<JsonElement> PerformCalendarRequestAsync(HttpMethod method, string endpoint, object? content = null)
        {
            if (string.IsNullOrEmpty(_googleCalendarAccessToken))
            {
                throw new InvalidOperationException("Google Calendar access token not available");
            }

            try
            {
                var request = new HttpRequestMessage(method, $"https://www.googleapis.com/calendar/v3/{endpoint}");
                request.Headers.Add("Authorization", $"Bearer {_googleCalendarAccessToken}");

                if (content != null)
                {
                    var json = JsonSerializer.Serialize(content);
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing Google Calendar API request");
                throw;
            }
        }
    }

    /// <summary>
    /// Tool for listing calendar events
    /// </summary>
    public class ListCalendarEventsTool : IMcpTool
    {
        private readonly CalendarToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "calendar_list_events";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Lists events from Google Calendar";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["calendar_id"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The calendar ID (use 'primary' for the primary calendar)",
                    Required = false,
                    DefaultValue = "primary"
                },
                ["start_date"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The start date in YYYY-MM-DD format",
                    Required = true
                },
                ["end_date"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The end date in YYYY-MM-DD format",
                    Required = false
                },
                ["max_results"] = new McpParameterSchema
                {
                    Type = "integer",
                    Description = "The maximum number of events to return",
                    Required = false,
                    DefaultValue = 10
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "List of calendar events"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ListCalendarEventsTool"/> class
        /// </summary>
        /// <param name="provider">The calendar tool provider</param>
        /// <param name="logger">The logger</param>
        public ListCalendarEventsTool(CalendarToolProvider provider, ILogger logger)
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
                // Get parameters
                var calendarId = parameters.TryGetValue("calendar_id", out var calendarIdObj) && calendarIdObj != null
                    ? calendarIdObj.ToString() ?? "primary"
                    : "primary";

                if (!parameters.TryGetValue("start_date", out var startDateObj) || startDateObj == null)
                {
                    return McpToolResult.Error("Start date parameter is required");
                }

                var startDate = startDateObj.ToString() ?? DateTime.Today.ToString("yyyy-MM-dd");
                
                var endDate = parameters.TryGetValue("end_date", out var endDateObj) && endDateObj != null
                    ? endDateObj.ToString()
                    : null;

                var maxResults = parameters.TryGetValue("max_results", out var maxResultsObj) && maxResultsObj != null
                    ? Convert.ToInt32(maxResultsObj)
                    : 10;

                // Build the query parameters
                var queryParams = new List<string>
                {
                    $"maxResults={maxResults}",
                    $"timeMin={Uri.EscapeDataString(DateTime.Parse(startDate).ToString("o"))}"
                };

                if (!string.IsNullOrEmpty(endDate))
                {
                    queryParams.Add($"timeMax={Uri.EscapeDataString(DateTime.Parse(endDate).ToString("o"))}");
                }

                var endpoint = $"calendars/{Uri.EscapeDataString(calendarId)}/events?{string.Join("&", queryParams)}";
                var result = await _provider.PerformCalendarRequestAsync(HttpMethod.Get, endpoint);

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing calendar events");
                return McpToolResult.Error($"Error listing calendar events: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for creating a calendar event
    /// </summary>
    public class CreateCalendarEventTool : IMcpTool
    {
        private readonly CalendarToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "calendar_create_event";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Creates an event in Google Calendar";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["calendar_id"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The calendar ID (use 'primary' for the primary calendar)",
                    Required = false,
                    DefaultValue = "primary"
                },
                ["summary"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The event summary/title",
                    Required = true
                },
                ["description"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The event description",
                    Required = false
                },
                ["location"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The event location",
                    Required = false
                },
                ["start_datetime"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The start date and time in ISO format (YYYY-MM-DDTHH:MM:SS)",
                    Required = true
                },
                ["end_datetime"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The end date and time in ISO format (YYYY-MM-DDTHH:MM:SS)",
                    Required = true
                },
                ["timezone"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The timezone for the event",
                    Required = false,
                    DefaultValue = "Europe/London"
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Created calendar event"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateCalendarEventTool"/> class
        /// </summary>
        /// <param name="provider">The calendar tool provider</param>
        /// <param name="logger">The logger</param>
        public CreateCalendarEventTool(CalendarToolProvider provider, ILogger logger)
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
                // Get parameters
                var calendarId = parameters.TryGetValue("calendar_id", out var calendarIdObj) && calendarIdObj != null
                    ? calendarIdObj.ToString() ?? "primary"
                    : "primary";

                if (!parameters.TryGetValue("summary", out var summaryObj) || summaryObj == null)
                {
                    return McpToolResult.Error("Summary parameter is required");
                }

                if (!parameters.TryGetValue("start_datetime", out var startDateTimeObj) || startDateTimeObj == null)
                {
                    return McpToolResult.Error("Start date/time parameter is required");
                }

                if (!parameters.TryGetValue("end_datetime", out var endDateTimeObj) || endDateTimeObj == null)
                {
                    return McpToolResult.Error("End date/time parameter is required");
                }

                var summary = summaryObj.ToString() ?? "";
                var description = parameters.TryGetValue("description", out var descriptionObj) && descriptionObj != null
                    ? descriptionObj.ToString()
                    : null;
                var location = parameters.TryGetValue("location", out var locationObj) && locationObj != null
                    ? locationObj.ToString()
                    : null;
                var startDateTime = startDateTimeObj.ToString() ?? "";
                var endDateTime = endDateTimeObj.ToString() ?? "";
                var timezone = parameters.TryGetValue("timezone", out var timezoneObj) && timezoneObj != null
                    ? timezoneObj.ToString() ?? "Europe/London"
                    : "Europe/London";

                // Create the event
                var eventData = new
                {
                    summary = summary,
                    description = description,
                    location = location,
                    start = new
                    {
                        dateTime = startDateTime,
                        timeZone = timezone
                    },
                    end = new
                    {
                        dateTime = endDateTime,
                        timeZone = timezone
                    }
                };

                var endpoint = $"calendars/{Uri.EscapeDataString(calendarId)}/events";
                var result = await _provider.PerformCalendarRequestAsync(HttpMethod.Post, endpoint, eventData);

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar event");
                return McpToolResult.Error($"Error creating calendar event: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for updating a calendar event
    /// </summary>
    public class UpdateCalendarEventTool : IMcpTool
    {
        private readonly CalendarToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "calendar_update_event";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Updates an event in Google Calendar";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["calendar_id"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The calendar ID (use 'primary' for the primary calendar)",
                    Required = false,
                    DefaultValue = "primary"
                },
                ["event_id"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The ID of the event to update",
                    Required = true
                },
                ["summary"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The event summary/title",
                    Required = false
                },
                ["description"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The event description",
                    Required = false
                },
                ["location"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The event location",
                    Required = false
                },
                ["start_datetime"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The start date and time in ISO format (YYYY-MM-DDTHH:MM:SS)",
                    Required = false
                },
                ["end_datetime"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The end date and time in ISO format (YYYY-MM-DDTHH:MM:SS)",
                    Required = false
                },
                ["timezone"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The timezone for the event",
                    Required = false,
                    DefaultValue = "Europe/London"
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Updated calendar event"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateCalendarEventTool"/> class
        /// </summary>
        /// <param name="provider">The calendar tool provider</param>
        /// <param name="logger">The logger</param>
        public UpdateCalendarEventTool(CalendarToolProvider provider, ILogger logger)
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
                // Get parameters
                var calendarId = parameters.TryGetValue("calendar_id", out var calendarIdObj) && calendarIdObj != null
                    ? calendarIdObj.ToString() ?? "primary"
                    : "primary";

                if (!parameters.TryGetValue("event_id", out var eventIdObj) || eventIdObj == null)
                {
                    return McpToolResult.Error("Event ID parameter is required");
                }

                var eventId = eventIdObj.ToString() ?? "";
                
                // Build the event data
                var eventData = new Dictionary<string, object>();
                
                if (parameters.TryGetValue("summary", out var summaryObj) && summaryObj != null)
                {
                    eventData["summary"] = summaryObj.ToString() ?? "";
                }
                
                if (parameters.TryGetValue("description", out var descriptionObj) && descriptionObj != null)
                {
                    eventData["description"] = descriptionObj.ToString() ?? "";
                }
                
                if (parameters.TryGetValue("location", out var locationObj) && locationObj != null)
                {
                    eventData["location"] = locationObj.ToString() ?? "";
                }
                
                var timezone = parameters.TryGetValue("timezone", out var timezoneObj) && timezoneObj != null
                    ? timezoneObj.ToString() ?? "Europe/London"
                    : "Europe/London";
                
                if (parameters.TryGetValue("start_datetime", out var startDateTimeObj) && startDateTimeObj != null)
                {
                    eventData["start"] = new
                    {
                        dateTime = startDateTimeObj.ToString(),
                        timeZone = timezone
                    };
                }
                
                if (parameters.TryGetValue("end_datetime", out var endDateTimeObj) && endDateTimeObj != null)
                {
                    eventData["end"] = new
                    {
                        dateTime = endDateTimeObj.ToString(),
                        timeZone = timezone
                    };
                }
                
                // Update the event
                var endpoint = $"calendars/{Uri.EscapeDataString(calendarId)}/events/{Uri.EscapeDataString(eventId)}";
                var result = await _provider.PerformCalendarRequestAsync(HttpMethod.Patch, endpoint, eventData);

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calendar event");
                return McpToolResult.Error($"Error updating calendar event: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for deleting a calendar event
    /// </summary>
    public class DeleteCalendarEventTool : IMcpTool
    {
        private readonly CalendarToolProvider _provider;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "calendar_delete_event";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Deletes an event from Google Calendar";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["calendar_id"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The calendar ID (use 'primary' for the primary calendar)",
                    Required = false,
                    DefaultValue = "primary"
                },
                ["event_id"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The ID of the event to delete",
                    Required = true
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Result of the delete operation"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteCalendarEventTool"/> class
        /// </summary>
        /// <param name="provider">The calendar tool provider</param>
        /// <param name="logger">The logger</param>
        public DeleteCalendarEventTool(CalendarToolProvider provider, ILogger logger)
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
                // Get parameters
                var calendarId = parameters.TryGetValue("calendar_id", out var calendarIdObj) && calendarIdObj != null
                    ? calendarIdObj.ToString() ?? "primary"
                    : "primary";

                if (!parameters.TryGetValue("event_id", out var eventIdObj) || eventIdObj == null)
                {
                    return McpToolResult.Error("Event ID parameter is required");
                }

                var eventId = eventIdObj.ToString() ?? "";
                
                // Delete the event
                var endpoint = $"calendars/{Uri.EscapeDataString(calendarId)}/events/{Uri.EscapeDataString(eventId)}";
                await _provider.PerformCalendarRequestAsync(HttpMethod.Delete, endpoint);

                return McpToolResult.Ok(new { deleted = true, eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar event");
                return McpToolResult.Error($"Error deleting calendar event: {ex.Message}");
            }
        }
    }
}
