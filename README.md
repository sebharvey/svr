# Severn Valley Live Train Tracker

A real-time visualization of train movements on the Severn Valley Railway line between Kidderminster and Bridgnorth. The system consists of a web frontend and an Azure Functions backend API that serves date-appropriate timetables.

https://svrlive.omegasoft.co.uk

## Features

### Core Functionality

- **Live Train Tracking**: Real-time visualization of train positions on the railway line
- **API-Driven Timetables**: Timetable data fetched from an Azure Functions backend, automatically selecting the correct timetable for today's date
- **Intelligent Train Display**: Each train appears only once, showing its current or next service
- **Status Panel**: Detailed information about each active train's current status
- **Terminus Management**: Smart handling of trains waiting at terminal stations
- **Color-Coded Trains**: Consistent color assignment per train throughout the day
- **System Health Status**: Live indicator showing whether the backend API is online

### Visual Design

- **Vertical Track Layout**: Stations displayed from Kidderminster (top) to Bridgnorth (bottom)
- **Directional Positioning**:
  - Northbound trains on the left (moving upward)
  - Southbound trains on the right (moving downward)
- **Dynamic Icons**: Steam (🚂), DMU (🚃), and Diesel (🚆) locomotives
- **Light/Dark Theme**: Toggle between light and dark mode; defaults to OS preference and persists across sessions
- **Live Clock**: Real-time HH:MM:SS clock displayed at the top
- **Mobile-Responsive**: Optimized for iPhone and mobile browsers

## Table of Contents

- [Repository Structure](#repository-structure)
- [Installation](#installation)
- [Usage](#usage)
- [Debug Mode](#debug-mode)
- [API Reference](#api-reference)
- [Data Format](#data-format)
- [Architecture](#architecture)
- [Algorithm Details](#algorithm-details)
- [Customization](#customization)
- [Browser Compatibility](#browser-compatibility)
- [Troubleshooting](#troubleshooting)

## Repository Structure

```
svr/
├── README.md
├── CLAUDE.MD
├── docs/
│   ├── MCP-SERVER-README.md   # MCP server documentation
│   └── TimetablePrompt.md
└── src/
    ├── Web/
    │   ├── index.html         # Frontend markup
    │   ├── styles.css         # Styles (light and dark themes)
    │   └── system.js          # All frontend logic
    └── API/
        └── SevernValleyTimetable/
            ├── SevernValleyTimetable.sln
            ├── SevernValleyTimetable.csproj
            ├── Program.cs
            ├── TimetableFunction.cs   # GET /api/timetable
            ├── HealthCheckFunction.cs # GET /api/health
            ├── TimetableMcpServer.cs  # MCP server (currently disabled)
            ├── TimetableService.cs    # Core scheduling logic + caching
            ├── host.json
            ├── local.settings.json
            └── Timetables/
                ├── debug.json         # Debug/test timetable
                ├── 2025/
                │   ├── schedule.json  # Date → timetable mapping for 2025
                │   └── *.json         # Individual timetable files
                └── 2026/
                    ├── schedule.json  # Date → timetable mapping for 2026
                    └── *.json         # Individual timetable files
```

## Installation

### Frontend

The web frontend is a set of static files (`index.html`, `styles.css`, `system.js`) that can be served from any static web host.

```bash
# Serve locally with Python
python -m http.server 8000 --directory src/Web

# Or with Node.js http-server
npx http-server src/Web

# Then navigate to http://localhost:8000
```

The frontend fetches timetable data from the hosted Azure Functions API by default. For local development against a local API, update the `apiUrl` in `system.js`.

### Backend API (Azure Functions)

Requirements: .NET 8 SDK, Azure Functions Core Tools v4.

```bash
cd src/API/SevernValleyTimetable
func start
```

The API will be available at `http://localhost:7071`.

## Usage

### Theme Toggle

A light/dark mode button is shown in the top-right corner. The theme defaults to the OS preference and any explicit user choice is persisted in `localStorage`.

### Reading the Display

**Timetable Info**

- The live HH:MM:SS clock is shown at the top
- When a timetable is loaded, the timetable name and date appear below the clock
- If no timetable is scheduled for today, a message is displayed instead of the track diagram

**Station Display**

- Gray boxes represent stations, Kidderminster at the top, Bridgnorth at the bottom
- Trains shown inside station boxes when stopped

**Track Sections**

- Darker areas between stations represent track segments
- The central vertical line is the railway
- Trains are positioned dynamically based on progress between stations

**Train Indicators**

- Icon (🚂/🚃/🚆) + Train Number + Direction Arrow (↑/↓)
- Left side = Northbound, Right side = Southbound
- Color-coded border and background (consistent per train)

**Status Panel**

- Shows all active trains below the track diagram
- Displays current activity (traveling, waiting, terminated)
- Countdown timers for arrivals and departures
- Non-stop passing information

**System Status**

- Bottom bar shows whether the backend API is online or unavailable
- Checked on page load and every 60 seconds

### Auto-Refresh

- In live mode the tracker re-renders every 30 seconds automatically

## Debug Mode

Append `?debug=true` to the page URL to enable debug mode:

```
https://svrlive.omegasoft.co.uk?debug=true
```

Debug mode:
- Shows manual time controls (◀ / ▶ arrows and "Live Time" button) to step through time in 5-minute increments
- Causes the API to return the `debug.json` timetable instead of the date-appropriate one

## API Reference

Base URL: `https://svrliveapi-aaeydueba4b9aveb.uksouth-01.azurewebsites.net`

### GET /api/timetable

Returns the timetable for today's UTC date. The service reads `schedule.json` for the current year to determine which timetable file applies.

**Query Parameters**

| Parameter | Type    | Description                                            |
|-----------|---------|--------------------------------------------------------|
| `debug`   | boolean | If `true`, returns `debug.json` instead of today's timetable |

**Responses**

| Status | Description                        |
|--------|------------------------------------|
| 200    | JSON timetable object              |
| 404    | No timetable scheduled for today   |
| 500    | Unexpected server error            |

### GET /api/health

Returns system health information, including a count of available timetable files.

**Response (200)**

```json
{
  "status": "healthy",
  "timestamp": "2026-03-03T10:00:00Z",
  "service": "SevernValleyTimetable",
  "version": "1.0.0",
  "timetablesAvailable": 12,
  "checks": {
    "timetableService": "ok",
    "fileSystem": "ok"
  }
}
```

## Data Format

### Schedule File (`schedule.json`)

Each year directory contains a `schedule.json` that maps calendar dates to timetable file names:

```json
[
  { "date": "01-Jan", "timetable": "timetable-b" },
  { "date": "03-Jan", "timetable": "winter-steam-gala-sat" }
]
```

The `date` field uses `dd-MMM` format. Dates not listed return a 404.

### Timetable JSON Structure

```json
{
  "name": "Timetable B",
  "date": "Saturday",
  "trains": [
    {
      "trainNumber": "Diesel 37248",
      "direction": "northbound",
      "stops": [
        {
          "station": "Bridgnorth",
          "departure": "09:40",
          "stopsAt": true
        },
        {
          "station": "Hampton Loade",
          "time": "09:58",
          "stopsAt": false
        },
        {
          "station": "Kidderminster",
          "arrival": "10:40",
          "stopsAt": true
        }
      ]
    }
  ]
}
```

### Field Definitions

**Root Level**

- `name` (string): Timetable name shown in the UI header
- `date` (string): Operating date description shown in the UI header
- `trains` (array): Array of train service objects

**Train Object**

- `trainNumber` (string): Unique identifier for the physical train (e.g., "Diesel 37248", "Steam 75069")
- `direction` (string): Either `"northbound"` or `"southbound"`
- `stops` (array): Ordered list of station stops

**Stop Object**

- `station` (string): Station name (must match station names in the timetable)
- `stopsAt` (boolean): Whether the train stops at this station
- `departure` (string, optional): Departure time in HH:MM format
- `arrival` (string, optional): Arrival time in HH:MM format
- `time` (string, optional): Pass-through time for non-stop stations

**Time Field Rules**

- First stop: must have `departure`
- Last stop: must have `arrival`
- Intermediate stops where the train stops: must have both `arrival` and `departure`
- Non-stop stations: use `time` with `stopsAt: false`

### Station List

Stations are extracted dynamically from the timetable at runtime (from the service with the most stops). The canonical order is Kidderminster to Bridgnorth:

1. Kidderminster
2. Bewdley
3. Arley
4. Highley
5. Hampton Loade
6. Bridgnorth

## Architecture

### Technology Stack

**Frontend**

- HTML5, CSS3 (Flexbox, CSS custom properties)
- Vanilla JavaScript — no frameworks or build step
- `localStorage` for theme persistence

**Backend**

- .NET 8 Azure Functions (isolated worker model)
- Hosted on Azure App Service (UK South)
- Application Insights telemetry
- JSON timetable files stored alongside the function app

### Key Frontend Components (`system.js`)

#### Theme System

- `resolveTheme()`: Reads `localStorage` or falls back to `prefers-color-scheme`
- `applyTheme(theme)`: Adds/removes `light-mode` class on `<body>`
- Theme applied before `DOMContentLoaded` to avoid flash of wrong theme

#### Data Loading

- `loadTimetable()`: Fetches timetable JSON from the API; handles 404 gracefully
- `updateTimetableData()`: Re-extracts stations and re-assigns train colors after each load
- `extractStations()`: Derives ordered station list from the service with the most stops

#### State Management

- `isLiveMode`: Boolean flag; `true` by default
- `manualTime`: Minutes since midnight, used only in debug/manual mode
- `refreshInterval`: Auto-refresh timer (30 s, live mode only)
- `clockInterval`: 1-second clock timer
- `timetableData`: The loaded timetable JSON object

#### Core Functions

**Time**

- `getCurrentTime()`: Returns current time based on mode
- `parseTime(timeStr)`: Converts HH:MM to minutes since midnight
- `formatTime(minutes)`: Converts minutes to HH:MM
- `getStopTime(stop)`: Extracts a comparable time from a stop object

**Position Calculation**

- `findTrainPosition(train, currentTime)`: Returns position object or `null`
- `getNextService(trainNumber, station, currentTime)`: Finds next departure for a train from a given station

**Rendering**

- `renderTracker()`: Main render — builds station and track-section DOM
- `createTrainElement(train, topPercentage)`: Creates a train DOM element
- `renderStatus(activeTrains, currentTime)`: Renders the status panel
- `generateStatusText(train, position, currentTime)`: Produces the human-readable status string

**Utility**

- `getTrainIcon(trainNumber)`: Returns emoji based on train type keyword
- `getTrainColor(trainNumber)`: Returns the pre-assigned hex color

### Key Backend Components

#### `TimetableService`

Singleton service with two in-memory caches (schedule and timetable file content):

- `GetTimetableForDateAsync(date, debugMode)`: Main entry point; selects and returns the correct JSON
- `LoadScheduleAsync(year)`: Reads and caches `schedule.json` for a given year
- Returns `FileNotFoundException` (→ HTTP 404) when no schedule entry exists for the date

#### `TimetableFunction`

Azure Function triggered by `GET /api/timetable`. Delegates to `TimetableService` and handles `FileNotFoundException` → 404.

#### `HealthCheckFunction`

Azure Function triggered by `GET /api/health`. Calls `GetAvailableTimetablesAsync()` to verify file system access and returns structured health JSON.

## Algorithm Details

### Service Selection Algorithm

When the same physical train (`trainNumber`) has multiple entries in the timetable (e.g., an outbound and return trip), one entry is displayed at a time. Priority order:

1. **Currently running** service over a finished one
2. **Moving** train over one stationary at a station (when both are running)
3. **Most recently finished** service (for terminus waiting display)

### Position Calculation

**Between stations:**

```
progress = (currentTime - departTime) / (arriveTime - departTime)

// Northbound: displayed bottom-to-top (lower % = higher on screen)
displayPercentage = (1 - progress) * 100

// Southbound: displayed top-to-bottom
displayPercentage = progress * 100
```

**At a station:** Train is at a station when:

- Current time is between the stop's `arrival` and `departure`, OR
- Current time is past the final `arrival` (at terminus)

**Pre-departure:** A train at a terminal station is shown up to 15 minutes before its first departure.

**Post-arrival:** A train is kept visible at its terminus for up to 15 minutes after arrival if it has no further services.

### Terminus Station Logic

```
1. Check if service has ended (currentTime > endTime)
2. Look for a future service from that station for this train number
3. IF next service exists:
     - Show "Waiting at [station], next departure at [time]..."
4. ELSE:
     - Show "Terminated at [station]"
     - Remove from display after 15 minutes
```

### Color Assignment

Colors are assigned once when the timetable loads:

1. Extract all unique train numbers from the timetable
2. Sort alphabetically
3. Assign colors from `colorPalette` in order
4. Store in `trainColors` object for the session

This guarantees each train always has the same color regardless of which services are currently active.

## Customization

### Changing the Color Palette

Update the `colorPalette` array in `system.js`:

```javascript
const colorPalette = ['#ff6b6b', '#4ecdc4', '#45b7d1', '#96ceb4', '#ffeaa7', '#fd79a8', '#fdcb6e', '#6c5ce7', '#a29bfe', '#74b9ff'];
```

### Adjusting Auto-Refresh Rate

Change the interval in `startAutoRefresh()`:

```javascript
refreshInterval = setInterval(() => {
    updateTimeDisplay();
    renderTracker();
}, 30000); // milliseconds
```

### Adding a New Timetable

1. Create a new JSON file in `src/API/SevernValleyTimetable/Timetables/<year>/`
2. Add entries to the corresponding `schedule.json` mapping dates to the new file name (without `.json` extension)
3. Redeploy the Azure Function app

### Custom Styling

CSS custom properties are defined for both themes in `styles.css`. Override the relevant variables under `:root` (dark) and `.light-mode` selectors.

## Browser Compatibility

### Supported Browsers

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+
- Mobile Safari (iOS 14+)
- Chrome Mobile (Android 5+)

### Required Features

- ES6 JavaScript (arrow functions, template literals, spread operator, `async`/`await`)
- CSS Flexbox and CSS custom properties
- `fetch` API
- `localStorage`
- `setInterval` / `clearInterval`

## Troubleshooting

**Problem: "No Timetable Available" message**

- The SVR may not be running services today
- Check `schedule.json` for the current year to confirm whether today's date is listed
- Use `?debug=true` to load the debug timetable regardless of date

**Problem: Trains not appearing**

- Check that current time falls within timetable hours
- Verify train numbers in timetable are spelled consistently
- Ensure station names match exactly (case-sensitive)

**Problem: Train in wrong position**

- Verify time fields are in HH:MM 24-hour format
- Check that times are in chronological order within each service

**Problem: System status shows "unavailable"**

- Check the health endpoint: `GET /api/health`
- Verify the Azure Function app is running in the Azure portal

**Problem: Theme not persisting**

- Ensure `localStorage` is not blocked by browser settings or extensions

**Problem: Auto-refresh not stopping in manual mode**

- Only appears in debug mode (`?debug=true`)
- `clearInterval` is called on every `startAutoRefresh()` call; manual mode simply does not restart the interval
