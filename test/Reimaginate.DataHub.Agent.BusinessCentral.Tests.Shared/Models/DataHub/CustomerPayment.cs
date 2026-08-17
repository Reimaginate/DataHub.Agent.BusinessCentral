using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "CustomerPayment")]
public sealed class CustomerPayment : DataHubEntity
{
    public CustomerPayment() => entityType = nameof(CustomerPayment);

    public EntityReference? PaymentJournal { get; set; }
    public EntityReference? Customer { get; set; }
    public EntityReference? AppliesToSalesInvoice { get; set; }
    public int? LineNumber { get; set; }
    public string? PostingDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? ExternalDocumentNumber { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public string? Comment { get; set; }
}
