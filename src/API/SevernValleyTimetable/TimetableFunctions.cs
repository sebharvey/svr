using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SevernValleyTimetable;

public class TimetableFunctions
{
    private readonly TimetableService _timetableService;
    private readonly ILogger<TimetableFunctions> _logger;

    public TimetableFunctions(TimetableService timetableService, ILogger<TimetableFunctions> logger)
    {
        _timetableService = timetableService;
        _logger = logger;
    }

    [Function("GetTimetable")]
    public async Task<IActionResult> GetTimetable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/timetable")] HttpRequest req)
    {
        var debugParam = req.Query["debug"].ToString();
        var isDebugMode = bool.TryParse(debugParam, out var debugValue) && debugValue;

        try
        {
            var timetable = await _timetableService.GetTimetableForDateAsync(DateTime.UtcNow, isDebugMode);
            return new ContentResult
            {
                Content = timetable,
                ContentType = "application/json",
                StatusCode = 200
            };
        }
        catch (FileNotFoundException)
        {
            return new NotFoundObjectResult(new { error = "No timetable found for the current date" });
        }
    }

    [Function("GetHealth")]
    public async Task<IActionResult> GetHealth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/health")] HttpRequest req)
    {
        try
        {
            var availableTimetables = await _timetableService.GetAvailableTimetablesAsync();

            return new OkObjectResult(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "SevernValleyTimetable",
                version = "1.0.0",
                timetablesAvailable = availableTimetables.Count,
                checks = new { timetableService = "ok", fileSystem = "ok" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new ObjectResult(new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "SevernValleyTimetable",
                version = "1.0.0",
                error = ex.Message,
                checks = new { timetableService = "error", fileSystem = "error" }
            })
            {
                StatusCode = 503
            };
        }
    }
}
