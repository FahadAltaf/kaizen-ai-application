using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<DataService>();
        services.AddSingleton<AIAssistant>();
        services.AddSingleton<WhatsAppService>();
        services.AddHttpClient();
    })
    .Build();

host.Run();
