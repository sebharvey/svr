using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SevernValleyTimetable.Functions;

public class TimetableScheduleEntry
{
    public string Date { get; set; } = string.Empty;
    public string Timetable { get; set; } = string.Empty;
}

public class TimetableService
{
    private readonly ILogger<TimetableService> _logger;
    private readonly string _timetablesBasePath;
    private readonly Dictionary<string, string> _timetableCache = new();
    private readonly Dictionary<int, List<TimetableScheduleEntry>> _scheduleCache = new();

    public TimetableService(ILogger<TimetableService> logger)
    {
        _logger = logger;
        _timetablesBasePath = Path.Combine(AppContext.BaseDirectory, "Timetables");
    }

    public async Task<string> GetTimetableForDateAsync(DateTime date)
    {
        var year = date.Year;
        
        _logger.LogInformation("Looking for timetable for date: {Date}", date.ToString("yyyy-MM-dd"));

        // Load the schedule for this year
        var schedule = await LoadScheduleAsync(year);
        
        if (schedule != null && schedule.Count > 0)
        {
            // Format the date to match the schedule format (dd-MMM, e.g., "18-Oct")
            var dateKey = date.ToString("dd-MMM");
            
            // Find matching schedule entry
            var scheduleEntry = schedule.FirstOrDefault(s => 
                string.Equals(s.Date, dateKey, StringComparison.OrdinalIgnoreCase));
            
            if (scheduleEntry != null && !string.IsNullOrWhiteSpace(scheduleEntry.Timetable))
            {
                _logger.LogInformation("Found schedule entry for {Date}: {Timetable}", dateKey, scheduleEntry.Timetable);
                
                // Load the specified timetable file
                var timetableFileName = $"{scheduleEntry.Timetable}.json";
                var filePath = Path.Combine(_timetablesBasePath, year.ToString(), timetableFileName);
                
                if (File.Exists(filePath))
                {
                    return await LoadAndCacheTimetable(filePath);
                }
                
                _logger.LogWarning("Scheduled timetable file not found: {FilePath}", filePath);
            }
            else
            {
                _logger.LogInformation("No schedule entry found for {Date}", dateKey);
            }
        }

        // Fall back to default.json for the year
        var defaultFilePath = Path.Combine(_timetablesBasePath, year.ToString(), "default.json");
        _logger.LogInformation("Falling back to default timetable: {FilePath}", defaultFilePath);

        if (!File.Exists(defaultFilePath))
        {
            _logger.LogWarning("No timetable found for year {Year}", year);
            throw new FileNotFoundException($"No timetable found for date {date:yyyy-MM-dd}");
        }

        return await LoadAndCacheTimetable(defaultFilePath);
    }

    private async Task<List<TimetableScheduleEntry>?> LoadScheduleAsync(int year)
    {
        // Check cache first
        if (_scheduleCache.TryGetValue(year, out var cachedSchedule))
        {
            return cachedSchedule;
        }

        var scheduleFilePath = Path.Combine(_timetablesBasePath, year.ToString(), "schedule.json");
        
        if (!File.Exists(scheduleFilePath))
        {
            _logger.LogInformation("No schedule.json found for year {Year}", year);
            return null;
        }

        try
        {
            var scheduleJson = await File.ReadAllTextAsync(scheduleFilePath);
            var schedule = JsonSerializer.Deserialize<List<TimetableScheduleEntry>>(scheduleJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (schedule != null)
            {
                _scheduleCache[year] = schedule;
                _logger.LogInformation("Loaded schedule for year {Year} with {Count} entries", year, schedule.Count);
            }

            return schedule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading schedule.json for year {Year}", year);
            return null;
        }
    }

    private async Task<string> LoadAndCacheTimetable(string filePath)
    {
        // Check cache first
        if (_timetableCache.TryGetValue(filePath, out var cachedContent))
        {
            _logger.LogInformation("Returning cached timetable for {FilePath}", filePath);
            return cachedContent;
        }

        var content = await File.ReadAllTextAsync(filePath);
        
        // Validate JSON
        try
        {
            JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in timetable file: {FilePath}", filePath);
            throw new InvalidOperationException($"Invalid JSON in timetable file: {filePath}", ex);
        }

        // Cache the content
        _timetableCache[filePath] = content;
        
        return content;
    }

    public async Task<List<string>> GetAvailableTimetablesAsync()
    {
        var timetables = new List<string>();
        
        if (!Directory.Exists(_timetablesBasePath))
        {
            _logger.LogWarning("Timetables base directory not found: {Path}", _timetablesBasePath);
            return timetables;
        }

        var yearDirectories = Directory.GetDirectories(_timetablesBasePath);
        
        foreach (var yearDir in yearDirectories)
        {
            var jsonFiles = Directory.GetFiles(yearDir, "*.json")
                .Where(f => !f.EndsWith("schedule.json", StringComparison.OrdinalIgnoreCase));
            timetables.AddRange(jsonFiles.Select(f => Path.GetRelativePath(_timetablesBasePath, f)));
        }

        return timetables;
    }
}