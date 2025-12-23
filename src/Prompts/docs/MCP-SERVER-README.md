# Severn Valley Railway Timetable MCP Server

This document describes the Model Context Protocol (MCP) server for querying Severn Valley Railway timetables.

## Overview

The MCP server provides three main tools for interacting with the timetable system:
1. **get_timetable** - Retrieve complete timetable for a specific date
2. **get_available_dates** - List all dates with scheduled services
3. **search_trains** - Search for specific trains by number, direction, or station

## Server Configuration

### Endpoint
```
https://svrliveapi-aaeydueba4b9aveb.uksouth-01.azurewebsites.net/api/TimetableMcpServer
```

### Server Details
- **Name**: `severn-valley-timetable`
- **Description**: Query Severn Valley Railway timetables by date
- **Version**: 1.0.0

## Tools

### 1. get_timetable

Retrieves the complete train timetable for a specific date.

**Parameters:**
- `date` (required): Date in YYYY-MM-DD format (e.g., "2025-12-26")
- `debug` (optional): Boolean, returns debug timetable if true (default: false)

**Example Request:**
```json
{
  "method": "tools/call",
  "params": {
    "name": "get_timetable",
    "arguments": {
      "date": "2025-12-26"
    }
  }
}
```

**Example Response:**
```json
{
  "date": "2025-12-26",
  "timetable": {
    "name": "Timetable B",
    "trains": [
      {
        "trainNumber": "Steam 75069",
        "direction": "southbound",
        "stops": [
          {
            "station": "Kidderminster",
            "departure": "10:15",
            "stopsAt": true
          },
          ...
        ]
      },
      ...
    ]
  }
}
```

**Error Response (No Timetable):**
```json
{
  "error": "No timetable found for 2025-12-25. The Severn Valley Railway may not be operating services on this date."
}
```

### 2. get_available_dates

Lists all dates that have scheduled timetables.

**Parameters:**
- `year` (optional): Integer, filter by specific year (e.g., 2025, 2026)

**Example Request (All Years):**
```json
{
  "method": "tools/call",
  "params": {
    "name": "get_available_dates",
    "arguments": {}
  }
}
```

**Example Request (Specific Year):**
```json
{
  "method": "tools/call",
  "params": {
    "name": "get_available_dates",
    "arguments": {
      "year": 2025
    }
  }
}
```

**Example Response:**
```json
{
  "available_dates": {
    "2025": [
      "2025-10-24",
      "2025-10-25",
      "2025-10-26",
      "2025-12-26",
      "2025-12-27",
      "2025-12-28",
      "2025-12-29",
      "2025-12-30",
      "2025-12-31"
    ],
    "2026": [
      "2026-01-01",
      "2026-01-02",
      "2026-01-03",
      "2026-01-04"
    ]
  },
  "total_dates": 13
}
```

### 3. search_trains

Searches for specific trains in a timetable by train number, direction, or station.

**Parameters:**
- `date` (required): Date in YYYY-MM-DD format
- `train_number` (optional): Train number to search for (e.g., "Steam 75069", "Diesel DMU")
- `direction` (optional): "northbound" or "southbound"
- `station` (optional): Station name (e.g., "Kidderminster", "Bridgnorth")

**Example Request (Search by Train Number):**
```json
{
  "method": "tools/call",
  "params": {
    "name": "search_trains",
    "arguments": {
      "date": "2025-12-26",
      "train_number": "Steam 75069"
    }
  }
}
```

**Example Request (Search by Station):**
```json
{
  "method": "tools/call",
  "params": {
    "name": "search_trains",
    "arguments": {
      "date": "2025-12-26",
      "station": "Highley",
      "direction": "northbound"
    }
  }
}
```

**Example Response:**
```json
{
  "date": "2025-12-26",
  "search_criteria": {
    "train_number": "Steam 75069",
    "direction": null,
    "station": null
  },
  "total_results": 2,
  "trains": [
    {
      "trainNumber": "Steam 75069",
      "direction": "southbound",
      "stops": [...]
    },
    {
      "trainNumber": "Steam 75069",
      "direction": "northbound",
      "stops": [...]
    }
  ]
}
```

## Resources

The MCP server also exposes two resources:

### 1. Current Day Timetable
- **URI**: `timetable://current`
- **Description**: The timetable for today's date
- **MIME Type**: application/json

### 2. Debug Timetable
- **URI**: `timetable://debug`
- **Description**: Debug timetable for testing purposes
- **MIME Type**: application/json

**Example Resource Read Request:**
```json
{
  "method": "resources/read",
  "params": {
    "uri": "timetable://current"
  }
}
```

## Using in Claude Desktop

To use this MCP server in Claude Desktop, add the following to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "severn-valley-timetable": {
      "url": "https://svrliveapi-aaeydueba4b9aveb.uksouth-01.azurewebsites.net/api/TimetableMcpServer",
      "type": "remote"
    }
  }
}
```

### Location of config file:
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

## Example Usage Scenarios

### Scenario 1: Planning a Visit
```
User: "Are there trains running on Boxing Day 2025?"
Claude uses get_available_dates → checks if 2025-12-26 is available
Claude uses get_timetable with date 2025-12-26 → gets full timetable
Claude responds with service details
```

### Scenario 2: Finding a Specific Train
```
User: "When does Steam 75069 run on December 26th?"
Claude uses search_trains with train_number="Steam 75069" and date="2025-12-26"
Claude responds with all services for that train
```

### Scenario 3: Checking Station Services
```
User: "What trains stop at Highley on December 29th?"
Claude uses search_trains with station="Highley" and date="2025-12-29"
Claude responds with all trains serving that station
```

### Scenario 4: Finding Steam Services
```
User: "Are there any steam trains on January 3rd 2026?"
Claude uses search_trains with train_number="Steam" and date="2026-01-03"
Claude responds with all steam locomotive services
```

## Error Handling

The MCP server returns descriptive error messages for common issues:

- **Invalid date format**: "Invalid date format. Use YYYY-MM-DD format."
- **Missing required parameter**: "Missing required parameter: date"
- **No timetable available**: "No timetable found for {date}. The Severn Valley Railway may not be operating services on this date."
- **Invalid tool**: "Unknown tool: {tool_name}"

## Development and Testing

### Testing with Debug Mode
Use the debug parameter to test with a standard timetable:

```json
{
  "method": "tools/call",
  "params": {
    "name": "get_timetable",
    "arguments": {
      "date": "2025-12-26",
      "debug": true
    }
  }
}
```

### Testing Locally
When running locally, the MCP server is available at:
```
http://localhost:7071/api/TimetableMcpServer
```

## Station Names

The following stations are recognized (case-insensitive):
- Kidderminster
- Bewdley
- Arley
- Highley
- Hampton Loade
- Bridgnorth

## Train Number Formats

Train numbers typically follow these patterns:
- Steam locomotives: "Steam 75069", "Steam 7802"
- Diesel locomotives: "Diesel 37248", "Diesel 46045"
- DMU (Diesel Multiple Unit): "Diesel DMU"

## API Integration

The MCP server uses the same underlying `TimetableService` as the REST API, ensuring consistency across all access methods:

- **REST API**: `GET /api/timetable`
- **MCP Server**: `tools/call` with `get_timetable`
- **Health Check**: `GET /api/health`

## Limitations

- Date range limited to configured timetables (currently 2025-2026)
- Search is case-insensitive but requires exact station names
- Train number search uses partial matching (contains)
- Station name search requires exact match
- Resources (timetable://current, timetable://debug) return current date or debug timetable only

## Support

For issues or questions:
1. Check the health endpoint: `GET /api/health`
2. Verify the date is in the available dates list
3. Ensure station names match exactly (case-insensitive)
4. Check Application Insights logs in Azure

## Version History

### Version 1.0.0 (December 2025)
- Initial MCP server implementation
- Three tools: get_timetable, get_available_dates, search_trains
- Two resources: current and debug timetables
- Support for 2025-2026 timetables
