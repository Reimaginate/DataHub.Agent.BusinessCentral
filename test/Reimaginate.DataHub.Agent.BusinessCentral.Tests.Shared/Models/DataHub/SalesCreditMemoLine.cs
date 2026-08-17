using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "SalesCreditMemoLine")]
public sealed class SalesCreditMemoLine : DataHubEntity
{
    public SalesCreditMemoLine()
    {
        entityType = nameof(SalesCreditMemoLine);
    }

    public EntityReference? SalesCreditMemo { get; set; }

    public EntityReference? Product { get; set; }

    public int? Sequence { get; set; }

    public string? Description { get; set; }

    public string? Description2 { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? ManualDiscountAmount { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal? AmountExcludingTax { get; set; }

    public decimal? TaxPercent { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? AmountIncludingTax { get; set; }

    public string? ShipmentDate { get; set; }
}
