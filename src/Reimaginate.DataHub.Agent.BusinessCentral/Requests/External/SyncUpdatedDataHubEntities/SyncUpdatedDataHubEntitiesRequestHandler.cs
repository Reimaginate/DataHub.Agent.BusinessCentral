using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RetrieveUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncFailuresToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncSuccessesToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDataHubEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralSyncMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncUpdatedDataHubEntities
{
    public class SyncUpdatedDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity> : IHandler<SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, NullResponse> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
    {
        private readonly IMediator _mediator;
      
        public SyncUpdatedDataHubEntitiesRequestHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Private Helpers

        private async Task ReportSuccessesAndFailures(List<SyncEntityResult> syncEntityResults, CancellationToken cancellationToken)
        {
            var failures = syncEntityResults.Where(w => w.SyncOutcome == SyncOutcomes.SyncFailed).ToList();
            if (failures.Any()) _ = (await _mediator.TrySend<NullResponse>(new SendSyncFailuresToDataHubRequest(failures), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            var successes = syncEntityResults.Where(w => w.SyncOutcome != SyncOutcomes.SyncFailed).ToList();
            if (successes.Any()) _ = (await _mediator.TrySend<NullResponse>(new SendSyncSuccessesToDataHubRequest(successes), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
        }

        #endregion  

        public async Task<NullResponse> HandleAsync(SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity> request, CancellationToken cancellationToken)
        {

            #region Get Sync Markers

            var syncMarkerResponse = (await _mediator.TrySend<GetBusinessCentralSyncMarkerResponse>(new GetBusinessCentralSyncMarkerRequest()
            {
                EntityType = typeof(TDataHubEntity).Name,
                DefaultValue = DateTimeOffset.Now.Date.AddDays(-1).ToString("o")
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            // Keep the boundary inclusive. Data Hub markers currently contain only a timestamp,
            // so a small overlap is required to avoid skipping records that share that timestamp.
            var syncMarkerVal = DateTimeOffset.Parse(syncMarkerResponse.SyncMarker.Value);

            #endregion Get Sync Markers

            #region Process Sync

            var dataHubEntitiesToProcess = new List<TDataHubEntity>();
            string? continuationToken = null;
            bool moreResultsAvailable;

            do
            {
                var page = (await _mediator.TrySend<RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>>(new RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity>()
                {
                    FromDateTime = syncMarkerVal,
                    ContinuationToken = continuationToken,
                    PageSize = Math.Max(1, request.BatchSize)
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                dataHubEntitiesToProcess.AddRange(page.Results);

                moreResultsAvailable = page.MoreResultsAvailable;
                continuationToken = page.ContinuationToken;
                if (moreResultsAvailable && string.IsNullOrWhiteSpace(continuationToken))
                {
                    throw new InvalidOperationException(
                        "Data Hub reported more updated entities but did not return a continuation token. The marker was not advanced.");
                }
            }
            while (moreResultsAvailable);

            // Capture the full ordered-query snapshot before processing any records. A sync can
            // update a Data Hub entity and invalidate Cosmos DB continuation tokens that were
            // issued for the original result set.
            dataHubEntitiesToProcess = dataHubEntitiesToProcess
                .GroupBy(entity => entity.id)
                .Select(group => group.MaxBy(entity => entity.lastUpdated)!)
                .OrderBy(entity => entity.lastUpdated)
                .ToList();

            foreach (var batch in dataHubEntitiesToProcess.Chunk(Math.Max(1, request.BatchSize)))
            {
                var syncResponse = (await _mediator.TrySend<ProcessDataHubEntitySyncResponse>(new SyncDataHubEntitiesWithLocksRequest<TDataHubEntity, TBusinessCentralEntity>()
                {
                    CorrelationId = request.CorrelationId,
                    EntityIds = batch.Select(entity => entity.id).ToList(),
                    ProcessorOverride = request.ProcessorOverride,
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                await ReportSuccessesAndFailures(syncResponse.Results, cancellationToken);
                IncrementalMarkerSafety.EnsureSyncBatchCanAdvance<TDataHubEntity>(syncResponse.Results);
            }

            var newestSuccessfullyProcessed = dataHubEntitiesToProcess
                .Where(entity => entity.lastUpdated.HasValue)
                .MaxBy(entity => entity.lastUpdated)?.lastUpdated;
            if (newestSuccessfullyProcessed.HasValue)
            {
                _ = (await _mediator.TrySend<UpdateBusinessCentralSyncMarkerResponse>(new UpdateBusinessCentralSyncMarkerRequest()
                {
                    Marker = syncMarkerResponse.SyncMarker,
                    NewValue = newestSuccessfullyProcessed.Value.ToString("o")
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
            }

            #endregion

            return new NullResponse();
        }
    }
}
