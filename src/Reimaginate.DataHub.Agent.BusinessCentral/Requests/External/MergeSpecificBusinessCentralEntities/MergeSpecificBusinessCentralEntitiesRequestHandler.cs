using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeBusinessCentralEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeFailuresToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeSuccessesToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeSpecificBusinessCentralEntities;

public class MergeSpecificBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity, TDataHubEntity> : IHandler<MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
{
    private readonly IMediator _mediator;


    public MergeSpecificBusinessCentralEntitiesRequestHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> HandleAsync(MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        var response = (await _mediator.TrySend<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>(new MergeBusinessCentralEntitiesWithLocksRequest<TBusinessCentralEntity, TDataHubEntity>()
        {
            CorrelationId = request.CorrelationId,
            BusinessCentralEntityIds = request.EntityIds,
            ForceUpdate = request.ForceUpdate
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        #region Register sync successes and failures with the Data Hub

        var failures = response.Results.Where(w => w.MergeOutcome == MergeOutcomes.MergeFailed).ToList();
        if (failures.Any()) _ = (await _mediator.TrySend<NullResponse>(new SendMergeFailuresToDataHubRequest(failures), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        var successes = response.Results.Where(w => w.MergeOutcome != MergeOutcomes.MergeFailed).ToList();
        if (successes.Any()) _ = (await _mediator.TrySend<NullResponse>(new SendMergeSuccessesToDataHubRequest(successes), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        #endregion

        return response;
    }
}
