using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "SalesShipment")]
public sealed class SalesShipment : DataHubEntity
{
    public SalesShipment() => entityType = nameof(SalesShipment);

    public string? ShipmentNumber { get; set; }
    public string? ExternalDocumentNumber { get; set; }
    public string? InvoiceDate { get; set; }
    public string? PostingDate { get; set; }
    public string? DueDate { get; set; }
    public string? CustomerPurchaseOrderReference { get; set; }
    public EntityReference? Customer { get; set; }
    public string? CustomerNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? ShipToName { get; set; }
    public string? ShipToContact { get; set; }
    public string? ShipToCity { get; set; }
    public string? ShipToCountry { get; set; }
    public string? ShipToState { get; set; }
    public string? ShipToPostCode { get; set; }
    public string? CurrencyCode { get; set; }
    public string? SalesOrderNumber { get; set; }
    public bool? PricesIncludeTax { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
