using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Queries.GetSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using Reimaginate.ProcessingLockService.Abstractions;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeBusinessCentralEntitiesWithLocks;

public class MergeBusinessCentralEntitiesWithLocksRequestHandler<TBusinessCentralEntity, TDataHubEntity> : IHandler<MergeBusinessCentralEntitiesWithLocksRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
{
    private readonly IOptions<BusinessCentralAgentOptions> _businessCentralAgentConfig;
    private readonly IMediator _mediator;
    private readonly IProcessingLockService _processingLockService;

    public MergeBusinessCentralEntitiesWithLocksRequestHandler(IOptions<BusinessCentralAgentOptions> businessCentralAgentConfig, IMediator mediator, IProcessingLockService processingLockService)
    {
        _businessCentralAgentConfig = businessCentralAgentConfig;
        _mediator = mediator;
        _processingLockService = processingLockService;
    }

    public async Task<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> HandleAsync(MergeBusinessCentralEntitiesWithLocksRequest<TBusinessCentralEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        if (!request.BusinessCentralEntityIds.Any()) return new ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>();

        List<ProcessingLock> businessCentralEntityLocks = null;

        try
        {
            var entityIds = request.BusinessCentralEntityIds.Distinct().ToList();
            
            var getLocksResponse = await _processingLockService.WaitForLocksAsync(entityIds.Select(s => $"{_businessCentralAgentConfig.Value.DataSource}.{typeof(TBusinessCentralEntity).Name}.{s}").ToList(), request.CorrelationId, cancellationToken);
            getLocksResponse.ThrowIfUnsuccessful();
            businessCentralEntityLocks = getLocksResponse.Result;

            var businessCentralEntities = (await _mediator.TrySend<List<TBusinessCentralEntity>>(new GetSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity>() { EntityIds = entityIds }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            if (request.ForceUpdate)
            {
                if (businessCentralEntities.Any(entity => entity is not IBusinessCentralIncrementalEntity))
                {
                    throw new InvalidOperationException(
                        $"ForceUpdate requires {typeof(TBusinessCentralEntity).Name} to implement {nameof(IBusinessCentralIncrementalEntity)}.");
                }

                foreach (var entity in businessCentralEntities.Cast<IBusinessCentralIncrementalEntity>())
                {
                    entity.LastModifiedAt = DateTimeOffset.UtcNow;
                }
            }

            var mergeBusinessCentralEntitiesResponse = (await _mediator.TrySend<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>(new ProcessBusinessCentralEntityMergeRequest<TBusinessCentralEntity, TDataHubEntity>()
            {
                CorrelationId = request.CorrelationId,
                BusinessCentralEntities = businessCentralEntities,
                DependencyTree = request.DependencyTree
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            return mergeBusinessCentralEntitiesResponse;
        }
        finally
        {
            if (businessCentralEntityLocks != null) await _processingLockService.ReleaseLocksAsync(businessCentralEntityLocks, cancellationToken);
        }
    }
}
