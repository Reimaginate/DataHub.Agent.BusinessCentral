using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Helpers;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeDependencyBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;

public class ProcessBusinessCentralEntityMergeRequestHandler<TBusinessCentralEntity, TDataHubEntity> : IHandler<ProcessBusinessCentralEntityMergeRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
{
    private readonly IOptions<BusinessCentralAgentOptions> _config;
    private readonly IDataHubClient _dataHubClient;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ProcessBusinessCentralEntityMergeRequestHandler(IOptions<BusinessCentralAgentOptions> config, IDataHubClient dataHubClient, IMediator mediator, IMapper mapper)
    {
        _config = config;
        _dataHubClient = dataHubClient;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> HandleAsync(ProcessBusinessCentralEntityMergeRequest<TBusinessCentralEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        #region Transform BusinessCentral entities to their Data Hub equivalents

        var businessCentralEntitiesAsTDataHub = await _mapper.MapAsync<List<TBusinessCentralEntity>, List<TDataHubEntity>>(request.BusinessCentralEntities, cancellationToken);

        #endregion

        var externalEntityReferences = businessCentralEntitiesAsTDataHub.Select(entity => JObject.FromObject(entity).Descendants().OfType<JObject>()
            .Where(w => w.ContainsKey("@Tag") && w.Value<string>("@Tag") == nameof(ExternalEntityReference))
            .ToList()).SelectMany(entityRefs => entityRefs.Select(entityRef => entityRef.ToObjectIgnoreErrors<ExternalEntityReference>())).DistinctBy(d => $"{d.DataSource}_{d.EntityType}_{d.SourceEntityType}_{d.EntityId}").ToList();

        List<ResolvedEntityReference> resolvedEntityRefs = null;

        if (externalEntityReferences.Any())
        {
            resolvedEntityRefs = _dataHubClient.PostRequestAsync<ResolveEntityReferencesRequest, ResolveEntityReferencesResponse>(new ResolveEntityReferencesRequest()
            {
                EntityReferences = externalEntityReferences
            }, cancellationToken).Result.Results;

            var sharedEntityRefs = externalEntityReferences.Join(resolvedEntityRefs, a => a.EntityId, b => b.SourceEntityReference.EntityId, (a, b) => a).ToList();
            var missingEntityRefs = externalEntityReferences.Except(sharedEntityRefs).ToList();

            if (missingEntityRefs.Any())
            {
                var typeGroups = missingEntityRefs.GroupBy(g => new { g.SourceEntityType, g.EntityType });

                foreach (var typeGroup in typeGroups)
                {
                    var businessCentralEntityIds = missingEntityRefs.Select(s => s.EntityId).ToList();
                
                    if (!request.DependencyTree.Select(s => s.EntityId).ToList().Intersect(businessCentralEntityIds).Any())
                    {
                        var entityRefBusinessCentralType = typeof(TBusinessCentralEntity).Assembly.GetType($"{typeof(TBusinessCentralEntity).Namespace}.{typeGroup.Key.SourceEntityType}".ToLower(), true, true);
                        var entityRefDataHubType = typeof(TDataHubEntity).Assembly.GetType($"{typeof(TDataHubEntity).Namespace}.{typeGroup.Key.EntityType}".ToLower(), true, true);

                        var mergeDependencyBusinessCentralEntitiesRequestType = typeof(MergeDependencyBusinessCentralEntitiesRequest<,>);
                        var mergeDependencyBusinessCentralEntitiesRequestTypeGeneric = mergeDependencyBusinessCentralEntitiesRequestType.MakeGenericType(entityRefBusinessCentralType!, entityRefDataHubType!);

                        var dependencyTree = request.DependencyTree.Concat(missingEntityRefs).ToList();
                        dynamic mergeSubRequest = Activator.CreateInstance(mergeDependencyBusinessCentralEntitiesRequestTypeGeneric, businessCentralEntityIds, dependencyTree, request.CorrelationId);

                        try
                        {
                            var mergeSubResponse = (dynamic)((await _mediator.SendAsync((IRequest)mergeSubRequest, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue });
                            var results = (List<MergeEntityResult>)mergeSubResponse.Results;
                            var successes = results.Where(w=>w.MergeOutcome != MergeOutcomes.MergeFailed).ToList();
                            var failures = results.Except(successes);

                            resolvedEntityRefs.AddRange(successes.Select(s => new ResolvedEntityReference()
                            {
                                DataHubEntityReference = new EntityReference()
                                {
                                    EntityId = s.DataHubEntityId,
                                    EntityType = s.DataHubEntityType
                                },
                                SourceEntityReference = new ExternalEntityReference()
                                {
                                    DataSource = s.DataSource,
                                    SourceEntityType = s.SourceEntityType,
                                    EntityType = s.SourceEntityType,
                                    EntityId = s.SourceEntityId,
                                }
                            }));
                        }
                        catch (Exception)
                        {
                            if (!request.DependencyTree.Any())
                            {
                                //Ignore as we do not have a circular dependency
                            }
                            else
                                throw;
                        }
                    }
                }
            }
        }

        var mergeRequest = new MergeEntitiesRequest()
        {
            DataSource = _config.Value.DataSource,
            Requests = businessCentralEntitiesAsTDataHub.Select(s =>
            {
                var dataHubEntityJObject = JObject.FromObject(s);
                var entityRefs = dataHubEntityJObject.Descendants().OfType<JObject>().Where(w => w.ContainsKey("@Tag") && w.Value<string>("@Tag") == nameof(ExternalEntityReference)).ToList();

                foreach (var entityRef in entityRefs)
                {
                    var resolvedEntity = resolvedEntityRefs?.FirstOrDefault(f => f.SourceEntityReference.EntityId == entityRef.Value<string>(nameof(ExternalEntityReference.EntityId)));
                    if (resolvedEntity != null)
                    {
                        entityRef.Replace(JObject.FromObject(new EntityReference()
                        {
                            EntityType = resolvedEntity.DataHubEntityReference.EntityType,
                            EntityId = resolvedEntity.DataHubEntityReference.EntityId
                        }));
                    }
                }

                return new MergeEntityRequest()
                {
                    DataSource = _config.Value.DataSource,
                    DataHubEntityType = typeof(TDataHubEntity).Name,
                    SourceEntityType = typeof(TBusinessCentralEntity).Name,
                    SourceEntityId = s.id,
                    Data = dataHubEntityJObject
                };
            }).ToList()
        };
        
        var response = await _dataHubClient.PostRequestAsync<MergeEntitiesRequest, MergeEntitiesResponse>(mergeRequest, cancellationToken);

        return new ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>()
        {
            Results = response.Results
        };
    }
}
