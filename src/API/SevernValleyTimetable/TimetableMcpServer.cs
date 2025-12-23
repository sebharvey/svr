using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SevernValleyTimetable.Functions;

public class TimetableMcpServer
{
    private readonly ILogger<TimetableMcpServer> _logger;
    private readonly TimetableService _timetableService;

    public TimetableMcpServer(ILogger<TimetableMcpServer> logger, TimetableService timetableService)
    {
        _logger = logger;
        _timetableService = timetableService;
    }

/*
    [Function("TimetableMcpServer")]
    [McpServer(
        Name = "severn-valley-timetable",
        Description = "Query Severn Valley Railway timetables by date",
        Version = "1.0.0"
    )]
    public async Task<McpServerResponse> Run(
        [McpServerTrigger] McpServerRequest request)
    {
        _logger.LogInformation("MCP Server request received: {RequestType}", request.Method);

        try
        {
            return request.Method switch
            {
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolCallAsync(request),
                "resources/list" => HandleResourcesList(),
                "resources/read" => await HandleResourceReadAsync(request),
                _ => McpServerResponse.Error($"Unsupported method: {request.Method}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return McpServerResponse.Error($"Error: {ex.Message}");
        }
    }

    private McpServerResponse HandleToolsList()
    {
        var tools = new[]
        {
            new McpTool
            {
                Name = "get_timetable",
                Description = "Get the train timetable for a specific date. Returns the complete timetable with all train services, including train numbers, directions, stations, and times.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        date = new
                        {
                            type = "string",
                            description = "Date in YYYY-MM-DD format (e.g., 2025-12-26)"
                        },
                        debug = new
                        {
                            type = "boolean",
                            description = "If true, returns debug timetable instead of date-specific timetable (optional)",
                            @default = false
                        }
                    },
                    required = new[] { "date" }
                }
            },
            new McpTool
            {
                Name = "get_available_dates",
                Description = "Get a list of all dates that have scheduled timetables. Returns dates grouped by year.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        year = new
                        {
                            type = "integer",
                            description = "Year to query (e.g., 2025, 2026). If not specified, returns all available years."
                        }
                    }
                }
            },
            new McpTool
            {
                Name = "search_trains",
                Description = "Search for specific trains in a timetable by train number, direction, or station. Useful for finding when a specific train runs or checking services through a particular station.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        date = new
                        {
                            type = "string",
                            description = "Date in YYYY-MM-DD format (e.g., 2025-12-26)"
                        },
                        train_number = new
                        {
                            type = "string",
                            description = "Train number to search for (e.g., 'Steam 75069', 'Diesel DMU')"
                        },
                        direction = new
                        {
                            type = "string",
                            description = "Train direction: 'northbound' or 'southbound'"
                        },
                        station = new
                        {
                            type = "string",
                            description = "Station name to filter by (e.g., 'Kidderminster', 'Bridgnorth')"
                        }
                    },
                    required = new[] { "date" }
                }
            }
        };

        return McpServerResponse.Success(new { tools });
    }

    private async Task<McpServerResponse> HandleToolCallAsync(McpServerRequest request)
    {
        var toolCall = request.Params?.ToObject<McpToolCall>();
        if (toolCall == null)
        {
            return McpServerResponse.Error("Invalid tool call parameters");
        }

        _logger.LogInformation("Tool call: {ToolName}", toolCall.Name);

        return toolCall.Name switch
        {
            "get_timetable" => await GetTimetableAsync(toolCall.Arguments),
            "get_available_dates" => await GetAvailableDatesAsync(toolCall.Arguments),
            "search_trains" => await SearchTrainsAsync(toolCall.Arguments),
            _ => McpServerResponse.Error($"Unknown tool: {toolCall.Name}")
        };
    }

    private async Task<McpServerResponse> GetTimetableAsync(JsonElement? arguments)
    {
        if (!arguments.HasValue)
        {
            return McpServerResponse.Error("Missing required parameter: date");
        }

        var args = arguments.Value;
        
        if (!args.TryGetProperty("date", out var dateElement))
        {
            return McpServerResponse.Error("Missing required parameter: date");
        }

        var dateString = dateElement.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            return McpServerResponse.Error("Invalid date parameter");
        }

        if (!DateTime.TryParse(dateString, out var date))
        {
            return McpServerResponse.Error("Invalid date format. Use YYYY-MM-DD format.");
        }

        var debug = args.TryGetProperty("debug", out var debugElement) && debugElement.GetBoolean();

        try
        {
            var timetableJson = await _timetableService.GetTimetableForDateAsync(date, debug);
            var timetable = JsonSerializer.Deserialize<JsonElement>(timetableJson);
            
            return McpServerResponse.Success(new
            {
                date = date.ToString("yyyy-MM-dd"),
                timetable
            });
        }
        catch (FileNotFoundException)
        {
            return McpServerResponse.Error($"No timetable found for {date:yyyy-MM-dd}. The Severn Valley Railway may not be operating services on this date.");
        }
    }

    private async Task<McpServerResponse> GetAvailableDatesAsync(JsonElement? arguments)
    {
        var yearFilter = arguments?.TryGetProperty("year", out var yearElement) == true 
            ? yearElement.GetInt32() 
            : (int?)null;

        try
        {
            var availableDates = new Dictionary<int, List<string>>();

            // Check for 2025
            if (!yearFilter.HasValue || yearFilter == 2025)
            {
                var schedule2025 = await _timetableService.LoadScheduleAsync(2025);
                if (schedule2025 != null)
                {
                    availableDates[2025] = schedule2025.Select(s => ConvertScheduleDateToFullDate(s.Date, 2025)).ToList();
                }
            }

            // Check for 2026
            if (!yearFilter.HasValue || yearFilter == 2026)
            {
                var schedule2026 = await _timetableService.LoadScheduleAsync(2026);
                if (schedule2026 != null)
                {
                    availableDates[2026] = schedule2026.Select(s => ConvertScheduleDateToFullDate(s.Date, 2026)).ToList();
                }
            }

            return McpServerResponse.Success(new
            {
                available_dates = availableDates,
                total_dates = availableDates.Values.Sum(list => list.Count)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available dates");
            return McpServerResponse.Error($"Error retrieving available dates: {ex.Message}");
        }
    }

    private async Task<McpServerResponse> SearchTrainsAsync(JsonElement? arguments)
    {
        if (!arguments.HasValue)
        {
            return McpServerResponse.Error("Missing required parameter: date");
        }

        var args = arguments.Value;
        
        if (!args.TryGetProperty("date", out var dateElement))
        {
            return McpServerResponse.Error("Missing required parameter: date");
        }

        var dateString = dateElement.GetString();
        if (string.IsNullOrEmpty(dateString) || !DateTime.TryParse(dateString, out var date))
        {
            return McpServerResponse.Error("Invalid date format. Use YYYY-MM-DD format.");
        }

        var trainNumber = args.TryGetProperty("train_number", out var trainElement) 
            ? trainElement.GetString() 
            : null;
        
        var direction = args.TryGetProperty("direction", out var dirElement) 
            ? dirElement.GetString() 
            : null;
        
        var station = args.TryGetProperty("station", out var stationElement) 
            ? stationElement.GetString() 
            : null;

        try
        {
            var timetableJson = await _timetableService.GetTimetableForDateAsync(date, false);
            var timetable = JsonSerializer.Deserialize<TimetableData>(timetableJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (timetable?.Trains == null)
            {
                return McpServerResponse.Error("Invalid timetable data");
            }

            var filteredTrains = timetable.Trains.AsEnumerable();

            if (!string.IsNullOrEmpty(trainNumber))
            {
                filteredTrains = filteredTrains.Where(t => 
                    t.TrainNumber?.Contains(trainNumber, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (!string.IsNullOrEmpty(direction))
            {
                filteredTrains = filteredTrains.Where(t => 
                    t.Direction?.Equals(direction, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (!string.IsNullOrEmpty(station))
            {
                filteredTrains = filteredTrains.Where(t => 
                    t.Stops?.Any(s => s.Station?.Equals(station, StringComparison.OrdinalIgnoreCase) == true) == true);
            }

            var results = filteredTrains.ToList();

            return McpServerResponse.Success(new
            {
                date = date.ToString("yyyy-MM-dd"),
                search_criteria = new
                {
                    train_number = trainNumber,
                    direction,
                    station
                },
                total_results = results.Count,
                trains = results
            });
        }
        catch (FileNotFoundException)
        {
            return McpServerResponse.Error($"No timetable found for {date:yyyy-MM-dd}");
        }
    }

    private McpServerResponse HandleResourcesList()
    {
        var resources = new[]
        {
            new
            {
                uri = "timetable://current",
                name = "Current Day Timetable",
                description = "The timetable for today's date",
                mimeType = "application/json"
            },
            new
            {
                uri = "timetable://debug",
                name = "Debug Timetable",
                description = "Debug timetable for testing purposes",
                mimeType = "application/json"
            }
        };

        return McpServerResponse.Success(new { resources });
    }

    private async Task<McpServerResponse> HandleResourceReadAsync(McpServerRequest request)
    {
        var resourceRead = request.Params?.ToObject<McpResourceRead>();
        if (resourceRead == null || string.IsNullOrEmpty(resourceRead.Uri))
        {
            return McpServerResponse.Error("Invalid resource URI");
        }

        try
        {
            var timetableJson = resourceRead.Uri switch
            {
                "timetable://current" => await _timetableService.GetTimetableForDateAsync(DateTime.UtcNow, false),
                "timetable://debug" => await _timetableService.GetTimetableForDateAsync(DateTime.UtcNow, true),
                _ => throw new ArgumentException($"Unknown resource URI: {resourceRead.Uri}")
            };

            return McpServerResponse.Success(new
            {
                contents = new[]
                {
                    new
                    {
                        uri = resourceRead.Uri,
                        mimeType = "application/json",
                        text = timetableJson
                    }
                }
            });
        }
        catch (FileNotFoundException)
        {
            return McpServerResponse.Error("No timetable found for the requested date");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading resource");
            return McpServerResponse.Error($"Error: {ex.Message}");
        }
    }

    private string ConvertScheduleDateToFullDate(string scheduleDate, int year)
    {
        // Schedule date format: "dd-MMM" (e.g., "26-Dec")
        // Output format: "yyyy-MM-dd" (e.g., "2025-12-26")
        
        try
        {
            var date = DateTime.ParseExact($"{scheduleDate}-{year}", "dd-MMM-yyyy", 
                System.Globalization.CultureInfo.InvariantCulture);
            return date.ToString("yyyy-MM-dd");
        }
        catch
        {
            return $"{year}-??-?? ({scheduleDate})";
        }
    }

    // Helper classes for MCP deserialization
    private class McpToolCall
    {
        public string Name { get; set; } = string.Empty;
        public JsonElement? Arguments { get; set; }
    }

    private class McpResourceRead
    {
        public string Uri { get; set; } = string.Empty;
    }

    */
}