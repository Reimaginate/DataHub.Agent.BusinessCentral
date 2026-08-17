using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "SalesShipmentLine")]
public sealed class SalesShipmentLine : DataHubEntity
{
    public SalesShipmentLine() => entityType = nameof(SalesShipmentLine);

    public EntityReference? SalesShipment { get; set; }
    public string? DocumentNumber { get; set; }
    public int? Sequence { get; set; }
    public string? LineType { get; set; }
    public string? ProductNumber { get; set; }
    public string? Description { get; set; }
    public string? Description2 { get; set; }
    public string? UnitOfMeasureCode { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? TaxPercent { get; set; }
    public string? ShipmentDate { get; set; }
}
