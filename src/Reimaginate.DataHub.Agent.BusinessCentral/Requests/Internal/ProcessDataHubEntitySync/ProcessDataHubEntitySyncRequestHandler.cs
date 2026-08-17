using JsonDiffPatchDotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Constants;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.CreateBusinessCentralRecords;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.UpdateBusinessCentralRecords;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Queries.GetSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Helpers;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ResolveResolutionPromises;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.DataHubEntityCache;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Reimaginate.Mediator;
using EntityReference = Reimaginate.DataHub.SharedModels.Core.EntityReference;
using Options = JsonDiffPatchDotNet.Options;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;

public class ProcessDataHubEntitySyncRequestHandler<TDataHubEntity, TBusinessCentralDocument> : IHandler<ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralDocument>, ProcessDataHubEntitySyncResponse> where TDataHubEntity : DataHubEntity, new() where TBusinessCentralDocument : BusinessCentralDocument, new()
{
    private readonly IOptions<BusinessCentralAgentOptions> _businessCentralAgentConfig;
    private readonly IDataHubClient _dataHubClient;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IDataHubEntityCache _dataHubEntityCache;
    private readonly IServiceProvider _serviceProvider;

    public ProcessDataHubEntitySyncRequestHandler(IOptions<BusinessCentralAgentOptions> businessCentralAgentConfig, IDataHubClient dataHubClient, IMapper mapper, IMediator mediator, IDataHubEntityCache dataHubEntityCache, IServiceProvider serviceProvider)
    {
        _businessCentralAgentConfig = businessCentralAgentConfig;
        _dataHubClient = dataHubClient;
        _mapper = mapper;
        _mediator = mediator;
        _dataHubEntityCache = dataHubEntityCache;
        _serviceProvider = serviceProvider;
    }

    #region Private Helpers

    private async Task ProcessNewEntities(ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralDocument> request, List<TDataHubEntity> creatingEntities, Dictionary<string, SyncEntityResult> syncResults, List<EntityReference> dependencyTree, List<TDataHubEntity> entitiesToUpdate, List<ResolutionPromise> resolutionPromises, CancellationToken cancellationToken)
    {
        #region Update the response to show created entities

        creatingEntities.ForEach(entity => { syncResults[entity.id].SyncOutcome = SyncOutcomes.NewSourceEntityCreated; });

        #endregion

        #region Make sure any other entities referenced by the entities to create exist in Business Central
        // If a referenced entity doesn't exist in Business Central then the referencing entity can't be sync'd or the reference could be lost.  

        var referencedEntities = await SyncReferencedEntities(creatingEntities, syncResults, dependencyTree, resolutionPromises, cancellationToken);
        var entityCache = request.Cache.Merge(referencedEntities.GroupBy(g => g.Value<string>(nameof(DataHubEntity.entityType))!).ToDictionary(k => k.Key, v => (object)v.ToList()));

        #endregion

        #region Convert the entities to create to their Business Central equivalents

        var businessCentralRecordsToCreate = await MapDataHubEntitiesToBusinessCentralEntities(creatingEntities, syncResults, entityCache, cancellationToken);

        #endregion

        cancellationToken.ThrowIfCancellationRequested(); //Throw before beginning DB operations if cancellation or shutdown is requested

        #region Create the sync'd entities in Business Central

        var bulkCreateResponse = (await _mediator.TrySend<CreateBusinessCentralRecordsResponse<TBusinessCentralDocument>>(new CreateBusinessCentralRecordsCommand<TBusinessCentralDocument>()
        {
            RecordsToCreate = businessCentralRecordsToCreate
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        #region Register creation successes or failures for submission later

        RegisterCreateSuccessesAndFailures(creatingEntities, syncResults, bulkCreateResponse);

        #endregion

        #endregion

        var failedEntityCreations = creatingEntities.Where(w => syncResults.Where(result => result.Value.SyncOutcome == SyncOutcomes.SyncFailed).Select(s => s.Key).ToList().Contains(w.id));
        var successfullyCreatedEntities = creatingEntities.Except(failedEntityCreations).ToList();

        #region Register alternate keys with the Data Hub

        await RegisterAlternateKeys(successfullyCreatedEntities, syncResults, cancellationToken, request.CorrelationId);

        #endregion

        #region Refresh the entity cache

        var entityTypeGroups = successfullyCreatedEntities.GroupBy(g => g.entityType);
        foreach (var entityTypeGroup in entityTypeGroups)
        {
            _dataHubEntityCache.InvalidateCacheEntries(entityTypeGroup.Key, entityTypeGroup.Select(s => s.id).ToList());
        }

        #endregion

        #region Immediately merge the newly created Business Central entities to initiate tracking in Business Central

        var successfulCreateResponses = bulkCreateResponse.Results.Where(w => w.Success).ToList();
        if (successfulCreateResponses.Any())
        {
            _ = (await _mediator.TrySend<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralDocument, TDataHubEntity>>(new MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralDocument, TDataHubEntity>()
            {
                EntityIds = successfulCreateResponses.Select(s => s.EntityId.ToString()).ToList()
            }, new CancellationToken())) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
        }

        #endregion

        #region Deal with Resolution Promises

        await HandleResolutionPromises(resolutionPromises, successfullyCreatedEntities, cancellationToken);

        #endregion
    }

    private async Task HandleResolutionPromises(List<ResolutionPromise> resolutionPromises, List<TDataHubEntity> successfullyCreatedEntities, CancellationToken cancellationToken)
    {
        if (resolutionPromises.Any() && successfullyCreatedEntities.Any())
        {
            var resolutionResponse = (await _mediator.TrySend<ResolveResolutionPromisesResponse<TDataHubEntity, TBusinessCentralDocument>>(new ResolveResolutionPromisesRequest<TDataHubEntity, TBusinessCentralDocument>()
            {
                EntitiesToResolve = successfullyCreatedEntities,
                ResolutionPromises = resolutionPromises
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            resolutionPromises = resolutionPromises.Except(resolutionResponse.ResolvedPromises).ToList();
            if (resolutionResponse.UpdatedEntities != null)
            {
                foreach (var resolutionResponseUpdatedEntity in resolutionResponse.UpdatedEntities)
                {
                    var MergeSpecificBusinessCentralEntitiesRequestBaseType = typeof(MergeSpecificBusinessCentralEntitiesRequest<,>);
                    var MergeSpecificBusinessCentralEntitiesRequestType = MergeSpecificBusinessCentralEntitiesRequestBaseType.MakeGenericType(resolutionResponseUpdatedEntity.BusinessCentralType, resolutionResponseUpdatedEntity.DataHubType);
                    var businessCentralDocumentId = new List<string>() { resolutionResponseUpdatedEntity.BusinessCentralEntityId };
                    _dataHubEntityCache.InvalidateCacheEntries(resolutionResponseUpdatedEntity.DataHubType, new List<string>() { resolutionResponseUpdatedEntity.DataHubEntityId });
                    dynamic MergeSpecificBusinessCentralEntitiesRequest = Activator.CreateInstance(MergeSpecificBusinessCentralEntitiesRequestType, businessCentralDocumentId);
                    _ = (await _mediator.SendAsync((IRequest)MergeSpecificBusinessCentralEntitiesRequest, new CancellationToken())) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue };
                }
            }
        }
    }

    private async Task RegisterAlternateKeys(IReadOnlyList<TDataHubEntity> entitiesToRegister, IReadOnlyDictionary<string, SyncEntityResult> resultsDictionary, CancellationToken cancellationToken, string? correlationId)
    {
        if (entitiesToRegister.Any())
        {
            var registrationRequests = entitiesToRegister.Select((dataHubEntity, i) => new RegisterAlternateKeyRequest()
            {
                EntityType = typeof(TDataHubEntity).Name,
                DataHubEntityId = dataHubEntity.id,
                SourceEntityId = resultsDictionary[dataHubEntity.id].SourceEntityId.ToString(),
                Key = $"{_businessCentralAgentConfig.Value.DataSource}.{typeof(TBusinessCentralDocument).Name}".ToLower()
            }).ToList();

            var registerAlternateKeysResponse = await _dataHubClient.PostRequestAsync<RegisterAlternateKeysRequest, RegisterAlternateKeysResponse>(new RegisterAlternateKeysRequest()
            {
                CorrelationId = correlationId,
                Requests = registrationRequests
            }, cancellationToken);

            for (var index = 0; index < entitiesToRegister.Count; index++)
            {
                var failure = registerAlternateKeysResponse.Responses.ElementAtOrDefault(index);
                if (failure?.Success == true) continue;

                var entity = entitiesToRegister[index];
                var syncResult = resultsDictionary[entity.id];
                syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                syncResult.FailureReason = $"Failed to register alternate key: {failure?.FailureReason ?? "Data Hub returned no result."}";
            }
        }
    }

    private static void RegisterCreateSuccessesAndFailures(List<TDataHubEntity> entitiesToCreate, Dictionary<string, SyncEntityResult> syncResults, CreateBusinessCentralRecordsResponse<TBusinessCentralDocument> bulkCreateResponse)
    {
        for (var index = 0; index < entitiesToCreate.Count; index++)
        {
            var result = bulkCreateResponse.Results.ElementAtOrDefault(index);
            var entity = entitiesToCreate[index];
            var syncResult = syncResults[entity.id];
            if (result?.Success != true || string.IsNullOrWhiteSpace(result.EntityId))
            {
                syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                syncResult.FailureReason = result?.Exception?.Message
                    ?? "Business Central returned no valid create result for the record.";
                continue;
            }

            syncResult.SourceEntityId = result.EntityId;
        }
    }

    private async Task<List<TBusinessCentralDocument>> MapDataHubEntitiesToBusinessCentralEntities(List<TDataHubEntity> entitiesToCreate, Dictionary<string, SyncEntityResult> syncResults, Dictionary<string, object> entityCache, CancellationToken cancellationToken)
    {
        var businessCentralRecordsToCreate = new List<TBusinessCentralDocument>();
        var mappingFailures = new List<TDataHubEntity>();
        foreach (var entityToCreate in entitiesToCreate)
        {
            try
            {
                var entityToCreateAsTbusinessCentral = await _mapper.MapAsync<TBusinessCentralDocument>(entityToCreate, cancellationToken, entityCache);
                businessCentralRecordsToCreate.Add(entityToCreateAsTbusinessCentral);
            }
            catch (Exception ex)
            {
                var failedEntityId = entityToCreate.id;
                syncResults[failedEntityId].SyncOutcome = SyncOutcomes.SyncFailed;
                syncResults[failedEntityId].FailureReason = ex.Message;
                mappingFailures.Add(entityToCreate);
            }
        }

        if (mappingFailures.Any())
        {
            entitiesToCreate.RemoveAll(mappingFailures.Contains);
        }

        return businessCentralRecordsToCreate;
    }

    private async Task ProcessUpdatedEntities(ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralDocument> request, Dictionary<string, SyncEntityResult> resultsDictionary, string businessCentralAltKey, List<EntityReference> dependencyTree, List<ResolutionPromise> resolutionPromises, JsonDiffPatch differencer, List<TDataHubEntity> updatingEntities, CancellationToken cancellationToken)
    {
        #region Update the response to show created entities

        updatingEntities.ForEach(entity =>
        {
            var result = resultsDictionary[entity.id];
            result.SyncOutcome = SyncOutcomes.NoSourceEntityUpdateToProcess;
            result.SourceEntityId = entity.alternateKeys
                .First(key => key.Key == businessCentralAltKey)
                .Value;
            result.ResultingDataHubEntity = JObject.FromObject(entity);
        });

        #endregion

        var businessCentralEntityIds = updatingEntities.SelectMany(entities => entities.alternateKeys.Where(ak => ak.Key == businessCentralAltKey).Select(ak => ak.Value)).ToList();

        var trackedBusinessCentralEntities = await RetrieveTrackedBusinessCentralEntitiesFromDataHub(businessCentralEntityIds, cancellationToken);
        var untrackedBusinessCentralEntities = businessCentralEntityIds.Except(trackedBusinessCentralEntities.Select(s => s.id)).ToList();

        #region Retrieve the entities to update from Business Central

        var businessCentralEntities = (await _mediator.TrySend<List<TBusinessCentralDocument>>(new GetSpecificBusinessCentralEntitiesRequest<TBusinessCentralDocument>()
        {
            EntityIds = businessCentralEntityIds
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        #endregion

        #region Pre-sync dependency / referenced entities
        //If a referenced entity doesn't exist in Business Central then the referencing entity can't be sync'd or the reference could be lost.

        var referencedEntities = await SyncReferencedEntities(updatingEntities, resultsDictionary, dependencyTree, resolutionPromises, cancellationToken);
        var entityCache = request.Cache.Merge(referencedEntities.GroupBy(g => g.Value<string>(nameof(DataHubEntity.entityType))!).ToDictionary(k => k.Key, v => (object)v.ToList()));

        #endregion

        #region Merge any Business Central entities that have unmerged updates

        foreach (var trackedBusinessCentralEntity in trackedBusinessCentralEntities)
        {
            var dataHubEntity = updatingEntities.First(f => f.alternateKeys.Any(ak => ak.Key == businessCentralAltKey && ak.Value == trackedBusinessCentralEntity.id));
            resultsDictionary[dataHubEntity.id].SourceEntityType = trackedBusinessCentralEntity.entityType;
            resultsDictionary[dataHubEntity.id].SourceEntityId = trackedBusinessCentralEntity.id;

            try
            {
                var businessCentralEntity = businessCentralEntities.FirstOrDefault(f => f.Id == trackedBusinessCentralEntity!.id);
                if (businessCentralEntity == null)
                {
                    var error = $"Tracked entity {trackedBusinessCentralEntity.id} not found in Business Central";

                    //var dataHubEntity = entitiesToUpdate.FirstOrDefault(f => f.alternateKeys.Any(f => f.Key == businessCentralAltKey && f.Value == trackedBusinessCentralEntityAsTBC.Id.ToString()));
                    //var syncFailure = new SyncFailure()
                    //{
                    //    Timestamp = DateTimeOffset.Now,
                    //    DataSource = _businessCentralAgentConfig.Value.DataSource,
                    //    AgentId = _businessCentralAgentConfig.Value.AgentId,
                    //    DataHubEntityType = dataHubEntity.entityType,
                    //    DataHubEntityId = dataHubEntity.id,
                    //    SourceEntityType = typeof(TBusinessCentralDocument).Name,
                    //    SourceEntityId = trackedBusinessCentralEntityAsTBC.Id,
                    //    Description = error
                    //};

                    throw new Exception(error);
                }

                var businessCentralEntityHasUnmergedUpdates = await CheckIfBusinessCentralEntityHasUnmergedUpdates(trackedBusinessCentralEntity, businessCentralEntity, differencer, untrackedBusinessCentralEntities, cancellationToken);
                if (businessCentralEntityHasUnmergedUpdates)
                {
                    untrackedBusinessCentralEntities.Add(businessCentralEntity.Id!);
                }
            }
            catch (Exception ex)
            {
                var failedEntityId = dataHubEntity.id;
                resultsDictionary[failedEntityId].SyncOutcome = SyncOutcomes.SyncFailed;
                resultsDictionary[failedEntityId].FailureReason = ex.Message;

                updatingEntities.Remove(dataHubEntity);
            }
        }

        if (untrackedBusinessCentralEntities.Any())
        {
            await MergeUpdatedBusinessCentralEntities(request, resultsDictionary, businessCentralAltKey, updatingEntities, untrackedBusinessCentralEntities, cancellationToken);
        }

        #endregion

        var updatedBusinessCentralEntities = new List<TBusinessCentralDocument>();

        foreach (var dataHubEntity in updatingEntities)
        {
            try
            {
                var businessCentralId = dataHubEntity.alternateKeys.First(f => f.Key == businessCentralAltKey).Value;
                var businessCentralEntity = businessCentralEntities.FirstOrDefault(f => f.Id == businessCentralId);

                var businessCentralEntityUpdate = await CalculateBusinessCentralEntityUpdates(dataHubEntity, businessCentralEntity, differencer, entityCache, cancellationToken);
                if (businessCentralEntityUpdate != null) updatedBusinessCentralEntities.Add(businessCentralEntityUpdate);
            }
            catch (Exception ex)
            {
                var syncResult = resultsDictionary[dataHubEntity.id];
                syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                syncResult.FailureReason = ex.Message;
            }
        }

        if (updatedBusinessCentralEntities.Any())
        {
            cancellationToken.ThrowIfCancellationRequested(); //Throw before attempting to send to the Data Hub if cancellation requested

            #region Update the entities in Business Central

            var updateBusinessCentralRecordsResponse = (await _mediator.TrySend<UpdateBusinessCentralRecordsResponse<TBusinessCentralDocument>>(new UpdateBusinessCentralRecordsCommand<TBusinessCentralDocument>()
            {
                Records = updatedBusinessCentralEntities
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            foreach (var updateResult in updateBusinessCentralRecordsResponse.Results)
            {
                var dataHubEntity = updatingEntities.FirstOrDefault(entity => entity.alternateKeys.Any(key =>
                    key.Key == businessCentralAltKey &&
                    key.Value == updateResult.EntityId));

                if (dataHubEntity is null)
                {
                    continue;
                }

                var syncResult = resultsDictionary[dataHubEntity.id];
                if (updateResult.Success)
                {
                    syncResult.SyncOutcome = SyncOutcomes.SourceEntityUpdated;
                    continue;
                }

                syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                syncResult.FailureReason = updateResult.Exception?.InnerException?.Message
                    ?? updateResult.Exception?.Message
                    ?? "Business Central update failed.";
            }

            var concurrencyConflicts = updateBusinessCentralRecordsResponse.Results
                .Where(result => result.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                .ToList();
            if (concurrencyConflicts.Any() && request.ConflictRetryCount == 0)
            {
                var conflictingEntities = updatingEntities
                    .Where(entity => concurrencyConflicts.Any(conflict =>
                        entity.alternateKeys.Any(key => key.Key == businessCentralAltKey && key.Value == conflict.EntityId)))
                    .ToList();

                request.ConflictRetryCount++;
                try
                {
                    await ProcessUpdatedEntities(
                        request,
                        resultsDictionary,
                        businessCentralAltKey,
                        dependencyTree,
                        resolutionPromises,
                        differencer,
                        conflictingEntities,
                        cancellationToken);
                }
                finally
                {
                    request.ConflictRetryCount--;
                }
            }

            #endregion

            #region Merge the updated entities to the Data Hub

            var successfulUpdates = updateBusinessCentralRecordsResponse.Results.Where(w => w.Success).ToList();
            if (successfulUpdates.Any())
            {
                var mergeEntitiesResponse = (await _mediator.TrySend<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralDocument, TDataHubEntity>>(new ProcessBusinessCentralEntityMergeRequest<TBusinessCentralDocument, TDataHubEntity>()
                {
                    BusinessCentralEntities = successfulUpdates.Select(s => s.ResultingEntity).ToList()!,
                    CorrelationId = request.CorrelationId,
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                mergeEntitiesResponse.Results.Where(w => w.MergeOutcome == MergeOutcomes.MergeFailed).ToList().ForEach(mergeEntityResult =>
                {
                    var dataHubEntity = updatingEntities.First(f => f.alternateKeys.Any(f => f.Key == businessCentralAltKey && f.Value == mergeEntityResult.SourceEntityId));
                    resultsDictionary[dataHubEntity.id].SyncOutcome = SyncOutcomes.SyncFailed;
                    resultsDictionary[dataHubEntity.id].FailureReason = $"{SyncFailureTypes.SourceEntityMergeFailed}: {mergeEntityResult.FailureReason}";
                    updatingEntities = updatingEntities.Where(w => w.id != dataHubEntity.id).ToList();
                });

                mergeEntitiesResponse.Results.Where(w => w.MergeOutcome != MergeOutcomes.MergeFailed).ToList().ForEach(mergeEntityResult =>
                {
                    var dataHubEntity = updatingEntities.First(f => f.alternateKeys.Any(f => f.Key == businessCentralAltKey && f.Value == mergeEntityResult.SourceEntityId));
                });
            }

            #endregion
        }
    }

    private async Task<TBusinessCentralDocument?> CalculateBusinessCentralEntityUpdates(TDataHubEntity dataHubEntity, TBusinessCentralDocument businessCentralEntity, JsonDiffPatch differencer, Dictionary<string, object> entityCache, CancellationToken cancellationToken)
    {
        #region Map the DataHub Entity to its Business Central equivalent

        var dataHubEntityAsBC = await _mapper.MapAsync<TDataHubEntity, TBusinessCentralDocument>(dataHubEntity, cancellationToken, entityCache);
        dataHubEntityAsBC.Id = businessCentralEntity.Id;
        BusinessCentralRouteMetadata.EnsureParentRouteValueUnchanged(
            businessCentralEntity,
            dataHubEntityAsBC);

        #endregion

        #region Calculate any changes that need to be made to the Business Central entity to align it to the DataHub entity

        var includeProps = dataHubEntityAsBC.GetAttributes().Keys.ToList();
        var ignoreProps = new List<string>() { };

        var left = JObject.FromObject(businessCentralEntity, new JsonSerializer()
        {
            ContractResolver = new BusinessCentralEntityResolver(includeProps, ignoreProps)
        });

        var right = JObject.FromObject(dataHubEntityAsBC, new JsonSerializer()
        {
            ContractResolver = new BusinessCentralEntityResolver(includeProps, ignoreProps)
        });

        var businessCentralEntityDiffs = differencer.Diff(left, right);

        #endregion

        if (businessCentralEntityDiffs == null) return null;

        #region Create a Business Central entity patch

        var patch = differencer.Patch(new JObject(), businessCentralEntityDiffs) as JObject
            ?? throw new InvalidOperationException(
                $"Business Central update for {typeof(TBusinessCentralDocument).Name} did not produce an object patch.");
        IncludePatchDependencies(dataHubEntityAsBC, right, patch);
        var businessCentralEntityUpdate = patch.ToObject<TBusinessCentralDocument>(new JsonSerializer()
        {
            ContractResolver = new BusinessCentralEntityResolver(includeProps, ignoreProps)
        })!;
        businessCentralEntityUpdate.Id = businessCentralEntity.Id;
        businessCentralEntityUpdate.ETag = businessCentralEntity.ETag;
        BusinessCentralRouteMetadata.CopyParentRouteValue(
            businessCentralEntity,
            businessCentralEntityUpdate);

        #endregion

        return businessCentralEntityUpdate;
    }

    private static void IncludePatchDependencies(
        TBusinessCentralDocument mappedEntity,
        JObject mappedValues,
        JObject patch)
    {
        var mappedAttributeNames = mappedEntity.GetAttributes().Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var changedAttributeNames = patch.Properties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var property in typeof(TBusinessCentralDocument).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var dependency = property.GetCustomAttribute<BusinessCentralPatchDependencyAttribute>(inherit: true);
            if (dependency is null || !dependency.TriggerAttributeNames.Any(changedAttributeNames.Contains))
            {
                continue;
            }

            var attributeName = property.GetCustomAttribute<JsonPropertyAttribute>(inherit: true)?.PropertyName
                ?? property.Name;
            if (!mappedAttributeNames.Contains(attributeName) ||
                !mappedValues.TryGetValue(attributeName, StringComparison.OrdinalIgnoreCase, out var mappedValue))
            {
                continue;
            }

            patch[attributeName] = mappedValue.DeepClone();
        }
    }

    private async Task MergeUpdatedBusinessCentralEntities(ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralDocument> request, Dictionary<string, SyncEntityResult> syncResults, string businessCentralAltKey, List<TDataHubEntity> entitiesToUpdate, List<string> businessCentralEntitiesToMerge, CancellationToken cancellationToken)
    {
        var mergeEntitiesResponse = (await _mediator.TrySend<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralDocument, TDataHubEntity>>(new MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralDocument, TDataHubEntity>()
        {
            CorrelationId = request.CorrelationId,
            EntityIds = businessCentralEntitiesToMerge
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        var mergeFailures = mergeEntitiesResponse.Results.Where(a => a.MergeOutcome == MergeOutcomes.MergeFailed).ToList();
        if (mergeFailures.Any())
        {
            foreach (var mergeEntityResult in mergeFailures)
            {
                var dataHubEntity = entitiesToUpdate.FirstOrDefault(f => f.alternateKeys.Any(f => f.Key == businessCentralAltKey && f.Value == mergeEntityResult.SourceEntityId));
                syncResults[dataHubEntity.id].SyncOutcome = SyncOutcomes.SyncFailed;
                syncResults[dataHubEntity.id].FailureReason = $"{SyncFailureTypes.SourceEntityMergeFailed}: {mergeEntityResult.FailureReason}";
                entitiesToUpdate = entitiesToUpdate.Where(w => w.id != dataHubEntity.id).ToList();
            }
        }

        #region Update the cache with the updated entities

        var entityIdsToRefresh = mergeEntitiesResponse.Results.Where(w => w.MergeOutcome != MergeOutcomes.MergeFailed).Select(s => s.DataHubEntityId).ToList();

        if (entityIdsToRefresh.Any())
        {
            var getUpdatedDataHubEntitiesResponse = await _dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
            {
                EntityType = typeof(TDataHubEntity).Name,
                EntityIds = entityIdsToRefresh
            }, cancellationToken);

            var updatedDataHubEntities = getUpdatedDataHubEntitiesResponse.Results.Select(s => s.ToObjectIgnoreErrors<TDataHubEntity>()).ToList();
            entitiesToUpdate.RemoveAll(w => updatedDataHubEntities.Select(s => s.id).Contains(w.id));
            entitiesToUpdate.AddRange(updatedDataHubEntities);
        }

        #endregion
    }

    private async Task<bool> CheckIfBusinessCentralEntityHasUnmergedUpdates(TDataHubEntity trackedBusinessCentralEntity, TBusinessCentralDocument businessCentralEntity, JsonDiffPatch differencer, List<string> businessCentralEntitiesToMerge, CancellationToken cancellationToken)
    {
        // Compared Data Hub tracked Business Central entity with the actual Business Central entity to see if there are any unmerged updates

        var ignoreProps = new List<string>() { nameof(DataHubEntity.lastUpdated), nameof(DataHubEntity.entityType) };
        var jsonSerializer = new JsonSerializer() { ContractResolver = new BusinessCentralEntityResolver(ignoreProps: ignoreProps) };

        var businessCentralEntityAsDataHub = await _mapper.MapAsync<TBusinessCentralDocument, TDataHubEntity>(businessCentralEntity, cancellationToken);


        var left = JObject.FromObject(trackedBusinessCentralEntity, jsonSerializer);
        var right = JObject.FromObject(businessCentralEntityAsDataHub, jsonSerializer);
        var trackedBusinessCentralEntityDiffs = differencer.Diff(left, right);

        return trackedBusinessCentralEntityDiffs != null;
    }

    private async Task<List<TDataHubEntity>> RetrieveTrackedBusinessCentralEntitiesFromDataHub(List<string> businessCentralEntityIds, CancellationToken cancellationToken)
    {
        var getTrackedEntitiesResponse = await _dataHubClient.PostRequestAsync<GetTrackedEntitiesRequest, GetTrackedEntitiesResponse>(new GetTrackedEntitiesRequest()
        {
            DataSource = _businessCentralAgentConfig.Value.DataSource,
            EntityType = typeof(TBusinessCentralDocument).Name,
            EntityIds = businessCentralEntityIds
        }, cancellationToken);

        var trackedBusinessCentralEntities = getTrackedEntitiesResponse.Results.Where(w => w.Data != null).Select(s => s.Data.ToObjectIgnoreErrors<TDataHubEntity>()).ToList();
        return trackedBusinessCentralEntities;
    }



    private async Task<List<JObject>> SyncReferencedEntities(List<TDataHubEntity> dataHubEntities, Dictionary<string, SyncEntityResult> resultsDictionary, List<EntityReference> dependencyTree, List<ResolutionPromise> resolutionPromises, CancellationToken cancellationToken)
    {
        var typeMap = (IDataHubTypeMapper<TDataHubEntity, TBusinessCentralDocument>)_serviceProvider.GetRequiredService<ITypeMapper<TDataHubEntity, TBusinessCentralDocument>>();

        var ensureReferencedEntitiesAreSyncdResponse = (await _mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TBusinessCentralDocument>>(new EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TBusinessCentralDocument>()
        {
            Entities = dataHubEntities,
            DependencyTree = dependencyTree,
            ResolutionPromises = resolutionPromises,
            TypeMap = typeMap
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        if (ensureReferencedEntitiesAreSyncdResponse.Failures.Any())
        {
            ensureReferencedEntitiesAreSyncdResponse.Failures.ForEach(e =>
            {
                resultsDictionary[e.Entity!.id!].SyncOutcome = SyncOutcomes.SyncFailed;
                resultsDictionary[e.Entity.id].FailureReason = e.Exception.Message;
                dataHubEntities.Remove(dataHubEntities.Find(f => f.id == e.Entity.id)!);

                //var businessCentralId = e.Entity.alternateKeys.FirstOrDefault(f => f.Key == businessCentralAltKey);
                //if (businessCentralId != null)
                //{
                //    var businessCentralEntityToRemoveFromMerge = businessCentralEntitiesToMerge.FirstOrDefault(f => f == businessCentralId.Value);
                //    if (businessCentralEntityToRemoveFromMerge != default) businessCentralEntitiesToMerge.Remove(businessCentralEntityToRemoveFromMerge);
                //}
            });
        }

        var referencedEntities = ensureReferencedEntitiesAreSyncdResponse.CachedEntities;
        resolutionPromises.AddRange(ensureReferencedEntitiesAreSyncdResponse.ResolutionPromises.Except(resolutionPromises));
        return referencedEntities;
    }

    #endregion


    public async Task<ProcessDataHubEntitySyncResponse> HandleAsync(ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralDocument> request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        #region Initialise the response so we can fill it in as tasks are completed

        var resultsDictionary = request.DataHubEntities.ToDictionary(k => k.id, v => new SyncEntityResult()
        {
            DataSource = _businessCentralAgentConfig.Value.DataSource,
            SourceEntityType = typeof(TBusinessCentralDocument).Name,
            DataHubEntityType = typeof(TDataHubEntity).Name,
            DataHubEntityId = v.id
        });

        #endregion

        var businessCentralAltKey = $"{_businessCentralAgentConfig.Value.DataSource}.{typeof(TBusinessCentralDocument).Name}".ToLower();

        var differencer = new JsonDiffPatch(options: new Options()
        {
            TextDiff = TextDiffMode.Simple
        });

        var entitiesToUpdate = request.DataHubEntities.Where(w => w.alternateKeys?.Any(a => a.Key == businessCentralAltKey) ?? false).ToList();
        var entitiesToCreate = request.DataHubEntities.Except(entitiesToUpdate).ToList();
        var dependencyTree = request.DependencyTree?.Concat(entitiesToCreate.Select(s => new EntityReference() { EntityType = typeof(TDataHubEntity).Name, EntityId = s.id })).ToList() ?? new List<EntityReference>();

        var resolutionPromises = request.ResolutionPromises;

        if (entitiesToCreate.Any())
        {
            await ProcessNewEntities(request, entitiesToCreate, resultsDictionary, dependencyTree, entitiesToUpdate, resolutionPromises, cancellationToken);
        }

        if (entitiesToUpdate.Any())
        {
            await ProcessUpdatedEntities(request, resultsDictionary, businessCentralAltKey, dependencyTree, resolutionPromises, differencer, entitiesToUpdate, cancellationToken);
        }

        return new ProcessDataHubEntitySyncResponse()
        {
            Results = resultsDictionary.Values.ToList(),
            ResolutionPromises = resolutionPromises
        };
    }

}
