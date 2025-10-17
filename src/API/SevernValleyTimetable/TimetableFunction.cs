using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SevernValleyTimetable.Functions;

public class TimetableFunction
{
    private readonly ILogger<TimetableFunction> _logger;
    private readonly TimetableService _timetableService;

    public TimetableFunction(ILogger<TimetableFunction> logger, TimetableService timetableService)
    {
        _logger = logger;
        _timetableService = timetableService;
    }

    [Function("GetTimetable")]
    public async Task<HttpResponseData> GetTimetable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timetable")] HttpRequestData req)
    {
        _logger.LogInformation("Getting timetable for current date");

        try
        {
            var timetable = await _timetableService.GetTimetableForDateAsync(DateTime.UtcNow);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            
            await response.WriteStringAsync(timetable);
            
            return response;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Timetable not found");
            
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "No timetable found for the current date" }));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving timetable");
            
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "An error occurred while retrieving the timetable" }));
            
            return response;
        }
    }
}