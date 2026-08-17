using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public enum BusinessCentralPurchaseInvoiceCleanupDisposition
{
    NotFound,
    DraftDeleted,
    NoSeriesPlaceholderDeleted
}

public sealed record BusinessCentralPurchaseInvoiceLineSnapshot(
    int? ExpectedCount,
    IReadOnlyList<BusinessCentralPurchaseInvoiceLine> Lines);

public sealed record BusinessCentralPurchaseInvoiceCleanupResult(
    BusinessCentralPurchaseInvoiceCleanupDisposition Disposition,
    IReadOnlyList<BusinessCentralPurchaseInvoiceLine> CapturedLines);

public static class BusinessCentralPurchaseInvoiceCleanupFlow
{
    public static async Task<BusinessCentralPurchaseInvoiceCleanupResult> DeleteCurrentAsync(
        Guid invoiceId,
        Func<Guid, CancellationToken, Task<BusinessCentralPurchaseInvoice?>> readInvoice,
        Func<Guid, CancellationToken, Task<BusinessCentralPurchaseInvoiceLineSnapshot>> readLines,
        Func<BusinessCentralPurchaseInvoice, CancellationToken, Task> deleteInvoice,
        bool allowDraftDelete = true,
        CancellationToken cancellationToken = default)
    {
        var invoice = await readInvoice(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return new BusinessCentralPurchaseInvoiceCleanupResult(
                BusinessCentralPurchaseInvoiceCleanupDisposition.NotFound,
                []);
        }

        if (!Guid.TryParse(invoice.Id, out var currentInvoiceId) || currentInvoiceId != invoiceId)
        {
            throw new InvalidOperationException(
                $"Business Central returned purchase invoice '{invoice.Id}' while cleanup requested '{invoiceId}'.");
        }

        if (string.IsNullOrWhiteSpace(invoice.ETag))
        {
            throw new InvalidOperationException(
                $"Purchase invoice {invoiceId} has no ETag. Cleanup will not use a wildcard If-Match value.");
        }

        if (invoice.VendorInvoiceNumber?.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new InvalidOperationException(
                $"Purchase invoice {invoiceId} is not a DHIT test artifact. Cleanup refused to delete it.");
        }

        if (string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            if (!allowDraftDelete)
            {
                throw new InvalidOperationException(
                    $"Purchase invoice {invoiceId} remained Draft after Business Central acknowledged deletion. " +
                    "Cleanup will not retry the draft DELETE.");
            }

            await deleteInvoice(invoice, cancellationToken);
            return new BusinessCentralPurchaseInvoiceCleanupResult(
                BusinessCentralPurchaseInvoiceCleanupDisposition.DraftDeleted,
                []);
        }

        if (!string.Equals(invoice.Status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Purchase invoice {invoiceId} has cleanup-unsafe status " +
                $"'{invoice.Status ?? "<missing>"}'. Only Draft or the exact Paid no-series placeholder is allowed.");
        }

        var lineSnapshot = await readLines(invoiceId, cancellationToken);
        if (!lineSnapshot.ExpectedCount.HasValue ||
            lineSnapshot.ExpectedCount.Value != lineSnapshot.Lines.Count)
        {
            throw new InvalidOperationException(
                $"Purchase invoice {invoiceId} returned an incomplete line set " +
                $"({lineSnapshot.Lines.Count} loaded, " +
                $"{lineSnapshot.ExpectedCount?.ToString() ?? "unknown"} reported). Cleanup refused to delete it.");
        }

        if (!BusinessCentralPurchaseInvoiceCleanupPolicy.IsSafeNoSeriesPlaceholder(
                invoice,
                lineSnapshot.Lines))
        {
            throw new InvalidOperationException(
                $"Paid purchase invoice {invoiceId} is not the exact zero-value Business Central " +
                "no-series placeholder. Cleanup refused to delete it.");
        }

        await deleteInvoice(invoice, cancellationToken);
        return new BusinessCentralPurchaseInvoiceCleanupResult(
            BusinessCentralPurchaseInvoiceCleanupDisposition.NoSeriesPlaceholderDeleted,
            lineSnapshot.Lines);
    }
}
