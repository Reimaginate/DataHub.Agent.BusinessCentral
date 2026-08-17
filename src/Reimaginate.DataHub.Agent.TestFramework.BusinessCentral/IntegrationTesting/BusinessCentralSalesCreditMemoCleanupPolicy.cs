using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemoLine;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public static class BusinessCentralSalesCreditMemoCleanupPolicy
{
    public const string NoSeriesPlaceholderDescription =
        "Document created to avoid gap in No. Series";

    public static bool IsSafeNoSeriesPlaceholder(
        BusinessCentralSalesCreditMemo creditMemo,
        IReadOnlyCollection<BusinessCentralSalesCreditMemoLine> lines,
        bool allowBlankExternalDocumentNumber = false)
    {
        if (!Guid.TryParse(creditMemo.Id, out var creditMemoId) ||
            string.IsNullOrWhiteSpace(creditMemo.ETag) ||
            string.Equals(creditMemo.ETag.Trim(), "*", StringComparison.Ordinal) ||
            !string.Equals(creditMemo.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
            !IsSafeExternalDocumentNumber(
                creditMemo.ExternalDocumentNumber,
                allowBlankExternalDocumentNumber) ||
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
            line.Sequence == 10000 &&
            string.Equals(line.LineType, "Comment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                line.Description,
                NoSeriesPlaceholderDescription,
                StringComparison.Ordinal) &&
            string.IsNullOrWhiteSpace(line.Description2) &&
            string.IsNullOrWhiteSpace(line.LineObjectNumber) &&
            (line.ItemId is null || line.ItemId == Guid.Empty) &&
            line.Quantity == 0m &&
            line.UnitPrice == 0m &&
            line.DiscountAmount == 0m &&
            line.DiscountPercent == 0m &&
            line.AmountExcludingTax == 0m &&
            line.TaxPercent == 0m &&
            line.TotalTaxAmount == 0m &&
            line.AmountIncludingTax == 0m &&
            IsBlankDate(line.ShipmentDate));
    }

    private static bool IsSafeExternalDocumentNumber(
        string? value,
        bool allowBlankExternalDocumentNumber) =>
        value?.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase) == true ||
        allowBlankExternalDocumentNumber && string.IsNullOrEmpty(value);

    private static bool IsBlankDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        string.Equals(value, "0001-01-01", StringComparison.Ordinal);
}
