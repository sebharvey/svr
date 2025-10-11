Here’s the complete updated prompt to recreate this app with all fixes and updates:

-----

**Project: Severn Valley Live Train Tracker**

Create a single HTML page with native JavaScript that displays a live train tracker for the Severn Valley Railway line between Kidderminster and Bridgnorth.

-----

## Data Source

- Use the provided JSON timetable as the sole source of data
- Do NOT modify the JSON timetable
- Do NOT hard-code ANY train numbers, times, or station names
- All information must be derived dynamically from the JSON object
- Extract all unique train numbers from the timetable, sort them alphabetically, and assign colors based on that sorted order for consistent color assignment throughout the app’s lifetime

-----

## Visual Layout

### Station & Track Display

Create a vertical stack of DIV elements representing the railway line:

1. **Stations** (grey divs):

- Station name centered
- Kidderminster at the top
- Bridgnorth at the bottom
- NO icons for stations

1. **Track sections** (darker grey divs, taller than stations):

- Between each station pair
- Vertical line down the center representing the railway track

1. **Train positioning**:

- **Northbound trains**: Left side of divs, moving from bottom to top
- **Southbound trains**: Right side of divs, moving from top to bottom
- Each train shows: icon + train number + direction arrow
- Train number placement: left of icon (northbound), right of icon (southbound)
- Use appropriate icons dynamically based on train number: steam locomotive (🚂 for “Steam”), DMU (🚃 for “DMU”), diesel locomotive (🚆 for “Diesel”)
- Icons ONLY for trains (nowhere else)

-----

## Train Movement Logic

### Position Calculation

- **At station**: Show train within station div
- **Between stations**: Show train in track section div at calculated percentage based on:
  - Departure time from origin station
  - Arrival time at destination station
  - Current time
  - Direction: Northbound trains move from bottom (100%) to top (0%) of div, southbound from top (0%) to bottom (100%)

### Train Service Rules

1. **Each train number appears only once**:

- One position on tracker
- One entry in status panel
- When a train has multiple services throughout the day, intelligently select which service to display:
  - **Priority 1**: Services currently running (between start and end time)
  - **Priority 2**: If multiple services are running, prioritize trains moving (between stations) over trains at stations
  - **Priority 3**: For finished services, show the most recently completed service (highest end time that has passed)
  - **Never show**: Future services that haven’t started yet UNLESS the train has no completed or running services

1. **Terminus station display logic**:

- After a train completes a service at a terminus (Kidderminster or Bridgnorth), check if it has ANY future service departing from that station
- **If train has a future service from that station**:
  - Keep the train visible at the terminus
  - After the current service end time has passed, switch the train’s display to show the next departure direction (e.g., if arrived southbound, show on northbound side for next northbound departure)
  - This prevents overlapping trains at terminus stations
  - Status shows “Waiting at [station], next departure at [time] (departing in X minutes)”
- **If NO further services from that station**:
  - Show “Terminated at [station]”
  - Remove train from display 15 minutes after arrival
- Look for next service by train number only, not by direction (trains change direction at terminus)

1. **Non-stop services**:

- Status shows “passing [station] in X minutes” for stations where train doesn’t stop

1. **Arrival time display**:

- If arriving in 0 minutes, show “arriving now” instead of “arriving in 0 minutes”

-----

## Status Panel

Display at bottom of page with format:

```
[Train Number] ([direction])
[Current Status Line]
```

**Examples:**

```
Diesel 37248 (northbound)
Traveling from Bewdley (departed 15:35) → Kidderminster (arriving now)

Diesel DMU (southbound)
Waiting at Kidderminster, next departure at 15:50 (departing in 13 minutes)

Steam 75069 (northbound)
Terminated at Bridgnorth
```

**Status Rules:**

- Show only active trains or trains with upcoming services
- One status entry per train number
- DO NOT duplicate train type in the header (train number already contains type info like “Diesel 37248” - just show train number)
- If terminated with no further service: Show “Terminated at [station]” for 15 minutes after arrival
- If waiting at terminus with next service: Show next departure time and countdown (check all services from that station, not just same direction)
- Color-code each train to match its icon on the tracker with a colored left border
- Colors assigned alphabetically by train number and remain consistent throughout the app

-----

## Time Controls

### Interface:

- **Time display**: Shows current time in HH:MM format (updates in real-time when in live mode)
- **Arrow buttons**:
  - Left arrow (◀) decreases time by 5 minutes
  - Right arrow (▶) increases time by 5 minutes
  - Clicking either arrow switches to manual mode
  - In manual mode, arrows continue to increment/decrement the manual time value (do NOT reset to current time on each click)
- **Live Time button**:
  - Disabled (greyed out) when in live mode (default state)
  - Enabled when in manual mode
  - Clicking returns to live mode with auto-refresh

### Modes:

- **Live time** (default on page load):
  - Uses actual current time
  - Time display updates to show current time every 30 seconds
  - Auto-refresh tracker and status every 30 seconds
  - Live Time button is disabled
- **Manual time**:
  - Triggered by clicking arrow buttons
  - Time can be incremented/decremented in 5-minute steps
  - NO auto-refresh
  - Live Time button is enabled
  - Manual time initializes to current time when FIRST entering manual mode (first arrow click)
  - Subsequent arrow clicks continue incrementing/decrementing the manual time

-----

## Technical Requirements

1. **Code Quality**:

- Keep code simple and clean
- Minimize duplication
- Reuse logic between tracker rendering and status panel
- Use functions for common operations
- NO hardcoded train numbers, station names, or times anywhere

1. **Responsive Design**:

- Mobile-friendly (primary target: iPhone browser)
- Use [Claude.ai](http://Claude.ai) dark mode color scheme (dark grey backgrounds, light text)

1. **Title**:

- “Severn Valley Live Train Tracker” (H1, no icon)

1. **Color Coding**:

- Extract unique train numbers from timetable dynamically
- Sort alphabetically
- Assign colors from palette based on sorted order
- Each train number has a consistent color throughout the app’s lifetime
- Same color used on tracker and in status panel (as left border) for easy matching
- Color persists even when train disappears and reappears later

1. **Browser Compatibility**:

- NEVER use localStorage or sessionStorage (not supported in artifacts)
- Use JavaScript variables for all state management

-----

## Key Constraints

- Icons ONLY for trains (nowhere else)
- All data dynamically generated from JSON timetable
- No hard-coded values (train numbers, times, stations)
- Single-page application
- No external dependencies beyond the JSON timetable
- Each train appears exactly once regardless of how many services it runs

-----

## Critical Implementation Details

### Service Selection Algorithm

When a train number has multiple services in the timetable, select the correct one to display:

1. Check if each service is currently running: `currentTime >= startTime && currentTime <= endTime`
1. If one service is running and another isn’t, show the running service
1. If both are running, prioritize the one that’s moving (between stations) over one at a station
1. If neither is running:

- Check if services have finished: `currentTime > endTime`
- If one has finished and one hasn’t started, show the finished one (it’s waiting at the terminus)
- If both have finished, show the most recently finished (highest endTime)
- Never show a service that hasn’t started yet unless there’s no other option

### Terminus Station Display Logic

For trains at Kidderminster or Bridgnorth:

1. After a service ends at a terminus, check for the next service from that station
1. If a next service exists and `currentTime > currentServiceEndTime`:

- Switch the train’s display to use the next service object (changes direction arrow and side)
- Keep position at the same station
- This prevents overlapping when a southbound arrival has a northbound departure

1. Only switch display AFTER current service has ended, not before

### Position Calculation for Moving Trains

- Northbound trains in track section: `percentage = (1 - progress) * 100` (start at 100%, end at 0%)
- Southbound trains in track section: `percentage = progress * 100` (start at 0%, end at 100%)
- Progress is calculated as: `(currentTime - departTime) / (arriveTime - departTime)`

### Track Section Matching

The page displays stations from top (Kidderminster) to bottom (Bridgnorth):

- For **northbound trains**: Match when `fromStation === nextStation && toStation === currentStation` (train moves upward)
- For **southbound trains**: Match when `fromStation === currentStation && toStation === nextStation` (train moves downward)

### Next Service Detection

When looking for the next service:

```javascript
function getNextService(trainNumber, currentStation, currentTime) {
    // Find ANY service (any direction) with same train number
    // That departs from currentStation
    // With start time > currentTime
    // Return the earliest matching service
}
```

-----

## Common Bugs to Avoid

1. **Wrong train position**: Ensure northbound trains start at bottom (100%) and move to top (0%), not reversed
1. **Duplicate trains**: Only show one instance per train number; use service selection algorithm correctly
1. **Wrong terminus display**: Don’t switch to next service direction until current service has actually ended
1. **Color inconsistency**: Assign colors once at initialization based on alphabetically sorted train numbers
1. **Manual time reset**: Only set manualTime to current time on FIRST arrow click, then keep incrementing the same value
1. **Service selection**: Don’t show future services that haven’t started if there’s a completed service to show
1. **Train type duplication**: Don’t add “Diesel” or “Steam” prefix when train number already contains it

-----
