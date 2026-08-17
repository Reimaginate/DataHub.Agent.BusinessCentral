using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemoLine;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public static class BusinessCentralPurchaseCreditMemoCleanupPolicy
{
    public const string NoSeriesPlaceholderDescription =
        "Document created to avoid gap in No. Series";

    public static bool IsSafeNoSeriesPlaceholder(
        BusinessCentralPurchaseCreditMemo creditMemo,
        IReadOnlyCollection<BusinessCentralPurchaseCreditMemoLine> lines)
    {
        if (!Guid.TryParse(creditMemo.Id, out var creditMemoId) ||
            string.IsNullOrWhiteSpace(creditMemo.ETag) ||
            !string.Equals(creditMemo.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
            creditMemo.VendorCreditMemoNumber?.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase) != true ||
            creditMemo.DiscountAmount != 0m ||
            creditMemo.TotalAmountExcludingTax != 0m ||
            creditMemo.TotalTaxAmount != 0m ||
            creditMemo.TotalAmountIncludingTax != 0m ||
            lines.Count != 1)
        {
            return false;
        }

        return lines.All(line =>
            Guid.TryParse(line.Id, out _) &&
            line.DocumentId == creditMemoId &&
            string.Equals(line.LineType, "Comment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(line.Description, NoSeriesPlaceholderDescription, StringComparison.Ordinal) &&
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
