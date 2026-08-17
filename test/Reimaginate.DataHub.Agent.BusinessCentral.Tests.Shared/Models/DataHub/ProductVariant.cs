using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "ItemVariant")]
public sealed class ProductVariant : DataHubEntity
{
    public ProductVariant() => entityType = nameof(ProductVariant);
    public EntityReference? Product { get; set; }
    public string? ItemNumber { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
}
