# Severn Valley Live Train Tracker

A real-time visualization of train movements on the Severn Valley Railway line between Kidderminster and Bridgnorth. This single-page web application provides an interactive, live-updating display of train positions and status information based on a JSON timetable.

https://victorious-mushroom-07851c603.1.azurestaticapps.net
 
## 🚂 Features

### Core Functionality

- **Live Train Tracking**: Real-time visualization of train positions on the railway line
- **Dual Time Modes**:
  - Live mode with automatic 30-second updates
  - Manual mode with 5-minute increment controls for testing/debugging
- **Intelligent Train Display**: Each train appears only once, showing its current or next service
- **Status Panel**: Detailed information about each active train’s current status
- **Terminus Management**: Smart handling of trains waiting at terminal stations
- **Color-Coded Trains**: Consistent color assignment per train throughout the day

### Visual Design

- **Vertical Track Layout**: Stations displayed from Kidderminster (top) to Bridgnorth (bottom)
- **Directional Positioning**:
  - Northbound trains on the left (moving upward)
  - Southbound trains on the right (moving downward)
- **Dynamic Icons**: Steam (🚂), DMU (🚃), and Diesel (🚆) locomotives
- **Dark Mode Interface**: Optimized for Claude.ai’s dark theme
- **Mobile-Responsive**: Optimized for iPhone and mobile browsers

## 📋 Table of Contents

- [Installation](#installation)
- [Usage](#usage)
- [Data Format](#data-format)
- [Architecture](#architecture)
- [Algorithm Details](#algorithm-details)
- [Customization](#customization)
- [Browser Compatibility](#browser-compatibility)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## 🚀 Installation

### Basic Setup

1. Clone the repository:

```bash
git clone https://github.com/yourusername/severn-valley-train-tracker.git
cd severn-valley-train-tracker
```

1. Open `index.html` in a web browser:

```bash
# On macOS
open index.html

# On Linux
xdg-open index.html

# On Windows
start index.html
```

No build process, dependencies, or server required - it’s a single self-contained HTML file!

### For Development

If you want to serve it locally for development:

```bash
# Using Python 3
python -m http.server 8000

# Using Node.js http-server
npx http-server

# Then navigate to http://localhost:8000
```

## 💡 Usage

### Time Controls

**Live Time Mode (Default)**

- Time display shows current real time
- Tracker updates automatically every 30 seconds
- “Live Time” button is disabled (grayed out)

**Manual Time Mode**

- Click the ◀ or ▶ arrows to enter manual mode
- Time adjusts in 5-minute increments
- NO automatic updates
- Click “Live Time” button to return to live mode

### Reading the Display

**Station Display**

- Gray boxes represent stations
- Kidderminster at the top, Bridgnorth at the bottom
- Trains shown inside station boxes when stopped

**Track Sections**

- Darker gray areas between stations
- Central vertical line represents the railway
- Trains positioned dynamically based on progress between stations

**Train Indicators**

- Icon (🚂/🚃/🚆) + Train Number + Direction Arrow (↑/↓)
- Left side = Northbound, Right side = Southbound
- Color-coded border and background (consistent per train)

**Status Panel**

- Shows all active trains
- Displays current activity (traveling, waiting, terminated)
- Countdown timers for arrivals and departures
- Non-stop passing information

## 📊 Data Format

### JSON Timetable Structure

The application expects a JSON object with the following structure:

```json
{
  "route": "Kidderminster - Bridgnorth",
  "date": "Saturday 11 October only",
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

- `route` (string): Route description (informational only)
- `date` (string): Operating date (informational only)
- `trains` (array): Array of train service objects

**Train Object**

- `trainNumber` (string): Unique identifier for the physical train (e.g., “Diesel 37248”, “Steam 75069”)
- `direction` (string): Either “northbound” or “southbound”
- `stops` (array): Ordered list of station stops

**Stop Object**

- `station` (string): Station name (must match station list in code)
- `stopsAt` (boolean): Whether train stops at this station
- `departure` (string, optional): Departure time in HH:MM format
- `arrival` (string, optional): Arrival time in HH:MM format
- `time` (string, optional): Pass-through time for non-stop stations

**Time Field Rules**

- First stop: Must have `departure`
- Last stop: Must have `arrival`
- Intermediate stops where train stops: Must have both `arrival` and `departure`
- Non-stop stations: Use `time` field with `stopsAt: false`

### Station List

The following stations must be included in order (Kidderminster to Bridgnorth):

1. Kidderminster
1. Bewdley
1. Arley
1. Highley
1. Hampton Loade
1. Bridgnorth

## 🏗 Architecture

### File Structure

```
severn-valley-train-tracker/
├── index.html              # Single-file application
├── README.md              # This file
└── LICENSE                # MIT License
```

### Technology Stack

- **HTML5**: Structure
- **CSS3**: Styling with Flexbox
- **Vanilla JavaScript**: All logic (no frameworks)
- **No Dependencies**: Completely self-contained

### Key Components

#### 1. Data Layer

- `timetableData`: Embedded JSON object containing all train services
- `trainColors`: Color mapping generated from sorted train numbers
- `stations`: Ordered array of station names

#### 2. State Management

- `isLiveMode`: Boolean flag for time mode
- `manualTime`: Current time in manual mode (minutes since midnight)
- `refreshInterval`: Auto-refresh timer reference

#### 3. Core Functions

**Time Functions**

- `getCurrentTime()`: Returns current time based on mode
- `parseTime(timeStr)`: Converts HH:MM to minutes
- `formatTime(minutes)`: Converts minutes to HH:MM
- `updateTimeDisplay()`: Updates time display
- `getStopTime(stop)`: Extracts time from stop object

**Position Calculation**

- `findTrainPosition(train, currentTime)`: Determines if/where train is active
- `getNextService(trainNumber, currentStation, currentTime)`: Finds next departure

**Rendering**

- `renderTracker()`: Main render function for track display
- `createTrainElement(train, topPercentage)`: Creates train DOM element
- `renderStatus(activeTrains, currentTime)`: Renders status panel
- `generateStatusText(train, position, currentTime)`: Creates status message

**Utility Functions**

- `getTrainIcon(trainNumber)`: Returns emoji based on train type
- `getTrainColor(trainNumber)`: Returns assigned color

## 🧮 Algorithm Details

### Service Selection Algorithm

When a train number has multiple services throughout the day, the algorithm selects which one to display:

```
FOR each train service in timetable:
  position = findTrainPosition(service, currentTime)
  
  IF position exists:
    IF trainNumber not in activeTrains:
      ADD to activeTrains
    ELSE:
      existing = activeTrains[trainNumber]
      
      currentIsRunning = currentTime between start and end
      existingIsRunning = currentTime between start and end
      
      IF currentIsRunning AND NOT existingIsRunning:
        REPLACE existing with current
      ELSE IF existingIsRunning:
        KEEP existing
      ELSE IF both running:
        IF current is moving AND existing is at station:
          REPLACE with current
      ELSE (neither running):
        currentFinished = currentTime > endTime
        existingFinished = currentTime > endTime
        
        IF currentFinished AND NOT existingFinished:
          REPLACE with current (waiting train)
        ELSE IF both finished:
          KEEP most recently finished
```

**Priority Order:**

1. Currently running services
1. Moving trains over stationary trains
1. Most recently completed services (for waiting at terminus)

### Position Calculation

**Between Stations:**

```javascript
progress = (currentTime - departTime) / (arriveTime - departTime)

// Northbound: bottom to top
displayPercentage = (1 - progress) * 100

// Southbound: top to bottom  
displayPercentage = progress * 100
```

**At Stations:**

- Train is at a station if:
  - Current time is between arrival and departure times, OR
  - Current time is after final arrival (at terminus)

**Track Segment Matching:**

- Stations displayed top (Kidderminster) to bottom (Bridgnorth)
- Northbound: Match when `fromStation === nextStation && toStation === currentStation`
- Southbound: Match when `fromStation === currentStation && toStation === nextStation`

### Terminus Station Logic

When a train arrives at Kidderminster or Bridgnorth:

```
1. Check if service has ended (currentTime > endTime)
2. Look for next service from that station (any direction)
3. IF next service exists:
     - Keep train visible at station
     - After endTime, switch display to next service direction
     - Show "Waiting at [station], next departure..."
4. ELSE (no next service):
     - Show "Terminated at [station]"
     - Remove from display after 15 minutes
```

### Color Assignment

Colors are assigned at initialization:

```
1. Extract all unique train numbers from timetable
2. Sort alphabetically
3. Assign colors from palette in order
4. Store in trainColors object
5. Use consistently throughout app lifetime
```

This ensures:

- Same train always has same color
- Colors don’t change when trains appear/disappear
- Predictable color assignment across different timetables

## 🎨 Customization

### Changing Station Names

Update the `stations` array and ensure all timetable entries use matching names:

```javascript
const stations = ['Your Station 1', 'Your Station 2', ...];
```

### Adding More Trains

Simply add more train objects to the `timetableData.trains` array. The app dynamically handles any number of trains.

### Modifying Colors

Update the `colorPalette` array with your preferred hex colors:

```javascript
const colorPalette = ['#ff6b6b', '#4ecdc4', '#45b7d1', ...];
```

Colors are assigned in order based on alphabetically sorted train numbers.

### Adjusting Auto-Refresh Rate

Change the interval in the `startAutoRefresh()` function:

```javascript
refreshInterval = setInterval(() => {
    updateTimeDisplay();
    renderTracker();
}, 30000); // Change 30000 to desired milliseconds
```

### Custom Styling

All styles are in the `<style>` tag. Key CSS variables to modify:

```css
/* Background colors */
body { background-color: #1a1a1a; }
.station { background-color: #404040; }
.track-section { background-color: #333333; }

/* Text colors */
body { color: #e0e0e0; }
.station-name { color: #ffffff; }
```

## 🌐 Browser Compatibility

### Supported Browsers

- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Edge 90+
- ✅ Mobile Safari (iOS 14+)
- ✅ Chrome Mobile (Android 5+)

### Required Features

- ES6 JavaScript (arrow functions, template literals, spread operator)
- CSS Flexbox
- CSS Transform
- Array methods (map, filter, reduce, sort)
- Set data structure
- setInterval/clearInterval

### Known Limitations

**NOT Supported:**

- localStorage/sessionStorage (deliberately excluded for Claude.ai compatibility)
- Service Workers
- IndexedDB
- Web Workers

**State is NOT persisted** - refreshing the page resets to live time mode.

## 🐛 Troubleshooting

### Common Issues

**Problem: Trains not appearing**

- Check that current time falls within timetable hours
- Verify train numbers in timetable are spelled consistently
- Ensure station names match exactly (case-sensitive)

**Problem: Train in wrong position**

- Verify time fields (arrival/departure/time) are in HH:MM format
- Check that times are in chronological order
- Ensure 24-hour time format (not 12-hour)

**Problem: Colors changing**

- This was a bug in earlier versions - ensure you have the latest code
- Colors should be assigned once at initialization based on sorted train numbers

**Problem: Duplicate trains showing**

- This should not happen in current version
- Each train number should appear exactly once
- Check that service selection algorithm is working correctly

**Problem: Manual time not incrementing**

- Ensure you’re not resetting manualTime on every arrow click
- Manual time should only initialize to current time on FIRST arrow click

**Problem: Auto-refresh not stopping in manual mode**

- Check that `startAutoRefresh()` is called after mode changes
- Verify `clearInterval()` is being called when switching to manual mode

### Debug Mode

To enable console logging for debugging, add this at the start of `renderTracker()`:

```javascript
console.log('Current Time:', formatTime(currentTime));
console.log('Active Trains:', activeTrains);
```

## 🤝 Contributing

Contributions are welcome! Here’s how to contribute:

### For Bug Fixes

1. Fork the repository
1. Create a feature branch: `git checkout -b fix-issue-name`
1. Make your changes
1. Test thoroughly at different times of day
1. Commit: `git commit -m "Fix: description of fix"`
1. Push: `git push origin fix-issue-name`
1. Create a Pull Request

### For New Features

1. Open an issue first to discuss the feature
1. Follow the same fork/branch/PR process
1. Include documentation updates
1. Add examples if applicable

### Code Style Guidelines

- Use 4-space indentation
- Use camelCase for variables and functions
- Add comments for complex logic
- Keep functions focused and small
- Avoid hardcoding values
- Test with different timetables

### Testing Checklist

Before submitting a PR, test:

- [ ] Live time mode with auto-refresh
- [ ] Manual time mode with arrow controls
- [ ] Switching between modes
- [ ] Train positions at different times
- [ ] Terminus station behavior
- [ ] Multiple trains at same location
- [ ] Non-stop services
- [ ] Mobile display
- [ ] Different timetable data

## 📄 License

MIT License

Copyright (c) 2024

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## 🙏 Acknowledgments

- Inspired by the Severn Valley Railway heritage line
- Built for Claude.ai artifact environment
- Designed for both human and AI agent comprehension

## 📞 Support

For issues, questions, or suggestions:

- Open an issue on GitHub
- Include browser/device information
- Provide example timetable data if relevant
- Describe expected vs actual behavior

## 🔮 Future Enhancements

Potential features for future versions:

- [ ] Multiple route support
- [ ] Delay/disruption indicators
- [ ] Historical playback mode
- [ ] Export/import timetables
- [ ] Platform number display
- [ ] Weather integration
- [ ] Sound notifications
- [ ] Accessibility improvements (ARIA labels)
- [ ] Keyboard navigation
- [ ] Print stylesheet

-----

**Note for AI Agents**: This application demonstrates several important patterns:

- Single-responsibility functions
- State management without frameworks
- Dynamic data-driven rendering
- Time-based calculations
- Conflict resolution (multiple services per train)
- Mobile-first responsive design
- No external dependencies

The codebase is intentionally simple and self-contained to maximize portability and minimize dependencies.
