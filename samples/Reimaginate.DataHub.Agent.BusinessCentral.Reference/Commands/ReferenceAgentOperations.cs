using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Configuration;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Hosting;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RetrieveUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.Mediator;
using Reimaginate.DataHub.SharedModels.Core;
using BCSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrder;
using BCSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrderLine;
using DHSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrder;
using DHSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Commands;

public sealed class ReferenceAgentOperations(
    IMediator mediator,
    IBusinessCentralODataService businessCentral,
    IEntityProcessingPlan processingPlan,
    IOptions<StarterOptions> starterOptions,
    IOptions<BusinessCentralAgentOptions> agentOptions)
{
    public async Task SmokeAsync(CancellationToken cancellationToken = default)
    {
        var customers = await businessCentral.GetEntitiesAsync<Customer>(top: 1, cancellationToken: cancellationToken);
        if (customers.IsT2)
        {
            throw customers.AsT2;
        }
        if (customers.IsT1)
        {
            throw new InvalidOperationException(
                $"Business Central customer read failed with {(int)customers.AsT1.StatusCode} {customers.AsT1.StatusCode}.");
        }

        // A future boundary makes this a cheap read-only connectivity check while still
        // exercising the configured IDataHubClient and Account contract.
        var dataHub = await SendAsync(new RetrieveUpdatedDataHubEntitiesRequest<Account>
        {
            FromDateTime = DateTimeOffset.UtcNow.AddMinutes(1),
            PageSize = 1
        }, cancellationToken);

        Console.WriteLine(
            $"Business Central connection succeeded ({customers.AsT0.Value.Count} customer sample record(s)).");
        Console.WriteLine(
            $"DataHub connection succeeded ({dataHub.Results.Count} future-dated Account record(s), expected 0).");
    }

    public Task RunOnceAsync(CancellationToken cancellationToken = default) =>
        processingPlan.RunOnceAsync(cancellationToken);

    public async Task SyncAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        EnsureWritesAllowed();
        var response = entityType.ToLowerInvariant() switch
        {
            "account" => await SyncAsync<Account, Customer>(entityId, cancellationToken),
            "product" => await SyncAsync<Product, Item>(entityId, cancellationToken),
            "salesorder" => await SyncAsync<DHSalesOrder, BCSalesOrder>(entityId, cancellationToken),
            "salesorderline" => await SyncAsync<DHSalesOrderLine, BCSalesOrderLine>(entityId, cancellationToken),
            _ => throw new ArgumentException(
                "Supported DataHub entity types are Account, Product, SalesOrder, and SalesOrderLine.",
                nameof(entityType))
        };

        foreach (var result in response.Results)
        {
            Console.WriteLine(
                $"{result.DataHubEntityId}: {result.SyncOutcome}" +
                (string.IsNullOrWhiteSpace(result.FailureReason) ? string.Empty : $" - {result.FailureReason}"));
        }
    }

    public async Task MergeAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        EnsureWritesAllowed();
        if (!Guid.TryParse(entityId, out _))
        {
            throw new ArgumentException("A Business Central entity id must be a GUID.", nameof(entityId));
        }

        IReadOnlyList<MergeEntityResult> results = entityType.ToLowerInvariant() switch
        {
            "customer" => (await MergeAsync<Customer, Account>(entityId, cancellationToken)).Results,
            "item" => (await MergeAsync<Item, Product>(entityId, cancellationToken)).Results,
            "salesorder" => (await MergeAsync<BCSalesOrder, DHSalesOrder>(entityId, cancellationToken)).Results,
            "salesorderline" => (await MergeAsync<BCSalesOrderLine, DHSalesOrderLine>(entityId, cancellationToken)).Results,
            _ => throw new ArgumentException(
                "Supported Business Central entity types are Customer, Item, SalesOrder, and SalesOrderLine.",
                nameof(entityType))
        };

        foreach (var result in results)
        {
            Console.WriteLine(
                $"{result.SourceEntityId}: {result.MergeOutcome}" +
                (string.IsNullOrWhiteSpace(result.FailureReason) ? string.Empty : $" - {result.FailureReason}"));
        }
    }

    private Task<ProcessDataHubEntitySyncResponse> SyncAsync<TDataHub, TBusinessCentral>(
        string id,
        CancellationToken cancellationToken)
        where TDataHub : Reimaginate.DataHub.SharedModels.Core.DataHubEntity
        where TBusinessCentral : Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models.BusinessCentralDocument =>
        SendAsync(new SyncSpecificDataHubEntitiesRequest<TDataHub, TBusinessCentral>
        {
            EntityIds = [id],
            CorrelationId = Guid.NewGuid().ToString()
        }, cancellationToken);

    private Task<ProcessBusinessCentralEntityMergeResponse<TBusinessCentral, TDataHub>> MergeAsync<TBusinessCentral, TDataHub>(
        string id,
        CancellationToken cancellationToken)
        where TDataHub : Reimaginate.DataHub.SharedModels.Core.DataHubEntity
        where TBusinessCentral : Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models.BusinessCentralDocument =>
        SendAsync(new MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentral, TDataHub>([id], Guid.NewGuid().ToString()), cancellationToken);

    private async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken) =>
        (await mediator.TrySend<TResponse>(request, cancellationToken)) switch
        {
            { Item2: { } exception } => throw exception,
            { Item1: var response } => response!
        };

    private void EnsureWritesAllowed() =>
        StarterConfiguration.EnsureWritesAllowed(starterOptions.Value, agentOptions.Value.Environment);
}
