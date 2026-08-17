using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemoLine;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public enum BusinessCentralSalesCreditMemoCleanupDisposition
{
    NotFound,
    DraftDeleted,
    NoSeriesPlaceholderDeleted
}

public sealed record BusinessCentralSalesCreditMemoLineSnapshot(
    int? ExpectedCount,
    IReadOnlyList<BusinessCentralSalesCreditMemoLine> Lines);

public sealed record BusinessCentralSalesCreditMemoCleanupResult(
    BusinessCentralSalesCreditMemoCleanupDisposition Disposition,
    IReadOnlyList<BusinessCentralSalesCreditMemoLine> CapturedLines,
    BusinessCentralSalesCreditMemoCleanupProvenance? TransitionProvenance = null);

public sealed class BusinessCentralSalesCreditMemoCleanupProvenance
{
    private readonly Guid _creditMemoId;
    private readonly string? _externalDocumentNumber;
    private int _consumed;

    internal BusinessCentralSalesCreditMemoCleanupProvenance(
        Guid creditMemoId,
        string? externalDocumentNumber,
        bool allowsBlankExternalDocumentNumber)
    {
        _creditMemoId = creditMemoId;
        _externalDocumentNumber = externalDocumentNumber;
        AllowsBlankExternalDocumentNumber = allowsBlankExternalDocumentNumber;
    }

    internal bool AllowsBlankExternalDocumentNumber { get; }

    internal bool Matches(Guid creditMemoId, string? externalDocumentNumber) =>
        _creditMemoId == creditMemoId &&
        (AllowsBlankExternalDocumentNumber
            ? string.IsNullOrEmpty(externalDocumentNumber)
            : string.Equals(
                _externalDocumentNumber,
                externalDocumentNumber,
                StringComparison.Ordinal));

    internal bool TryConsume() => Interlocked.CompareExchange(ref _consumed, 1, 0) == 0;
}

public static class BusinessCentralSalesCreditMemoCleanupFlow
{
    public static async Task<BusinessCentralSalesCreditMemoCleanupResult> DeleteCurrentAsync(
        Guid creditMemoId,
        Func<Guid, CancellationToken, Task<BusinessCentralSalesCreditMemo?>> readCreditMemo,
        Func<Guid, CancellationToken, Task<BusinessCentralSalesCreditMemoLineSnapshot>> readLines,
        Func<BusinessCentralSalesCreditMemo, CancellationToken, Task> deleteCreditMemo,
        bool allowDraftDelete = true,
        string? expectedExternalDocumentNumber = null,
        bool allowCapturedBlankDraft = false,
        BusinessCentralSalesCreditMemoCleanupProvenance? transitionProvenance = null,
        CancellationToken cancellationToken = default)
    {
        var creditMemo = await readCreditMemo(creditMemoId, cancellationToken);
        if (creditMemo is null)
        {
            return new BusinessCentralSalesCreditMemoCleanupResult(
                BusinessCentralSalesCreditMemoCleanupDisposition.NotFound,
                []);
        }

        if (!Guid.TryParse(creditMemo.Id, out var currentCreditMemoId) ||
            currentCreditMemoId != creditMemoId)
        {
            throw new InvalidOperationException(
                $"Business Central returned sales credit memo '{creditMemo.Id}' while cleanup requested " +
                $"'{creditMemoId}'.");
        }

        if (string.IsNullOrWhiteSpace(creditMemo.ETag) ||
            string.Equals(creditMemo.ETag.Trim(), "*", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Sales credit memo {creditMemoId} has no exact ETag. Cleanup will not use a wildcard " +
                "If-Match value.");
        }

        if (string.Equals(creditMemo.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            if (!allowDraftDelete)
            {
                throw new InvalidOperationException(
                    $"Sales credit memo {creditMemoId} remained Draft after Business Central acknowledged " +
                    "deletion. Cleanup will not retry the draft DELETE.");
            }

            var hasDhitExternalDocumentNumber =
                creditMemo.ExternalDocumentNumber?.StartsWith(
                    "DHIT-",
                    StringComparison.OrdinalIgnoreCase) == true;
            var hasAuthorizedBlankExternalDocumentNumber =
                allowCapturedBlankDraft && string.IsNullOrEmpty(creditMemo.ExternalDocumentNumber);
            if (!hasDhitExternalDocumentNumber && !hasAuthorizedBlankExternalDocumentNumber)
            {
                throw new InvalidOperationException(
                    $"Sales credit memo {creditMemoId} is not a DHIT test artifact, and no captured-blank " +
                    "draft provenance authorized it. Cleanup refused to delete it.");
            }

            if (expectedExternalDocumentNumber is not null &&
                !string.Equals(
                    creditMemo.ExternalDocumentNumber,
                    expectedExternalDocumentNumber,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Sales credit memo {creditMemoId} no longer has the exact expected external document " +
                    $"number '{expectedExternalDocumentNumber}'. Cleanup refused to delete it.");
            }

            await deleteCreditMemo(creditMemo, cancellationToken);
            return new BusinessCentralSalesCreditMemoCleanupResult(
                BusinessCentralSalesCreditMemoCleanupDisposition.DraftDeleted,
                [],
                new BusinessCentralSalesCreditMemoCleanupProvenance(
                    creditMemoId,
                    creditMemo.ExternalDocumentNumber,
                    hasAuthorizedBlankExternalDocumentNumber));
        }

        if (!string.Equals(creditMemo.Status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Sales credit memo {creditMemoId} has cleanup-unsafe status " +
                $"'{creditMemo.Status ?? "<missing>"}'. Only Draft or the exact Paid no-series placeholder " +
                "is allowed.");
        }

        if (allowDraftDelete)
        {
            throw new InvalidOperationException(
                $"Sales credit memo {creditMemoId} was already Paid when normal cleanup began. Cleanup will " +
                "delete a Paid no-series placeholder only in an explicit guarded placeholder phase.");
        }

        var hasBlankExternalDocumentNumber = string.IsNullOrEmpty(creditMemo.ExternalDocumentNumber);
        if (transitionProvenance is not null)
        {
            if (!transitionProvenance.Matches(creditMemoId, creditMemo.ExternalDocumentNumber))
            {
                throw new InvalidOperationException(
                    $"Sales credit memo {creditMemoId} does not match the one-shot Draft-delete provenance. " +
                    "Cleanup refused to delete it.");
            }
        }
        else
        {
            if (hasBlankExternalDocumentNumber)
            {
                throw new InvalidOperationException(
                    $"Paid sales credit memo {creditMemoId} has a blank external document number and no " +
                    "same-operation Draft-delete provenance. Cleanup refused to delete it.");
            }

            if (string.IsNullOrWhiteSpace(expectedExternalDocumentNumber) ||
                !string.Equals(
                    creditMemo.ExternalDocumentNumber,
                    expectedExternalDocumentNumber,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Paid sales credit memo {creditMemoId} requires its exact DHIT external document number " +
                    "for explicit placeholder cleanup.");
            }
        }

        if (!hasBlankExternalDocumentNumber &&
            creditMemo.ExternalDocumentNumber?.StartsWith(
                "DHIT-",
                StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new InvalidOperationException(
                $"Sales credit memo {creditMemoId} is not a DHIT test artifact. Cleanup refused to delete it.");
        }

        var lineSnapshot = await readLines(creditMemoId, cancellationToken);
        if (!lineSnapshot.ExpectedCount.HasValue ||
            lineSnapshot.ExpectedCount.Value != lineSnapshot.Lines.Count)
        {
            throw new InvalidOperationException(
                $"Sales credit memo {creditMemoId} returned an incomplete line set " +
                $"({lineSnapshot.Lines.Count} loaded, " +
                $"{lineSnapshot.ExpectedCount?.ToString() ?? "unknown"} reported). Cleanup refused to delete it.");
        }

        if (!BusinessCentralSalesCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(
                creditMemo,
                lineSnapshot.Lines,
                allowBlankExternalDocumentNumber:
                    transitionProvenance?.AllowsBlankExternalDocumentNumber == true))
        {
            throw new InvalidOperationException(
                $"Paid sales credit memo {creditMemoId} is not the exact zero-value Business Central " +
                "no-series placeholder. Cleanup refused to delete it.");
        }

        if (transitionProvenance is not null && !transitionProvenance.TryConsume())
        {
            throw new InvalidOperationException(
                $"The one-shot Draft-delete provenance for sales credit memo {creditMemoId} was already used. " +
                "Cleanup refused to retry the placeholder DELETE.");
        }

        await deleteCreditMemo(creditMemo, cancellationToken);
        return new BusinessCentralSalesCreditMemoCleanupResult(
            BusinessCentralSalesCreditMemoCleanupDisposition.NoSeriesPlaceholderDeleted,
            lineSnapshot.Lines);
    }
}
