using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "PurchaseReceiptLine")]
public sealed class PurchaseReceiptLine : DataHubEntity
{
    public PurchaseReceiptLine() => entityType = nameof(PurchaseReceiptLine);

    public EntityReference? PurchaseReceipt { get; set; }
    public int? Sequence { get; set; }
    public string? LineType { get; set; }
    public string? ProductNumber { get; set; }
    public string? Description { get; set; }
    public string? Description2 { get; set; }
    public string? UnitOfMeasureCode { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? TaxPercent { get; set; }
    public string? ExpectedReceiptDate { get; set; }
}
