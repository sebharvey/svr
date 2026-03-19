// Author: Seb Harvey
// Description: Minimal API for the Severn Valley Timetable

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TimetableService>();

var app = builder.Build();

app.MapGet("/api/v1/timetable", async (HttpContext ctx, TimetableService timetableService) =>
{
    var debugParam = ctx.Request.Query["debug"].ToString();
    var isDebugMode = bool.TryParse(debugParam, out var debugValue) && debugValue;

    try
    {
        var timetable = await timetableService.GetTimetableForDateAsync(DateTime.UtcNow, isDebugMode);
        return Results.Text(timetable, "application/json");
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound(new { error = "No timetable found for the current date" });
    }
});

app.MapGet("/api/v1/health", async (TimetableService timetableService) =>
{
    try
    {
        var availableTimetables = await timetableService.GetAvailableTimetablesAsync();

        return Results.Ok(new
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
        return Results.Json(new
        {
            status = "unhealthy",
            timestamp = DateTime.UtcNow,
            service = "SevernValleyTimetable",
            version = "1.0.0",
            error = ex.Message,
            checks = new { timetableService = "error", fileSystem = "error" }
        }, statusCode: 503);
    }
});

app.Run();
