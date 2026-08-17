using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "PurchaseCreditMemo")]
public sealed class PurchaseCreditMemo : DataHubEntity
{
    public PurchaseCreditMemo() => entityType = nameof(PurchaseCreditMemo);

    public string? CreditMemoNumber { get; set; }
    public string? VendorCreditMemoNumber { get; set; }
    public string? CreditMemoDate { get; set; }
    public string? PostingDate { get; set; }
    public string? DueDate { get; set; }
    public EntityReference? Supplier { get; set; }
    public EntityReference? OriginalPurchaseInvoice { get; set; }
    public string? Status { get; set; }
    public bool? PricesIncludeTax { get; set; }
    public decimal? DiscountAmount { get; set; }
    public bool? DiscountAppliedBeforeTax { get; set; }
    public decimal? TotalAmountExcludingTax { get; set; }
    public decimal? TotalTaxAmount { get; set; }
    public decimal? TotalAmountIncludingTax { get; set; }
}
