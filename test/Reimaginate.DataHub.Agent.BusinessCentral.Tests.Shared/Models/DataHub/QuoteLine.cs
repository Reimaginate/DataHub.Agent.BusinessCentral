using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "SalesQuoteLine")]
public sealed class QuoteLine : DataHubEntity
{
    public QuoteLine()
    {
        entityType = nameof(QuoteLine);
    }

    public EntityReference? Quote { get; set; }

    public EntityReference? Product { get; set; }

    public int? Sequence { get; set; }

    public string? Description { get; set; }

    public string? Description2 { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? PricePerUnit { get; set; }

    public decimal? ManualDiscountAmount { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal? BaseAmount { get; set; }

    public decimal? TaxPercent { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? ExtendedAmount { get; set; }
}
