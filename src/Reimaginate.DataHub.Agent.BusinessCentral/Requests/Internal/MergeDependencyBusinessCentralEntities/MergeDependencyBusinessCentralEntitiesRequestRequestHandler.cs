using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeBusinessCentralEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeDependencyBusinessCentralEntities;

public class MergeDependencyBusinessCentralEntitiesRequestRequestHandler<TBusinessCentralEntity, TDataHubEntity> : IHandler<MergeDependencyBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
{
    private readonly IMediator _mediator;
 
    public MergeDependencyBusinessCentralEntitiesRequestRequestHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> HandleAsync(MergeDependencyBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        var response = (await _mediator.TrySend<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>(new MergeBusinessCentralEntitiesWithLocksRequest<TBusinessCentralEntity, TDataHubEntity>()
        {
            CorrelationId = request.CorrelationId,
            BusinessCentralEntityIds = request.EntityIds,
            DependencyTree = request.DependencyTree
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        return response;
    }
}
