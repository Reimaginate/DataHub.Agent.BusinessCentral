using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal;

/// <summary>
/// Central guard that must run before an incremental marker update. The agent has
/// no durable automatic retry queue, so advancing past a failed record is unsafe.
/// </summary>
public static class IncrementalMarkerSafety
{
    public static void EnsureMergeBatchCanAdvance<TBusinessCentralEntity>(
        IEnumerable<MergeEntityResult> results)
    {
        var failures = results.Where(result => result.MergeOutcome == MergeOutcomes.MergeFailed).ToList();
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"The incremental Business Central {typeof(TBusinessCentralEntity).Name} merge contained failures. " +
                $"Failed records: {DescribeMergeFailures(failures)}. " +
                "The marker was not advanced so the records can be retried safely.");
        }
    }

    public static void EnsureSyncBatchCanAdvance<TDataHubEntity>(
        IEnumerable<SyncEntityResult> results)
    {
        var failures = results.Where(result => result.SyncOutcome == SyncOutcomes.SyncFailed).ToList();
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"The incremental Data Hub {typeof(TDataHubEntity).Name} sync contained failures. " +
                $"Failed records: {DescribeSyncFailures(failures)}. " +
                "The marker was not advanced so the records can be retried safely.");
        }
    }

    private static string DescribeMergeFailures(IEnumerable<MergeEntityResult> failures) =>
        string.Join("; ", failures.Select(failure =>
            $"source={ValueOrUnknown(failure.SourceEntityId)}, dataHub={ValueOrUnknown(failure.DataHubEntityId)}, " +
            $"reason={ValueOrUnknown(failure.FailureReason)}"));

    private static string DescribeSyncFailures(IEnumerable<SyncEntityResult> failures) =>
        string.Join("; ", failures.Select(failure =>
            $"dataHub={ValueOrUnknown(failure.DataHubEntityId)}, source={ValueOrUnknown(failure.SourceEntityId)}, " +
            $"reason={ValueOrUnknown(failure.FailureReason)}"));

    private static string ValueOrUnknown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "<unknown>" : value;
}
