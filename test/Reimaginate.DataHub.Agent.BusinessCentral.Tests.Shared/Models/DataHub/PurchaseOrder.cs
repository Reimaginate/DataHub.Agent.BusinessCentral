using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "PurchaseOrder")]
public sealed class PurchaseOrder : DataHubEntity
{
    public PurchaseOrder()
    {
        entityType = nameof(PurchaseOrder);
    }

    public string? OrderNumber { get; set; }

    public string? OrderDate { get; set; }

    public string? PostingDate { get; set; }

    public EntityReference? Supplier { get; set; }

    public string? RequestedReceiptDate { get; set; }

    public decimal? DiscountAmount { get; set; }

    public bool? PricesIncludeTax { get; set; }

    public string? Status { get; set; }

    public bool? FullyReceived { get; set; }

    public decimal? TotalAmountExcludingTax { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? TotalAmountIncludingTax { get; set; }
}
