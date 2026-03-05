# Add Timetable

Convert a railway timetable image into JSON and add it to the API.

**Usage**: `/add-timetable <timetable-filename> <full-date>`

**Arguments**: $ARGUMENTS

- `timetable-filename`: The kebab-case filename (without .json) to save as, e.g. `timetable-c`, `easter-special`, `autumn-gala-sat`
- `full-date`: The full display date string, e.g. `Saturday 15 March 2026`

---

Parse the two arguments from `$ARGUMENTS`. The first word/token is the timetable filename, and the remainder is the full date string. For example:

- `/add-timetable timetable-c Saturday 15 March 2026` → filename: `timetable-c`, date: `Saturday 15 March 2026`
- `/add-timetable easter-special Sunday 5 April 2026` → filename: `easter-special`, date: `Sunday 5 April 2026`

Extract the year from the date string to determine which year folder and schedule file to use.

---

## Step 1 – Convert the timetable image to JSON

The user has provided a timetable image (attached to this conversation or referenced above). Convert it to JSON using the rules below.

### JSON Structure Requirements

1. **Metadata**: Include the timetable name and date/day of operation
   - Use the provided `timetable-filename` argument as the basis — humanise it for the `"name"` field (e.g. `timetable-c` → `"Timetable C"`, `easter-special` → `"Easter Special"`)
   - Use the provided `full-date` argument exactly as the `"date"` field
   - Timetable names must NOT include "Severn Valley Railway" or similar. Use the actual timetable name such as "Timetable A" or "Autumn Timetable".

2. **Trains array**: Each train object must contain:
   - `trainNumber`: the train identifier (e.g. `"Diesel 37248"`, `"Steam 75069"`, `"Diesel DMU"`)
   - `direction`: either `"northbound"` (Bridgnorth → Kidderminster) or `"southbound"` (Kidderminster → Bridgnorth)
   - `stops`: an array of stations on the route in order of travel

3. **Station order and direction definitions**:
   - **IMPORTANT**: "Northbound" and "Southbound" are naming conventions that do NOT match geographic directions
   - **Northbound** = Bridgnorth → Kidderminster
   - **Southbound** = Kidderminster → Bridgnorth
   - Station order along the line: Bridgnorth, Hampton Loade, Highley, Arley, Bewdley, Kidderminster
   - If first station is Bridgnorth (or closer to Bridgnorth) travelling towards Kidderminster → `"northbound"`
   - If first station is Kidderminster (or closer to Kidderminster) travelling towards Bridgnorth → `"southbound"`

4. **For each stop in the stops array**:
   - `station`: station name
   - `arrival`: arrival time in HH:MM format (intermediate and destination stops only)
   - `departure`: departure time in HH:MM format (origin and intermediate stops only)
   - `time`: estimated pass-through time for non-stopping trains (only when `stopsAt` is false)
   - `stopsAt`: boolean — `true` if train stops, `false` if passing through

### Critical Rules

#### Stopping trains (timetable shows a time):
- Set `stopsAt: true`
- **Origin station** (first stop): `departure` only, no `arrival`
- **Destination station** (last stop): `arrival` only (use the actual timetable time), no `departure`
- **Intermediate stops**: both `arrival` (3 minutes before departure) and `departure`
- Example intermediate: timetable shows "10:40" → `"arrival": "10:37", "departure": "10:40", "stopsAt": true`

#### Passing trains (timetable shows `-`):
- Set `stopsAt: false`
- Include a `time` field with an estimated pass-through time based on adjacent station timings
- Do NOT include `arrival` or `departure` fields

#### Blank stations (empty cell):
- Train does not serve this station at all — do NOT include it in the stops array
- Do NOT estimate times for blank stations

#### Local/Partial trains:
- Only include stations that have a time or a `-` symbol
- Apply the same origin/destination rules

#### Times with `x` notation (e.g. `11x10`, `2x05`):
- Ignore the `x` — treat as a regular stop time
- `11x10` = stop at 11:10, `2x05` = stop at 14:05, `4x47` = stop at 16:47

#### Time format:
- Always use 24-hour HH:MM strings
- Convert PM times correctly (add 12 for hours after 12:59)

### Example output structure

```json
{
  "name": "Special Timetable",
  "date": "Saturday 11 October 2025",
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
    }
  ]
}
```

---

## Step 2 – Save the timetable JSON file

Determine the save path:
```
src/API/SevernValleyTimetable/Timetables/{year}/{timetable-filename}.json
```

Where `{year}` is extracted from the full-date argument (e.g. `2026`). If no year is present in the date string, default to the current year.

**Important**: Always create a new file — never overwrite an existing one. If a file already exists at that path, append a numeric suffix to make it unique:
- First try: `{timetable-filename}.json`
- If taken: `{timetable-filename}-2.json`
- If taken: `{timetable-filename}-3.json`
- And so on, incrementing until a free filename is found.

Use the chosen unique filename for both this step and Step 3.

Use 2-space indentation and consistent camelCase field names.

---

## Step 3 – Update the schedule.json

Read the existing schedule file at:
```
src/API/SevernValleyTimetable/Timetables/{year}/schedule.json
```

Always **append** a new entry for this timetable — do not update or replace any existing entry, even if one with the same timetable filename already exists. The `"date"` field must use `dd-MMM` format (e.g. `"15-Mar"`, `"05-Apr"`). The `"timetable"` field is the unique filename chosen in Step 2 (without `.json`).

Example entry to add:
```json
{
  "date": "15-Mar",
  "timetable": "timetable-c"
}
```

Keep the array sorted by date order if possible. If the schedule file does not exist for that year, create it as a new JSON array.

---

## Step 4 – Confirm

Report back:
- The file saved: `src/API/SevernValleyTimetable/Timetables/{year}/{timetable-filename}.json`
- The schedule entry added to: `src/API/SevernValleyTimetable/Timetables/{year}/schedule.json`
- A summary of trains converted (count, directions)
