using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub;

// START HERE: replace these deliberately small contracts with the canonical
// entity contracts already used by your DataHub.
[RelatedEntityType("BusinessCentral", "Customer")]
public sealed class Account : DataHubEntity
{
    public Account() => entityType = nameof(Account);
    public string? Name { get; set; }
    public string? AccountNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

[RelatedEntityType("BusinessCentral", "Item")]
public sealed class Product : DataHubEntity
{
    public Product() => entityType = nameof(Product);
    public string? ProductNumber { get; set; }
    public string? Name { get; set; }
    public decimal? UnitPrice { get; set; }
}

[RelatedEntityType("BusinessCentral", "SalesOrder")]
public sealed class SalesOrder : DataHubEntity
{
    public SalesOrder() => entityType = nameof(SalesOrder);
    public string? OrderNumber { get; set; }
    public string? ExternalDocumentNumber { get; set; }
    public string? OrderDate { get; set; }
    public EntityReference? Customer { get; set; }
    public string? Status { get; set; }
    public decimal? TotalAmountIncludingTax { get; set; }
}

[RelatedEntityType("BusinessCentral", "SalesOrderLine")]
public sealed class SalesOrderLine : DataHubEntity
{
    public SalesOrderLine() => entityType = nameof(SalesOrderLine);
    public EntityReference? SalesOrder { get; set; }
    public EntityReference? Product { get; set; }
    public int? Sequence { get; set; }
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? AmountIncludingTax { get; set; }
}
