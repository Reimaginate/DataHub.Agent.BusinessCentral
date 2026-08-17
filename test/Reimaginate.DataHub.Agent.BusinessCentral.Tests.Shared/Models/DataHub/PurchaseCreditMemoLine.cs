using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "PurchaseCreditMemoLine")]
public sealed class PurchaseCreditMemoLine : DataHubEntity
{
    public PurchaseCreditMemoLine() => entityType = nameof(PurchaseCreditMemoLine);

    public EntityReference? PurchaseCreditMemo { get; set; }
    public EntityReference? Product { get; set; }
    public int? Sequence { get; set; }
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool? DiscountAppliedBeforeTax { get; set; }
    public decimal? AmountExcludingTax { get; set; }
    public decimal? TaxPercent { get; set; }
    public decimal? TotalTaxAmount { get; set; }
    public decimal? AmountIncludingTax { get; set; }
}
