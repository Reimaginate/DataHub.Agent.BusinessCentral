using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "ItemLedgerEntry")]
public sealed class ItemLedgerEntry : DataHubEntity
{
    public ItemLedgerEntry() => entityType = nameof(ItemLedgerEntry);
    public int? EntryNumber { get; set; }
    public string? ItemNumber { get; set; }
    public string? PostingDate { get; set; }
    public string? EntryType { get; set; }
    public string? SourceNumber { get; set; }
    public string? SourceType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? DocumentType { get; set; }
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? SalesAmountActual { get; set; }
    public decimal? CostAmountActual { get; set; }
}
