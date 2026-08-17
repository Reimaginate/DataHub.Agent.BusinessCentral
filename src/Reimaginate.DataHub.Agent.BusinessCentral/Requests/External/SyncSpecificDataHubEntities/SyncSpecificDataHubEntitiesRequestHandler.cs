using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncFailuresToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncSuccessesToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDataHubEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncSpecificDataHubEntities;

public class SyncSpecificDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity> : IHandler<SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
{
    private readonly IMediator _mediator;


    public SyncSpecificDataHubEntitiesRequestHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ProcessDataHubEntitySyncResponse> HandleAsync(SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity> request, CancellationToken cancellationToken)
    {
        request.CorrelationId ??= Guid.NewGuid().ToString();

        var response = (await _mediator.TrySend<ProcessDataHubEntitySyncResponse>(new SyncDataHubEntitiesWithLocksRequest<TDataHubEntity, TBusinessCentralEntity>()
        {
            CorrelationId = request.CorrelationId,
            EntityIds = request.EntityIds,
            ProcessorOverride = request.ProcessorOverride
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        #region Register sync successes and failures with the Data Hub

        var failures = response.Results.Where(w => w.SyncOutcome == SyncOutcomes.SyncFailed).ToList();
        if (failures.Any()) _ = (await _mediator.TrySend<NullResponse>(new SendSyncFailuresToDataHubRequest(failures), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        var successes = response.Results.Where(w => w.SyncOutcome != SyncOutcomes.SyncFailed).ToList();
        if (successes.Any()) _ = (await _mediator.TrySend<NullResponse>(new SendSyncSuccessesToDataHubRequest(successes), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        #endregion

        return response;
    }
}
