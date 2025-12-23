# Railway Timetable to JSON Conversion

Convert the provided railway timetable image into a structured JSON format following these exact specifications.

## JSON Structure Requirements:

1. **Metadata**: Include the timetable name, and date/day of operation
   - Timetable names should not include 'severn valley railway' or anything similar.  Use the actual timetable name such as 'timetable A', or 'autumn timetable'.
3. **Trains array**: Each train object must contain:
   - `trainNumber`: the train identifier (e.g., "Diesel 37248", "Steam 75069", "Diesel DMU")
   - `direction`: either "northbound" (Bridgnorth to Kidderminster) or "southbound" (Kidderminster to Bridgnorth)
   - `stops`: an array of stations on the route in order of travel

4. **Station order** (always in this order from north to south):
   - Kidderminster, Bewdley, Arley, Highley, Hampton Loade, Bridgnorth

5. **For each stop in the stops array**:
   - `station`: station name
   - `arrival`: arrival time in HH:MM format (use when train stops, except at origin station)
   - `departure`: departure time in HH:MM format (use when train stops, except at destination station)
   - `time`: estimated pass-through time for non-stopping trains (use this field only when `stopsAt` is false)
   - `stopsAt`: boolean - `true` if train stops, `false` if passing through

## Critical Rules:

### Stopping trains:
- If the timetable shows a time at a station, the train STOPS there
- Set `stopsAt: true`
- For intermediate stops: include both `arrival` (3 minutes before departure) and `departure` times
- For the ORIGIN station (first stop): only include `departure` time (no arrival time)
- For the DESTINATION station (last stop): only include `arrival` time (no departure time), using the actual timetable time
- Example intermediate stop: If timetable shows "10:40", use `"arrival": "10:37", "departure": "10:40", "stopsAt": true`
- Example origin: `"departure": "09:40", "stopsAt": true`
- Example destination: `"arrival": "11:25", "stopsAt": true`

### Passing trains (marked with "-"):
- If the timetable shows "-" for a station, the train does NOT stop but DOES pass through
- Set `stopsAt: false`
- Include a `time` field with an estimated pass-through time
- Estimate the pass-through time based on typical journey times between adjacent stations
- Look at other trains' journey times between the same stations to estimate reasonable timings
- DO NOT include `arrival` or `departure` fields for passing stations

### Blank stations (not serviced):
- If the timetable shows a BLANK (empty cell) for a station, the train does NOT serve that station at all
- DO NOT include this station in the stops array for that train
- DO NOT estimate times for blank stations
- The train either hasn't reached that station yet or has already terminated before it
- A blank indicates the terminus of a local/partial service

### Local/Partial trains:
- Some trains do not travel the complete line
- If stations at the start or end of a route are blank, the train starts/ends at a different station
- Only include stations that have either a time or a "-" symbol
- The first station with a time is the origin (departure only), the last station with a time or "-" is the terminus (arrival only if stopping)
- Apply the same origin/destination rules: first stop has departure only, last stop has arrival only

### Times with 'x' notation:
- The 'x' in times (like "11x10" or "2x05") indicates trains crossing at stations
- **IGNORE the 'x' character itself** - it's just notation indicating a crossing point
- Treat times with 'x' exactly the same as regular times - they are STOPS where the train calls
- "11x10" means the train stops at 11:10 (same as if it showed "11.10")
- "2x05" means the train stops at 14:05 in 24-hour format (same as if it showed "2.05")
- "4x47" means the train stops at 16:47 in 24-hour format (same as if it showed "4.47")
- Apply the same origin/destination/intermediate stop rules: first stop = departure only, last stop = arrival only, intermediate = both arrival and departure

### Time format:
- Always use 24-hour format (HH:MM)
- Ensure all times are strings: "14:30" not 14:30
- Convert PM times correctly (add 12 to hours after 12:59)

### Terminus/Origin/Destination stations:
- **Origin station** (where train starts): Include only `departure` time
- **Destination station** (where train ends): Include only `arrival` time (use the actual timetable time, not minus 3 minutes)
- **Intermediate stations**: Include both `arrival` (3 minutes before departure) and `departure` times
- This applies to both main terminus stations (Kidderminster/Bridgnorth) AND partial service terminus stations (like Highley when a local service terminates there)

## Example output structure:
```json
{
  "name": "Special timetable",
  "date": "Saturday 11 October only",
  "trains": [
    {
      "trainNumber": "Diesel 37248",
      "direction": "northbound",
      "stops": [
        { "station": "Bridgnorth", "departure": "09:40", "stopsAt": true },
        { "station": "Hampton Loade", "time": "09:58", "stopsAt": false },
        { "station": "Highley", "time": "10:08", "stopsAt": false },
        { "station": "Arley", "time": "10:20", "stopsAt": false },
        { "station": "Bewdley", "arrival": "10:24", "departure": "10:27", "stopsAt": true },
        { "station": "Kidderminster", "arrival": "10:40", "stopsAt": true }
      ]
    },
    {
      "trainNumber": "Diesel 37248",
      "direction": "southbound",
      "stops": [
        { "station": "Highley", "departure": "13:52", "stopsAt": true },
        { "station": "Hampton Loade", "arrival": "13:59", "departure": "14:05", "stopsAt": true },
        { "station": "Bridgnorth", "arrival": "14:20", "stopsAt": true }
      ]
    },
    {
      "trainNumber": "Diesel 37248",
      "direction": "northbound",
      "stops": [
        { "station": "Bridgnorth", "departure": "13:00", "stopsAt": true },
        { "station": "Hampton Loade", "arrival": "13:22", "departure": "13:25", "stopsAt": true },
        { "station": "Highley", "arrival": "13:35", "stopsAt": true }
      ]
    }
  ]
}
