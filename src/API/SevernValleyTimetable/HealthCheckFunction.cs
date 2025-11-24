using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SevernValleyTimetable.Functions;

public class HealthCheckFunction
{
    private readonly ILogger<HealthCheckFunction> _logger;
    private readonly TimetableService _timetableService;

    public HealthCheckFunction(ILogger<HealthCheckFunction> logger, TimetableService timetableService)
    {
        _logger = logger;
        _timetableService = timetableService;
    }

    [Function("HealthCheck")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check requested");

        try
        {
            // Check if we can access timetables directory
            var availableTimetables = await _timetableService.GetAvailableTimetablesAsync();
            
            var healthStatus = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "SevernValleyTimetable",
                version = "1.0.0",
                timetablesAvailable = availableTimetables.Count,
                checks = new
                {
                    timetableService = "ok",
                    fileSystem = "ok"
                }
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(healthStatus, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            var healthStatus = new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "SevernValleyTimetable",
                version = "1.0.0",
                error = ex.Message,
                checks = new
                {
                    timetableService = "error",
                    fileSystem = "error"
                }
            };

            var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(healthStatus, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));

            return response;
        }
    }
}