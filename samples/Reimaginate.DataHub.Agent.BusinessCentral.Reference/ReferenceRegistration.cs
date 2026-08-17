using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Commands;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Configuration;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Hosting;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Mapping;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Registration;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.Client.Config;
using Reimaginate.Mapper;
using Reimaginate.Mapper.Config;
using Reimaginate.Mediator;
using Reimaginate.Mediator.DependencyInjection;
using BCSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrder;
using BCSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrderLine;
using DHSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrder;
using DHSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference;

public static class ReferenceRegistration
{
    public static IServiceCollection AddBusinessCentralReference(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
        services.AddTransient<BusinessCentralBearerTokenHandler>();
        services.AddBusinessCentralAgent(options =>
            options.WithAppSettingsConfig(configuration, "BusinessCentralAgentOptions"));
        // Keep the external DataHub connection visible in copied code and bind it
        // from the conventional root-level DataHubClientOptions section.
        services.RemoveAll<IDataHubClient>();
        services.AddDataHubClient(options =>
            options.WithAppSettingsConfig(configuration, "DataHubClientOptions"));
        services.AddHttpClient("BusinessCentral")
            .AddHttpMessageHandler<BusinessCentralBearerTokenHandler>();

        services.Configure<StarterOptions>(configuration.GetSection(StarterOptions.SectionName));
        services.AddMapper(markers: [typeof(ReferenceRegistration)]);
        services.AddMediator(options => options
            .AddAssembliesToScan(typeof(ReferenceRegistration), typeof(SyncSpecificDataHubEntitiesRequest<,>))
            .WithThrowOnMissingHandler(true));

        // START HERE: add one call for each entity pair in your DataHub solution.
        services.AddBusinessCentralPipelineHandlers();
        services.AddIncrementalBusinessCentralEntityPair<Account, Customer>();
        services.AddIncrementalBusinessCentralEntityPair<Product, Item>();
        services.AddIncrementalBusinessCentralEntityPair<DHSalesOrder, BCSalesOrder>();
        services.AddBusinessCentralEntityPair<DHSalesOrderLine, BCSalesOrderLine>();

        services.AddScoped<IEntityProcessingPlan, EntityProcessingPlan>();
        services.AddScoped<ReferenceAgentOperations>();
        services.AddHostedService<BusinessCentralAgentWorker>();
        return services;
    }

    public static void ValidateRegistrations(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        provider = scope.ServiceProvider;
        _ = provider.GetRequiredService<IDataHubClient>();
        _ = provider.GetRequiredService<IBusinessCentralODataService>();
        _ = provider.GetRequiredService<IMapper>();
        _ = provider.GetRequiredService<IMediator>();
        _ = provider.GetRequiredService<IOptions<StarterOptions>>();
        _ = provider.GetRequiredService<IHandler<
            SyncSpecificDataHubEntitiesRequest<Account, Customer>,
            ProcessDataHubEntitySyncResponse>>();
        _ = provider.GetRequiredService<IHandler<
            SyncSpecificDataHubEntitiesRequest<DHSalesOrderLine, BCSalesOrderLine>,
            ProcessDataHubEntitySyncResponse>>();
    }
}

public sealed class BusinessCentralBearerTokenHandler(TokenCredential credential) : DelegatingHandler
{
    private static readonly TokenRequestContext TokenContext =
        new(["https://api.businesscentral.dynamics.com/.default"]);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await credential.GetTokenAsync(TokenContext, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}
