// Author: Seb Harvey
// Description: Minimal API for the Severn Valley Timetable, hosted in Azure Functions isolated worker

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SevernValleyTimetable;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<TimetableService>();
    })
    .Build();

host.Run();
