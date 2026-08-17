using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "VendorPayment")]
public sealed class VendorPayment : DataHubEntity
{
    public VendorPayment() => entityType = nameof(VendorPayment);
    public EntityReference? PaymentJournal { get; set; }
    public EntityReference? Supplier { get; set; }
    public EntityReference? AppliesToPurchaseInvoice { get; set; }
    public int? LineNumber { get; set; }
    public string? PostingDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? ExternalDocumentNumber { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public string? Comment { get; set; }
}
