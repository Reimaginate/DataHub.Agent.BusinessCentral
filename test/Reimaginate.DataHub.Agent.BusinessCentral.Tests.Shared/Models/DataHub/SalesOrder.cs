using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "SalesOrder")]
public sealed class SalesOrder : DataHubEntity
{
    public SalesOrder()
    {
        entityType = nameof(SalesOrder);
    }

    public string? OrderNumber { get; set; }

    public string? ExternalDocumentNumber { get; set; }

    public string? OrderDate { get; set; }

    public EntityReference? Customer { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Status { get; set; }

    public decimal? TotalAmountExcludingTax { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? TotalAmountIncludingTax { get; set; }
}
