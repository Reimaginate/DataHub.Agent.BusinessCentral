using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Configuration;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Hosting;

public sealed class BusinessCentralAgentWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<StarterOptions> options,
    ILogger<BusinessCentralAgentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.ScheduledProcessingEnabled)
        {
            throw new InvalidOperationException(
                "The worker command requires Starter:ScheduledProcessingEnabled=true.");
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollingIntervalSeconds));
        do
        {
            try
            {
                logger.LogInformation("Starting Business Central/DataHub processing pass.");
                await using var scope = scopeFactory.CreateAsyncScope();
                var processingPlan = scope.ServiceProvider.GetRequiredService<IEntityProcessingPlan>();
                await processingPlan.RunOnceAsync(stoppingToken);
                logger.LogInformation("Business Central/DataHub processing pass completed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Processing pass failed. Marker safety prevents an unsafe advance; the next pass will retry.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
