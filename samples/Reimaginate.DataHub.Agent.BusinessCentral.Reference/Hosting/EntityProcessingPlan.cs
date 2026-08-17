using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Configuration;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeUpdatedBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncUpdatedDataHubEntities;
using Reimaginate.Mediator;
using BCSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrder;
using DHSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrder;
using DHSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrderLine;
using BCSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Hosting;

public interface IEntityProcessingPlan
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The dependency order for this starter. Reference entities are processed before
/// documents, and document headers before lines.
/// </summary>
public sealed class EntityProcessingPlan(
    IMediator mediator,
    IOptions<StarterOptions> starterOptions,
    IOptions<BusinessCentralAgentOptions> agentOptions) : IEntityProcessingPlan
{
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var options = starterOptions.Value;
        StarterConfiguration.EnsureWritesAllowed(options, agentOptions.Value.Environment);
        var correlationId = Guid.NewGuid().ToString();

        // Inbound incremental merge. Standard salesOrderLines do not expose a
        // lastModifiedDateTime field; merge those by known id or from an event source.
        await SendAsync(new MergeUpdatedBusinessCentralEntitiesRequest<Customer, Account>
        {
            BatchSize = options.BatchSize,
            CorrelationId = correlationId
        }, cancellationToken);
        await SendAsync(new MergeUpdatedBusinessCentralEntitiesRequest<Item, Product>
        {
            BatchSize = options.BatchSize,
            CorrelationId = correlationId
        }, cancellationToken);
        await SendAsync(new MergeUpdatedBusinessCentralEntitiesRequest<BCSalesOrder, DHSalesOrder>
        {
            BatchSize = options.BatchSize,
            CorrelationId = correlationId
        }, cancellationToken);

        // Outbound sync. Parents and referenced records must be tracked before lines.
        await SendAsync(new SyncUpdatedDataHubEntitiesRequest<Account, Customer>
        {
            BatchSize = options.BatchSize,
            CorrelationId = correlationId
        }, cancellationToken);
        await SendAsync(new SyncUpdatedDataHubEntitiesRequest<Product, Item>
        {
            BatchSize = options.BatchSize,
            CorrelationId = correlationId
        }, cancellationToken);
        await SendAsync(new SyncUpdatedDataHubEntitiesRequest<DHSalesOrder, BCSalesOrder>
        {
            BatchSize = options.BatchSize,
            CorrelationId = correlationId
        }, cancellationToken);
        await SendAsync(new SyncUpdatedDataHubEntitiesRequest<DHSalesOrderLine, BCSalesOrderLine>
        {
            BatchSize = options.BatchSize,
            CorrelationId = correlationId
        }, cancellationToken);
    }

    private async Task SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        _ = (await mediator.TrySend<TResponse>(request, cancellationToken)) switch
        {
            { Item2: { } exception } => throw exception,
            { Item1: var response } => response
        };
    }
}
