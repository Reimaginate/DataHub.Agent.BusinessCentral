using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public static class BusinessCentralPurchaseInvoiceCleanupPolicy
{
    public const string NoSeriesPlaceholderDescription =
        "Document created to avoid gap in No. Series";

    public static bool IsSafeNoSeriesPlaceholder(
        BusinessCentralPurchaseInvoice invoice,
        IReadOnlyCollection<BusinessCentralPurchaseInvoiceLine> lines)
    {
        if (!Guid.TryParse(invoice.Id, out var invoiceId) ||
            string.IsNullOrWhiteSpace(invoice.ETag) ||
            !string.Equals(invoice.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
            invoice.VendorInvoiceNumber?.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase) != true ||
            invoice.DiscountAmount != 0m ||
            invoice.TotalAmountExcludingTax != 0m ||
            invoice.TotalTaxAmount != 0m ||
            invoice.TotalAmountIncludingTax != 0m ||
            lines.Count != 1)
        {
            return false;
        }

        return lines.All(line =>
            Guid.TryParse(line.Id, out _) &&
            line.DocumentId == invoiceId &&
            string.Equals(line.LineType, "Comment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                line.Description,
                NoSeriesPlaceholderDescription,
                StringComparison.Ordinal) &&
            string.IsNullOrWhiteSpace(line.Description2) &&
            string.IsNullOrWhiteSpace(line.LineObjectNumber) &&
            (line.ItemId is null || line.ItemId == Guid.Empty) &&
            line.Quantity == 0m &&
            line.UnitCost == 0m &&
            line.DiscountAmount == 0m &&
            line.DiscountPercent == 0m &&
            line.AmountExcludingTax == 0m &&
            line.TaxPercent == 0m &&
            line.TotalTaxAmount == 0m &&
            line.AmountIncludingTax == 0m);
    }
}
