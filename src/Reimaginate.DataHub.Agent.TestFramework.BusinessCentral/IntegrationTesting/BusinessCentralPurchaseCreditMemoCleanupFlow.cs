using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemoLine;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public enum BusinessCentralPurchaseCreditMemoCleanupDisposition
{
    NotFound,
    DraftDeleted,
    NoSeriesPlaceholderDeleted
}

public sealed record BusinessCentralPurchaseCreditMemoLineSnapshot(
    int? ExpectedCount,
    IReadOnlyList<BusinessCentralPurchaseCreditMemoLine> Lines);

public sealed record BusinessCentralPurchaseCreditMemoCleanupResult(
    BusinessCentralPurchaseCreditMemoCleanupDisposition Disposition,
    IReadOnlyList<BusinessCentralPurchaseCreditMemoLine> CapturedLines);

public static class BusinessCentralPurchaseCreditMemoCleanupFlow
{
    public static async Task<BusinessCentralPurchaseCreditMemoCleanupResult> DeleteCurrentAsync(
        Guid creditMemoId,
        Func<Guid, CancellationToken, Task<BusinessCentralPurchaseCreditMemo?>> readCreditMemo,
        Func<Guid, CancellationToken, Task<BusinessCentralPurchaseCreditMemoLineSnapshot>> readLines,
        Func<BusinessCentralPurchaseCreditMemo, CancellationToken, Task> deleteCreditMemo,
        bool allowDraftDelete = true,
        CancellationToken cancellationToken = default)
    {
        var creditMemo = await readCreditMemo(creditMemoId, cancellationToken);
        if (creditMemo is null)
        {
            return new BusinessCentralPurchaseCreditMemoCleanupResult(
                BusinessCentralPurchaseCreditMemoCleanupDisposition.NotFound,
                []);
        }

        if (!Guid.TryParse(creditMemo.Id, out var currentId) || currentId != creditMemoId)
        {
            throw new InvalidOperationException(
                $"Business Central returned purchase credit memo '{creditMemo.Id}' while cleanup requested '{creditMemoId}'.");
        }

        if (string.IsNullOrWhiteSpace(creditMemo.ETag))
        {
            throw new InvalidOperationException(
                $"Purchase credit memo {creditMemoId} has no ETag. Cleanup will not use a wildcard If-Match value.");
        }

        if (creditMemo.VendorCreditMemoNumber?.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new InvalidOperationException(
                $"Purchase credit memo {creditMemoId} is not a DHIT test artifact. Cleanup refused to delete it.");
        }

        if (string.Equals(creditMemo.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            if (!allowDraftDelete)
            {
                throw new InvalidOperationException(
                    $"Purchase credit memo {creditMemoId} remained Draft after Business Central acknowledged deletion. Cleanup will not retry it.");
            }

            await deleteCreditMemo(creditMemo, cancellationToken);
            return new BusinessCentralPurchaseCreditMemoCleanupResult(
                BusinessCentralPurchaseCreditMemoCleanupDisposition.DraftDeleted,
                []);
        }

        if (!string.Equals(creditMemo.Status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Purchase credit memo {creditMemoId} has cleanup-unsafe status '{creditMemo.Status ?? "<missing>"}'.");
        }

        var lineSnapshot = await readLines(creditMemoId, cancellationToken);
        if (!lineSnapshot.ExpectedCount.HasValue ||
            lineSnapshot.ExpectedCount.Value != lineSnapshot.Lines.Count)
        {
            throw new InvalidOperationException(
                $"Purchase credit memo {creditMemoId} returned an incomplete line set. Cleanup refused to delete it.");
        }

        if (!BusinessCentralPurchaseCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(
                creditMemo,
                lineSnapshot.Lines))
        {
            throw new InvalidOperationException(
                $"Paid purchase credit memo {creditMemoId} is not the exact zero-value no-series placeholder.");
        }

        await deleteCreditMemo(creditMemo, cancellationToken);
        return new BusinessCentralPurchaseCreditMemoCleanupResult(
            BusinessCentralPurchaseCreditMemoCleanupDisposition.NoSeriesPlaceholderDeleted,
            lineSnapshot.Lines);
    }
}
