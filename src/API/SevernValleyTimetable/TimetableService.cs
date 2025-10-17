using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SevernValleyTimetable.Functions;

public class TimetableService
{
    private readonly ILogger<TimetableService> _logger;
    private readonly string _timetablesBasePath;
    private readonly Dictionary<string, string> _timetableCache = new();

    public TimetableService(ILogger<TimetableService> logger)
    {
        _logger = logger;
        _timetablesBasePath = Path.Combine(AppContext.BaseDirectory, "Timetables");
    }

    public async Task<string> GetTimetableForDateAsync(DateTime date)
    {
        var year = date.Year;
        var month = date.ToString("MMM").ToLower(); // "oct", "nov", etc.
        var day = date.Day;
        
        // Try to find a date-specific timetable: oct-18.json
        var fileName = $"{month}-{day}.json";
        var filePath = Path.Combine(_timetablesBasePath, year.ToString(), fileName);
        
        _logger.LogInformation("Looking for timetable: {FilePath}", filePath);

        // Check cache first
        if (_timetableCache.TryGetValue(filePath, out var cachedContent))
        {
            _logger.LogInformation("Returning cached timetable for {FileName}", fileName);
            return cachedContent;
        }

        // If date-specific file exists, use it
        if (File.Exists(filePath))
        {
            _logger.LogInformation("Found date-specific timetable: {FileName}", fileName);
            return await LoadAndCacheTimetable(filePath);
        }

        // Fall back to default.json for the year
        var defaultFilePath = Path.Combine(_timetablesBasePath, year.ToString(), "default.json");
        _logger.LogInformation("Date-specific timetable not found, trying default: {FilePath}", defaultFilePath);

        if (!File.Exists(defaultFilePath))
        {
            _logger.LogWarning("No timetable found for year {Year}", year);
            throw new FileNotFoundException($"No timetable found for date {date:yyyy-MM-dd}");
        }

        return await LoadAndCacheTimetable(defaultFilePath);
    }

    private async Task<string> LoadAndCacheTimetable(string filePath)
    {
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
            var jsonFiles = Directory.GetFiles(yearDir, "*.json");
            timetables.AddRange(jsonFiles.Select(f => Path.GetRelativePath(_timetablesBasePath, f)));
        }

        return timetables;
    }
}