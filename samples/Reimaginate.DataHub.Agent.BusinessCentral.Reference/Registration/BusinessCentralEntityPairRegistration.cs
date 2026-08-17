using Microsoft.Extensions.DependencyInjection;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.CreateBusinessCentralRecords;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.UpdateBusinessCentralRecords;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Queries.GetSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeUpdatedBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeBusinessCentralEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeDependencyBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RegisterAlternateKey;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ResolveResolutionPromises;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RetrieveUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeFailuresToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeSuccessesToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncFailuresToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncSuccessesToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDataHubEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDependencyDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralSyncMarker;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Registration;

/// <summary>
/// Registers the closed generic handlers required for one DataHub/Business Central
/// entity pair. Copy one registration call for every pair in your solution.
/// </summary>
public static class BusinessCentralEntityPairRegistration
{
    public static IServiceCollection AddBusinessCentralPipelineHandlers(this IServiceCollection services)
    {
        services.AddTransient<IHandler<SendMergeFailuresToDataHubRequest, NullResponse>, SendMergeFailuresToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendMergeSuccessesToDataHubRequest, NullResponse>, SendMergeSuccessesToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendSyncFailuresToDataHubRequest, NullResponse>, SendSyncFailuresToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendSyncSuccessesToDataHubRequest, NullResponse>, SendSyncSuccessesToDataHubRequestHandler>();
        services.AddTransient<IHandler<GetBusinessCentralMergeMarkerRequest, GetBusinessCentralMergeMarkerResponse>, GetBusinessCentralMergeMarkerRequestHandler>();
        services.AddTransient<IHandler<UpdateBusinessCentralMergeMarkerRequest, UpdateBusinessCentralMergeMarkerResponse>, UpdateBusinessCentralMergeMarkerRequestHandler>();
        services.AddTransient<IHandler<GetBusinessCentralSyncMarkerRequest, GetBusinessCentralSyncMarkerResponse>, GetBusinessCentralSyncMarkerRequestHandler>();
        services.AddTransient<IHandler<UpdateBusinessCentralSyncMarkerRequest, UpdateBusinessCentralSyncMarkerResponse>, UpdateBusinessCentralSyncMarkerRequestHandler>();
        services.AddTransient<IHandler<RegisterAlternateKeyRequest, NullResponse>, RegisterAlternateKeyRequestHandler>();
        return services;
    }

    public static IServiceCollection AddBusinessCentralEntityPair<TDataHubEntity, TBusinessCentralEntity>(
        this IServiceCollection services)
        where TDataHubEntity : DataHubEntity, new()
        where TBusinessCentralEntity : BusinessCentralDocument, new()
    {
        services.AddTransient<IHandler<GetSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity>, List<TBusinessCentralEntity>>, GetSpecificBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity>>();
        services.AddTransient<IHandler<CreateBusinessCentralRecordsCommand<TBusinessCentralEntity>, CreateBusinessCentralRecordsResponse<TBusinessCentralEntity>>, CreateBusinessCentralRecordsCommandHandler<TBusinessCentralEntity>>();
        services.AddTransient<IHandler<UpdateBusinessCentralRecordsCommand<TBusinessCentralEntity>, UpdateBusinessCentralRecordsResponse<TBusinessCentralEntity>>, UpdateBusinessCentralRecordsCommandHandler<TBusinessCentralEntity>>();
        services.AddTransient<IHandler<RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity>, RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>>, RetrieveUpdatedDataHubEntitiesRequestHandler<TDataHubEntity>>();

        services.AddTransient<IHandler<SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse>, SyncSpecificDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<SyncDataHubEntitiesWithLocksRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse>, SyncDataHubEntitiesWithLocksRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse>, ProcessDataHubEntitySyncRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TBusinessCentralEntity>, EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TBusinessCentralEntity>>, EnsureReferencedEntitiesAreSyncdRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<ResolveResolutionPromisesRequest<TDataHubEntity, TBusinessCentralEntity>, ResolveResolutionPromisesResponse<TDataHubEntity, TBusinessCentralEntity>>, ResolveResolutionPromisesRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse>, SyncDependencyDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, NullResponse>, SyncUpdatedDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();

        services.AddTransient<IHandler<MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>, MergeSpecificBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<MergeBusinessCentralEntitiesWithLocksRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>, MergeBusinessCentralEntitiesWithLocksRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<ProcessBusinessCentralEntityMergeRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>, ProcessBusinessCentralEntityMergeRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<MergeDependencyBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>, MergeDependencyBusinessCentralEntitiesRequestRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
        return services;
    }

    public static IServiceCollection AddIncrementalBusinessCentralEntityPair<TDataHubEntity, TBusinessCentralEntity>(
        this IServiceCollection services)
        where TDataHubEntity : DataHubEntity, new()
        where TBusinessCentralEntity : BusinessCentralDocument, IBusinessCentralIncrementalEntity, new()
    {
        services.AddBusinessCentralEntityPair<TDataHubEntity, TBusinessCentralEntity>();
        services.AddTransient<IHandler<MergeUpdatedBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, NullResponse>, MergeUpdatedBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
        return services;
    }
}
