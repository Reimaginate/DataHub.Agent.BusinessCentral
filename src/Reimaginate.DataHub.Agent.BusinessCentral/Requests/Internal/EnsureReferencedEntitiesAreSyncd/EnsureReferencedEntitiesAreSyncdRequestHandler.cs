using System.Reflection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.CustomExceptions;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDependencyDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.DataHubEntityCache;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.EnsureReferencedEntitiesAreSyncd;

public class EnsureReferencedEntitiesAreSyncdRequestHandler<TDataHubEntity, TBusinessCentralEntity> : IHandler<EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TBusinessCentralEntity>, EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TBusinessCentralEntity>> where TDataHubEntity : DataHubEntity, new() where TBusinessCentralEntity : BusinessCentralDocument, new()
{
    private readonly IOptions<BusinessCentralAgentOptions> _businessCentralAgentConfig;
    private readonly IDataHubEntityCache _dataHubEntityCache;
    private readonly IMediator _mediator;

    public EnsureReferencedEntitiesAreSyncdRequestHandler(IOptions<BusinessCentralAgentOptions> businessCentralAgentConfig, IDataHubEntityCache dataHubEntityCache, IMediator mediator)
    {
        _businessCentralAgentConfig = businessCentralAgentConfig;
        _dataHubEntityCache = dataHubEntityCache;
        _mediator = mediator;
    }

    private async Task<Dictionary<PropertyInfo, JObject>> ResolveEntityReferencesForEntity(TDataHubEntity entity, List<string>? mappedEntityReferences, CancellationToken cancellationToken)
    {
        var ret = new Dictionary<PropertyInfo, JObject>();

        #region Extract entity references for entity 

        //Get Schema?
        
        var entityReferenceProps = typeof(TDataHubEntity).GetProperties().Where(w => w.PropertyType == typeof(EntityReference)).ToList();
        var entityReferences = entityReferenceProps.Select(prop => new { p = prop, val = (EntityReference)prop.GetValue(entity)! }).ToList();
        var entityReferencesToResolve = entityReferences.Where(w => w.val != null!).ToList();

        if (mappedEntityReferences != null)
        {
            entityReferencesToResolve = entityReferencesToResolve.Where(w=> mappedEntityReferences.Contains(w.p.Name)).ToList();
        }
        
        #endregion

        #region Make sure we don't attempt to sync if there are any unresolved entity references for other source systems

        var unresolvedEntityReferences = entityReferencesToResolve.Where(a => a.val._tag == nameof(ExternalEntityReference)).Select(s => JObject.FromObject(s.val).ToObject<ExternalEntityReference>()).ToList();
        if (unresolvedEntityReferences.Any())
        {
            throw new UnresolvedEntityReferenceException(entity, unresolvedEntityReferences);
        }

        #endregion

        var entityReferenceTypes = entityReferencesToResolve.GroupBy(g => g.val.EntityType);
        foreach (var entityReferenceTypeGroup in entityReferenceTypes)
        {
            #region Retrieve referenced entities from the Data Hub

            var entityIds = entityReferenceTypeGroup.Select(entityReference => entityReference.val.EntityId).ToList();
            var resolvedDataHubEntities = await _dataHubEntityCache.GetDataHubEntities(entityReferenceTypeGroup.Key, entityIds, cancellationToken);

            foreach (var resolvedDataHubEntity in resolvedDataHubEntities)
            {
                var entityId = resolvedDataHubEntity.Value<string>(nameof(DataHubEntity.id));
                var entityReferences2 = entityReferencesToResolve.Where(w => w.val.EntityType == entityReferenceTypeGroup.Key && w.val.EntityId == entityId).ToList();
                entityReferences2.ForEach(er =>
                {
                    ret[er.p] = resolvedDataHubEntity;
                });
            }

            #endregion
        }

        return ret;
    }

    public async Task<EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TBusinessCentralEntity>> HandleAsync(EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TBusinessCentralEntity> request, CancellationToken cancellationToken)
    {
        var resolvedEntityReferences = new List<KeyValuePair<PropertyInfo, JObject>>();
        var resolutionPromises = new List<ResolutionPromise>();
        var failures = new List<ReferenceEntitySyncFailure>();

        foreach (var entity in request.Entities)
        {
            try
            {
                #region Get all referenced entities for entity and preload them from the Data hub

                var resolveEntityReferencesResponse = await ResolveEntityReferencesForEntity(entity, request.TypeMap?.MappedEntityReferences, cancellationToken);
                foreach (var resolvedEntityReference in resolveEntityReferencesResponse)
                {
                    resolvedEntityReferences.Add(resolvedEntityReference);
                }

                #endregion
            }
            catch (Exception ex)
            {
                failures.Add(new ReferenceEntitySyncFailure()
                {
                    Entity = entity,
                    Exception = ex
                });
            }
        }

        var distinctResolvedEntities = resolvedEntityReferences.Select(s => s.Value).DistinctBy(d => new
        {
            EntityType = d.Value<string>(nameof(DataHubEntity.entityType)),
            Id = d.Value<string>(nameof(DataHubEntity.id))
        }).ToList();

        #region Find referenced entities that do not yet exist in Business Central

        var unsyncedReferencedEntities = distinctResolvedEntities
            .Where(w => !w.TryGetSourceSystemAlternateKeys(_businessCentralAgentConfig.Value.DataSource).Any())
            .Distinct()
            .ToList();

        #endregion

        if (unsyncedReferencedEntities.Any())
        {
            var unsyncdReferencedEntitiesByType = unsyncedReferencedEntities.GroupBy(g => g.Value<string>(nameof(DataHubEntity.entityType)));
            foreach (var typeGroup in unsyncdReferencedEntitiesByType)
            {
                #region Find the DataHub and Business Central entity types from the type group and custom attributes attached to the entity definitions

                var entityRefDataHubType = typeof(TDataHubEntity).Assembly.GetType($"{typeof(TDataHubEntity).Namespace}.{typeGroup.Key}", true, true);

                var entityRefBusinessCentralTypeName = entityRefDataHubType!
                    .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true)
                    .Select(s => (RelatedEntityTypeAttribute)s)
                    .FirstOrDefault(f => f.DataSource == _businessCentralAgentConfig.Value.DataSource)?.TypeName;

                var entityRefBusinessCentralType = typeof(TBusinessCentralEntity).Assembly.GetType($"{typeof(TBusinessCentralEntity).Namespace}.{entityRefBusinessCentralTypeName}", true, true);

                #endregion
                
                #region Check if referenced entities would create a circular reference if syncd

                var dataHubEntityIds = typeGroup.Select(s => s.Value<string>(nameof(DataHubEntity.id))).ToList();
                var circularDependencies = request.DependencyTree?.Select(s => s.EntityId).ToList().Intersect(dataHubEntityIds).ToList();

                if (circularDependencies?.Any() ?? false)
                {
                    foreach (var circularDependency in circularDependencies)
                    {
                        var props = resolvedEntityReferences.Where(w => w.Value.Value<string>(nameof(DataHubEntity.id)) == circularDependency).ToDictionary(k => k.Key, v => v.Value);
                        foreach (var entity in request.Entities)
                        {
                            foreach (var prop in props)
                            {
                                prop.Key.SetValue(entity, null);
                                resolutionPromises.Add(new ResolutionPromise()
                                {
                                    DataHubEntityId = entity.id,
                                    DataHubEntityType = entity.entityType,
                                    ExternalEntityReference = new ExternalEntityReference()
                                    {
                                        DataSource = "DataHub",
                                        EntityType = prop.Value.Value<string>(nameof(DataHubEntity.entityType)),
                                        EntityId = prop.Value.Value<string>(nameof(DataHubEntity.id))
                                    },
                                    EntityReferencePath = prop.Key.Name
                                });
                            }
                        }
                    }

                    continue;
                }

                #endregion
                
                #region Dispatch a sync request for the referenced entities

                var syncRequestBaseType = typeof(SyncDependencyDataHubEntitiesRequest<,>);
                var syncRequestType = syncRequestBaseType.MakeGenericType(entityRefDataHubType, entityRefBusinessCentralType!);

                dynamic syncEntityRefsRequest = Activator.CreateInstance(syncRequestType, dataHubEntityIds, request.DependencyTree, request.ResolutionPromises);
                _ = (await _mediator.SendAsync((IRequest)syncEntityRefsRequest, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue };
                resolutionPromises = syncEntityRefsRequest!.ResolutionPromises;

                #endregion

                #region Reload the sync'd entities to the cache to ensure cache has latest details

                _dataHubEntityCache.InvalidateCacheEntries(entityRefDataHubType, dataHubEntityIds);
                var reloadedEntities = await _dataHubEntityCache.GetDataHubEntities(typeGroup.Key, dataHubEntityIds, cancellationToken);

                reloadedEntities.ForEach(e =>
                {
                    distinctResolvedEntities.Remove(distinctResolvedEntities.First(f => f.Value<string>(nameof(DataHubEntity.id)) == e.Value<string>(nameof(DataHubEntity.id))));
                    distinctResolvedEntities.Add(e);
                });

                #endregion
            }
        }

        return new EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TBusinessCentralEntity>()
        {
            CachedEntities = distinctResolvedEntities,
            ResolutionPromises = resolutionPromises,
            Failures = failures
        };
    }
}
