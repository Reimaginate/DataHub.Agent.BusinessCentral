using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "SalesInvoice")]
public sealed class SalesInvoice : DataHubEntity
{
    public SalesInvoice()
    {
        entityType = nameof(SalesInvoice);
    }

    public string? InvoiceNumber { get; set; }

    public string? ExternalDocumentNumber { get; set; }

    public string? InvoiceDate { get; set; }

    public string? PostingDate { get; set; }

    public string? DueDate { get; set; }

    public EntityReference? Customer { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Status { get; set; }

    public bool? PricesIncludeTax { get; set; }

    public decimal? RemainingAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? TotalAmountExcludingTax { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? TotalAmountIncludingTax { get; set; }
}
