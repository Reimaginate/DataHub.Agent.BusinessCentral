using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "Item")]
public sealed class Product : DataHubEntity
{
    public Product()
    {
        entityType = nameof(Product);
    }

    public string? ProductNumber { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public EntityReference? BaseUnitOfMeasure { get; set; }

    public string? BaseUnitOfMeasureCode { get; set; }
}
