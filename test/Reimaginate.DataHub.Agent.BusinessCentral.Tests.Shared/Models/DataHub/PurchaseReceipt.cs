using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "PurchaseReceipt")]
public sealed class PurchaseReceipt : DataHubEntity
{
    public PurchaseReceipt() => entityType = nameof(PurchaseReceipt);

    public string? ReceiptNumber { get; set; }
    public string? InvoiceDate { get; set; }
    public string? PostingDate { get; set; }
    public string? DueDate { get; set; }
    public string? SupplierNumber { get; set; }
    public string? SupplierName { get; set; }
    public string? PayToName { get; set; }
    public string? PayToContact { get; set; }
    public string? ShipToName { get; set; }
    public string? ShipToContact { get; set; }
    public string? ShipToCity { get; set; }
    public string? ShipToCountry { get; set; }
    public string? ShipToState { get; set; }
    public string? ShipToPostCode { get; set; }
    public string? CurrencyCode { get; set; }
    public string? PurchaseOrderNumber { get; set; }
}
