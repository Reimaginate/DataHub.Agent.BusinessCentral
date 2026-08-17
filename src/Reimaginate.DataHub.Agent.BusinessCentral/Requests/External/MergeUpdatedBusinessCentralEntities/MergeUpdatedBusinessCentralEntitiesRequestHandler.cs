using System.Reflection;
using Reimaginate.DataHub.Agent.BusinessCentral.Constants;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeBusinessCentralEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeFailuresToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeSuccessesToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeUpdatedBusinessCentralEntities;

public class MergeUpdatedBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity, TDataHubEntity> : IHandler<MergeUpdatedBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, NullResponse> where TBusinessCentralEntity : BusinessCentralDocument, IBusinessCentralIncrementalEntity where TDataHubEntity : DataHubEntity
{
    private readonly IMediator _mediator;
    private readonly IBusinessCentralODataService _businessCentralApiService;

    private const string MergeMarkerDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffK";  //2021-10-08T03:22:37.023Z
    private const string BusinessCentralDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public MergeUpdatedBusinessCentralEntitiesRequestHandler(IBusinessCentralODataService businessCentralApiService, IMediator mediator)
    {
        _mediator = mediator;
        _businessCentralApiService = businessCentralApiService;
    }


    #region Private Helpers

    private async Task ReportSuccessesAndFailures(List<MergeEntityResult> mergeEntityResults, CancellationToken cancellationToken)
    {
        var failures = mergeEntityResults.Where(w => w.MergeOutcome == MergeOutcomes.MergeFailed).ToList();
        if (failures.Any()) _ = (await _mediator.TrySend<NullResponse>(new SendMergeFailuresToDataHubRequest(failures), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        var successes = mergeEntityResults.Where(w => w.MergeOutcome != MergeOutcomes.MergeFailed).ToList();
        if (successes.Any()) _ = (await _mediator.TrySend<NullResponse>(new SendMergeSuccessesToDataHubRequest(successes), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
    }

    #endregion

    public async Task<NullResponse> HandleAsync(MergeUpdatedBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {

        #region Retrieve merge marker from Data Hub and get "from" datetime 

        var mergeMarkerResponse = (await _mediator.TrySend<GetBusinessCentralMergeMarkerResponse>(new GetBusinessCentralMergeMarkerRequest()
        {
            EntityType = typeof(TBusinessCentralEntity).Name,
            DefaultValue = DateTime.Today.AddYears(-1).ToString(Constants.DateFormats.ISO8601)
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        var mergeMarkerVal = DateTimeOffset.Parse(mergeMarkerResponse.MergeMarker.Value);

        if (request.FromDateTime != null)
        {
            mergeMarkerVal = request.FromDateTime.Value;
        }

        #endregion
        
        var lastModifiedProperty = typeof(TBusinessCentralEntity)
            .GetCustomAttribute<BusinessCentralLastModifiedAttribute>(inherit: true)?.PropertyName
            ?? throw new InvalidOperationException(
                $"{typeof(TBusinessCentralEntity).Name} must declare {nameof(BusinessCentralLastModifiedAttribute)} to support incremental merges.");

        var order = $"{lastModifiedProperty},id";
        var filter = $"{lastModifiedProperty} gt {mergeMarkerVal.ToUniversalTime().ToString(BusinessCentralDateTimeFormat)}";
        var select = $"id,{lastModifiedProperty}";
        var getBusinessCentralEntitiesResponse = await _businessCentralApiService.GetEntitiesAsync<TBusinessCentralEntity>(filter, select: select, order: order, cancellationToken: cancellationToken);
        if (getBusinessCentralEntitiesResponse.IsT2) throw getBusinessCentralEntitiesResponse.AsT2;
        if (getBusinessCentralEntitiesResponse.IsT1) throw new InvalidOperationException(
            $"Business Central rejected the incremental {typeof(TBusinessCentralEntity).Name} query with status {(int)getBusinessCentralEntitiesResponse.AsT1.StatusCode} ({getBusinessCentralEntitiesResponse.AsT1.StatusCode}).");
        if (!getBusinessCentralEntitiesResponse.AsT0.Value.Any()) return new NullResponse();

        var entitiesToMerge = getBusinessCentralEntitiesResponse.AsT0.Value;
        DateTimeOffset? newestSuccessfullyProcessed = null;

        while (entitiesToMerge.Any())
        {
            var batch = entitiesToMerge.Take(Math.Max(1, request.BatchSize)).ToList();

            var mergeBusinessCentralEntitiesResponse = (await _mediator.TrySend<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>(new MergeBusinessCentralEntitiesWithLocksRequest<TBusinessCentralEntity, TDataHubEntity>()
            {
                CorrelationId = request.CorrelationId,
                BusinessCentralEntityIds = batch.Select(s => s.Id).ToList()
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            await ReportSuccessesAndFailures(mergeBusinessCentralEntitiesResponse.Results, cancellationToken);

            IncrementalMarkerSafety.EnsureMergeBatchCanAdvance<TBusinessCentralEntity>(
                mergeBusinessCentralEntitiesResponse.Results);

            if (batch.Any(entity => !entity.LastModifiedAt.HasValue))
            {
                throw new InvalidOperationException(
                    $"Business Central returned {typeof(TBusinessCentralEntity).Name} records without the configured '{lastModifiedProperty}' value.");
            }

            newestSuccessfullyProcessed = batch.Max(entity => entity.LastModifiedAt!.Value);

            entitiesToMerge.RemoveRange(0, batch.Count);
        }

        if (newestSuccessfullyProcessed.HasValue)
        {
            _ = (await _mediator.TrySend<UpdateBusinessCentralMergeMarkerResponse>(new UpdateBusinessCentralMergeMarkerRequest()
            {
                Marker = mergeMarkerResponse.MergeMarker,
                // The marker format has millisecond precision and does not contain an id tie-breaker.
                // Retain a one-millisecond overlap so a record committed at the boundary after the
                // query cannot be skipped. Re-merges are idempotent and safer than data loss.
                NewValue = newestSuccessfullyProcessed.Value.AddMilliseconds(-1).ToString(MergeMarkerDateTimeFormat)
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
        }

        return new NullResponse();
    }
}
