using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "PurchaseInvoice")]
public sealed class PurchaseInvoice : DataHubEntity
{
    public PurchaseInvoice()
    {
        entityType = nameof(PurchaseInvoice);
    }

    public string? InvoiceNumber { get; set; }

    public string? VendorInvoiceNumber { get; set; }

    public string? PostingDate { get; set; }

    public string? InvoiceDate { get; set; }

    public string? DueDate { get; set; }

    public EntityReference? Supplier { get; set; }

    public string? Status { get; set; }

    public bool? PricesIncludeTax { get; set; }

    public decimal? DiscountAmount { get; set; }

    public bool? DiscountAppliedBeforeTax { get; set; }

    public decimal? TotalAmountExcludingTax { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? TotalAmountIncludingTax { get; set; }
}
