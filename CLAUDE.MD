# Severn Valley Railway Live Train Tracker

## Project Overview

A real-time web application that visualises train movements on the Severn Valley Railway heritage line between Kidderminster and Bridgnorth. The system consists of a .NET 8 Azure Functions API backend serving timetable data, and a pure JavaScript frontend providing live train tracking with no dependencies.

**Live URL**: https://victorious-mushroom-07851c603.1.azurestaticapps.net  
**API URL**: https://svrliveapi-aaeydueba4b9aveb.uksouth-01.azurewebsites.net

## Architecture

### Backend (Azure Functions API)
- **Framework**: .NET 8 (Isolated Worker Model)
- **Hosting**: Azure Functions
- **Location**: `src/API/SevernValleyTimetable/`
- **Primary Functions**:
  - `HealthCheckFunction.cs` - System health endpoint at `/api/health`
  - `TimetableFunction.cs` - Timetable data endpoint at `/api/timetable`
  - `TimetableService.cs` - Core business logic for timetable loading and caching
  - `TimetableMcpServer.cs` - MCP (Model Context Protocol) server (currently commented out)

### Frontend (Static Web App)
- **Technology**: Pure vanilla JavaScript, HTML5, CSS3
- **Hosting**: Azure Static Web Apps
- **Location**: `src/Web/`
- **Files**:
  - `index.html` - Main application structure
  - `styles.css` - Responsive styling with desktop two-column layout
  - `system.js` - All application logic (no frameworks or dependencies)

### Data Storage
- **Timetables**: JSON files in `src/API/SevernValleyTimetable/Timetables/`
- **Structure**:
  - `{year}/schedule.json` - Maps dates to timetable files
  - `{year}/{timetable-name}.json` - Actual timetable data
  - `debug.json` - Debug/test timetable
  - `config.json` - Station list configuration

## Key Concepts

### Timetable Data Model

```csharp
public class TimetableData
{
    public string? Name { get; set; }          // e.g., "Timetable B"
    public string? Date { get; set; }          // e.g., "Saturday 26 December"
    public List<TrainService>? Trains { get; set; }
}

public class TrainService
{
    public string? TrainNumber { get; set; }   // e.g., "Steam 75069", "Diesel 46045"
    public string? Direction { get; set; }     // "northbound" or "southbound"
    public List<TrainStop>? Stops { get; set; }
}

public class TrainStop
{
    public string? Station { get; set; }
    public string? Departure { get; set; }     // HH:MM format
    public string? Arrival { get; set; }       // HH:MM format
    public string? Time { get; set; }          // For non-stop pass-through
    public bool StopsAt { get; set; }          // true if train stops, false if passing
}
```

### Direction Naming Convention

**CRITICAL**: Direction names do NOT match geographic directions:
- **Northbound**: Bridgnorth → Kidderminster (geographically westward)
- **Southbound**: Kidderminster → Bridgnorth (geographically eastward)

This is railway convention, not geography. The frontend visualisation shows:
- Kidderminster at the top
- Bridgnorth at the bottom
- Northbound trains move upward (left side of track)
- Southbound trains move downward (right side of track)

### Station Order

From north to south (top to bottom in UI):
1. Kidderminster
2. Bewdley
3. Arley
4. Highley
5. Hampton Loade
6. Bridgnorth

### Time Handling

- All times stored as HH:MM strings in 24-hour format
- Frontend converts to "minutes since midnight" for calculations
- Live mode: Uses current system time
- Debug mode: Manual time control with 5-minute increments (enabled via `?debug=true`)

### Train Service Selection Algorithm

When a train number appears in multiple services throughout the day, the frontend displays only ONE instance using this priority:

1. **Currently Running Services** (current time between start and end)
2. **Moving Trains** (position.type === 'between')
3. **Recently Completed Services** (waiting at terminus for next departure)
4. **Most Recent Service** (by end time)

This ensures each physical train appears exactly once in the visualisation.

## Frontend Implementation Details

### Position Calculation

The `findTrainPosition(train, currentTime)` function returns one of three position types:

1. **at_station**: Train is stopped at a station
   - Between arrival and departure times
   - At terminus before/after service
   - 15 minutes before first departure at terminus
   
2. **between**: Train is traveling between stations
   - Includes progress percentage (0-1)
   - departTime and arriveTime for status updates
   
3. **null**: Train not currently visible
   - Before service starts (and not at pre-departure window)
   - More than 15 minutes after service ends with no next service

### Color Assignment

- Colors assigned alphabetically by train number at initialisation
- Consistent throughout session
- Uses predefined color palette: `['#ff6b6b', '#4ecdc4', '#45b7d1', ...]`
- Same train always gets same color

### Auto-Refresh Behaviour

- **Live Mode**: Updates every 30 seconds
- **Manual Mode**: No auto-refresh, user controls time
- Switching modes clears/restarts refresh interval

## API Endpoints

### GET /api/health
Returns system health status and timetable availability count.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2026-01-26T10:30:00Z",
  "service": "SevernValleyTimetable",
  "version": "1.0.0",
  "timetablesAvailable": 13,
  "checks": {
    "timetableService": "ok",
    "fileSystem": "ok"
  }
}
```

### GET /api/timetable?debug={true|false}
Returns timetable for current date, or debug timetable if debug=true.

**Success Response** (200):
```json
{
  "name": "Timetable B",
  "date": "Saturday 26 December",
  "trains": [...]
}
```

**No Timetable Response** (404):
```json
{
  "error": "No timetable found for the current date"
}
```

### Schedule Loading Logic

1. API receives date request
2. Loads `{year}/schedule.json`
3. Finds entry matching `dd-MMM` format (e.g., "26-Dec")
4. If found, loads corresponding timetable file
5. If not found OR schedule doesn't exist, returns 404
6. Debug mode bypasses schedule, always returns `debug.json`

## CI/CD Pipeline

### API Deployment
**Workflow**: `.github/workflows/main_svrliveapi.yml`
- Triggers: Push/PR to main or develop branches
- Builds .NET 8 solution
- Publishes to Azure Function App `svrliveapi`
- Uses OIDC authentication with Azure

### Web Deployment
**Workflow**: `.github/workflows/azure-static-web-apps-victorious-mushroom-07851c603.yml`
- Triggers: Push/PR to main branch
- Deploys static content from `src/Web/` 
- Uses Azure Static Web Apps deployment token

### Build Verification
**Workflow**: `.github/workflows/api-build.yml`
- Verifies solution builds successfully
- Runs tests (currently with continue-on-error)
- Validates Timetables folder structure
- Creates build artifacts

## Development Setup

### Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local API testing)
- Modern web browser
- Optional: Visual Studio 2022 or VS Code with C# extension

### Running Locally

**API**:
```bash
cd src/API/SevernValleyTimetable
dotnet restore
dotnet build
func start
```
API runs at `http://localhost:7071`

**Web**:
```bash
cd src/Web
# Use any static server, e.g.:
python -m http.server 8000
# Or:
npx http-server
```
Web runs at `http://localhost:8000`

**Important**: Update API URL in `system.js` for local development:
```javascript
// Change from:
const apiUrl = 'https://svrliveapi-aaeydueba4b9aveb.uksouth-01.azurewebsites.net/api/timetable';
// To:
const apiUrl = 'http://localhost:7071/api/timetable';
```

## File Structure

```
├── .github/workflows/           # CI/CD pipelines
├── docs/
│   ├── MCP-SERVER-README.md    # MCP server documentation
│   └── TimetablePrompt.md      # Timetable conversion guide
├── src/
│   ├── API/
│   │   └── SevernValleyTimetable/
│   │       ├── Functions/
│   │       │   ├── HealthCheckFunction.cs
│   │       │   ├── TimetableFunction.cs
│   │       │   ├── TimetableMcpServer.cs
│   │       │   ├── TimetableService.cs
│   │       │   └── Program.cs
│   │       ├── Timetables/
│   │       │   ├── 2025/
│   │       │   │   ├── schedule.json
│   │       │   │   ├── timetable-b.json
│   │       │   │   ├── timetable-c.json
│   │       │   │   └── ...
│   │       │   ├── 2026/
│   │       │   │   ├── schedule.json
│   │       │   │   └── ...
│   │       │   ├── debug.json
│   │       │   └── config.json
│   │       ├── SevernValleyTimetable.csproj
│   │       └── host.json
│   └── Web/
│       ├── index.html
│       ├── styles.css
│       └── system.js
└── README.md
```

## Common Tasks

### Adding a New Timetable

1. Create JSON file following the data model
2. Place in `src/API/SevernValleyTimetable/Timetables/{year}/`
3. Update `{year}/schedule.json` with date mapping:
   ```json
   [
     {
       "date": "26-Dec",
       "timetable": "timetable-b"
     }
   ]
   ```
4. Ensure `.csproj` includes:
   ```xml
   <None Update="Timetables\**\*.json">
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </None>
   ```

### Converting Timetable Images

Use the prompt in `docs/TimetablePrompt.md` with Claude to convert timetable images to JSON format. Key rules:

- **Stopping stations**: Show time → include arrival (time - 3min) and departure
- **Passing stations**: Show "-" → include time field, stopsAt: false
- **Blank stations**: Don't include in stops array
- **Origin station**: departure only, no arrival
- **Destination station**: arrival only, no departure
- **Times with 'x'**: Ignore the 'x', treat as regular times

### Testing Locally

1. Run API locally (see above)
2. Add `?debug=true` to web URL to enable manual time controls
3. Use time arrows to test different times of day
4. Check browser console for errors
5. Verify train positions match expected timetable

### Debugging Position Issues

If trains appear in wrong positions:
1. Check direction (northbound vs southbound)
2. Verify station order matches `stations` array
3. Confirm times are in chronological order in JSON
4. Check for missing arrival/departure fields
5. Use browser DevTools to inspect `findTrainPosition()` output

## Known Limitations

- No localStorage/sessionStorage (Claude.ai artifact compatibility)
- State resets on page refresh
- Maximum 20 results from recent_chats tool (not used in this project)
- Timetables must be manually created/updated
- No real-time data from actual railway systems
- No offline support

## Future Enhancements

Documented in README.md, including:
- Multiple route support
- Delay/disruption indicators
- Historical playback mode
- Export/import timetables
- Platform number display
- Sound notifications
- Accessibility improvements (ARIA labels)
- Keyboard navigation

## MCP Server (Currently Disabled)

The project includes MCP server code in `TimetableMcpServer.cs` (commented out). When enabled, it provides:
- `get_timetable` tool
- `get_available_dates` tool
- `search_trains` tool
- Resources for current and debug timetables

See `docs/MCP-SERVER-README.md` for full documentation.

## Testing Strategy

Current testing approach:
1. Manual testing with debug mode
2. Visual verification of train positions
3. Cross-browser testing (Chrome, Firefox, Safari, Edge)
4. Mobile device testing (iOS Safari, Chrome Mobile)
5. API health checks via `/api/health`

No automated test suite currently exists.

## Deployment Notes

- API deployment requires Azure Function App configured
- Static Web App requires deployment token as GitHub secret
- Both require appropriate Azure permissions
- CORS is configured as `*` for open access
- No authentication/authorisation required

## Code Style Guidelines

**C# Backend**:
- Follow Microsoft C# coding conventions
- Use nullable reference types
- Async/await for all I/O operations
- Dependency injection via HostBuilder
- Descriptive variable names

**JavaScript Frontend**:
- Vanilla JavaScript (no frameworks)
- 4-space indentation
- camelCase for variables and functions
- Clear, descriptive function names
- Comments for complex logic
- No external dependencies

**JSON Data**:
- 2-space indentation
- Consistent field naming (camelCase)
- Times always as strings ("HH:MM")
- Booleans for flags (stopsAt)

## Performance Considerations

**Backend**:
- Timetable data cached in memory (singleton service)
- Schedule files cached separately by year
- No database queries required
- Fast cold start (Isolated Worker Model)

**Frontend**:
- Single HTML file loads immediately
- CSS and JS are inline (no extra requests)
- API called once on load, then uses cached data
- Auto-refresh interval: 30 seconds (configurable)
- Efficient DOM updates (full re-render acceptable for small dataset)

## Contact & Support

For issues, questions, or contributions:
- Open GitHub issue
- Include browser/device information
- Provide example timetable data if relevant
- Describe expected vs actual behaviour

## License

MIT License - See LICENSE file for details

---

**For AI Assistants**: This project prioritises simplicity, clarity, and zero dependencies. When suggesting changes:
1. Maintain vanilla JavaScript approach (no frameworks)
2. Keep API stateless and cacheable
3. Preserve existing timetable data structure
4. Consider mobile responsiveness
5. Test across time ranges (early morning, late evening, midnight crossover)
6. Respect the northbound/southbound naming convention
