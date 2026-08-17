using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "SalesQuote")]
public sealed class Quote : DataHubEntity
{
    public Quote()
    {
        entityType = nameof(Quote);
    }

    public string? Name { get; set; }

    public string? QuoteNumber { get; set; }

    public string? ExternalDocumentNumber { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public EntityReference? CustomerAccount { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Status { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? TotalLineItemAmount { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? TotalAmount { get; set; }
}
