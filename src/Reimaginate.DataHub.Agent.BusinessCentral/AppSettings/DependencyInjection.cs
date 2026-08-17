using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.DataHubEntityCache;
using Reimaginate.DataHub.Client.Config;
using Reimaginate.ProcessingLockService;

namespace Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessCentralAgent(this IServiceCollection services, Action<AddBusinessCentralAgentOptions> options = null)
    {
        var addBusinessCentralAgentOptions = new AddBusinessCentralAgentOptions();
        options?.Invoke(addBusinessCentralAgentOptions);

        var configuredAgentOptions = addBusinessCentralAgentOptions.BusinessCentralAgentOptions;
        var configuredServiceOptions = addBusinessCentralAgentOptions.BusinessCentralServiceOptions;
        var dataSource = string.IsNullOrWhiteSpace(configuredAgentOptions.DataSource)
            ? "BusinessCentral"
            : configuredAgentOptions.DataSource;

        services.Configure<BusinessCentralAgentOptions>(agentOptions =>
        {
            addBusinessCentralAgentOptions.Config.Bind(agentOptions);
            agentOptions.AgentId = configuredAgentOptions.AgentId;
            agentOptions.DataSource = dataSource;
            agentOptions.Environment = configuredAgentOptions.Environment;
            agentOptions.SalesOrderStartingJobNumber = configuredAgentOptions.SalesOrderStartingJobNumber;
            agentOptions.ProcessingLockOptions = configuredAgentOptions.ProcessingLockOptions ?? new ProcessingLockOptions();
        });

        services.Configure<BusinessCentralServiceOptions>(serviceOptions =>
        {
            addBusinessCentralAgentOptions.Config.GetSection("BusinessCentralServiceOptions").Bind(serviceOptions);
            serviceOptions.BaseUrl = string.IsNullOrWhiteSpace(configuredServiceOptions.BaseUrl) ? serviceOptions.BaseUrl : configuredServiceOptions.BaseUrl;
            serviceOptions.CompanyId = string.IsNullOrWhiteSpace(configuredServiceOptions.CompanyId) ? serviceOptions.CompanyId : configuredServiceOptions.CompanyId;
            serviceOptions.CompanyName = string.IsNullOrWhiteSpace(configuredServiceOptions.CompanyName) ? serviceOptions.CompanyName : configuredServiceOptions.CompanyName;
        });

        services.AddOptions();
        services.AddHttpClient("BusinessCentral", (sp, client) =>
        {
            var serviceOptions = sp.GetRequiredService<IOptions<BusinessCentralServiceOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(serviceOptions.BaseUrl))
            {
                client.BaseAddress = new Uri(serviceOptions.BaseUrl, UriKind.Absolute);
            }
        });

        var processingLockOptions = configuredAgentOptions.ProcessingLockOptions ?? new ProcessingLockOptions();
        switch ((processingLockOptions.UseRepository ?? "InMemory").ToLowerInvariant())
        {
            case "inmemory":
                services.AddProcessingLockService(cfg => cfg.WithInMemoryRepository());
                break;

            case "redis":
                var redisClientOptions = processingLockOptions.RedisClientOptions;
                services.AddProcessingLockService(cfg =>
                {
                    cfg.WithRedisRepository(r =>
                    {
                        r.ConnString = redisClientOptions.ConnString;
                        r.ConnectTimeout = redisClientOptions.ConnectTimeout;
                        r.Protocol = redisClientOptions.Protocol;
                        r.SyncTimeout = redisClientOptions.SyncTimeout;
                    });
                });
                break;

            default:
                throw new InvalidOperationException($"Unsupported processing lock repository '{processingLockOptions.UseRepository}'.");
        }

        services.AddDataHubClient(cfg => cfg.WithAppSettingsConfig(addBusinessCentralAgentOptions.RootConfig, "DataHubClientOptions"));
        services.AddTransient<IBusinessCentralODataService, BusinessCentralODataService>();
        services.AddTransient<IDataHubEntityCache, DataHubEntityCache>();

        return services;
    }
}
