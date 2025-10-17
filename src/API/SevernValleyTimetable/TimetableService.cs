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
        // For now, always return the Oct 11 special timetable
        var year = 2025;
        var fileName = "oct-11-special.json";
        
        var filePath = Path.Combine(_timetablesBasePath, year.ToString(), fileName);
        
        _logger.LogInformation("Loading timetable from {FilePath}", filePath);

        // Check cache first
        if (_timetableCache.TryGetValue(filePath, out var cachedContent))
        {
            _logger.LogInformation("Returning cached timetable");
            return cachedContent;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Timetable file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Timetable file not found: {filePath}");
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
            var jsonFiles = Directory.GetFiles(yearDir, "*.json");
            timetables.AddRange(jsonFiles.Select(f => Path.GetRelativePath(_timetablesBasePath, f)));
        }

        return timetables;
    }
}