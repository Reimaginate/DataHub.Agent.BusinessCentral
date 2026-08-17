using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "SalesCreditMemo")]
public sealed class SalesCreditMemo : DataHubEntity
{
    public SalesCreditMemo()
    {
        entityType = nameof(SalesCreditMemo);
    }

    public string? CreditMemoNumber { get; set; }

    public string? ExternalDocumentNumber { get; set; }

    public string? CreditMemoDate { get; set; }

    public string? PostingDate { get; set; }

    public string? DueDate { get; set; }

    public EntityReference? Customer { get; set; }

    public EntityReference? OriginalSalesInvoice { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Status { get; set; }

    public bool? PricesIncludeTax { get; set; }

    public decimal? DiscountAmount { get; set; }

    public bool? DiscountAppliedBeforeTax { get; set; }

    public decimal? TotalAmountExcludingTax { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? TotalAmountIncludingTax { get; set; }
}
