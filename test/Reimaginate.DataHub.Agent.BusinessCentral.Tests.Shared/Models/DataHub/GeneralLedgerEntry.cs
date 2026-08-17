using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "GeneralLedgerEntry")]
public sealed class GeneralLedgerEntry : DataHubEntity
{
    public GeneralLedgerEntry() => entityType = nameof(GeneralLedgerEntry);
    public EntityReference? Account { get; set; }
    public int? EntryNumber { get; set; }
    public string? PostingDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? DocumentType { get; set; }
    public string? AccountNumber { get; set; }
    public string? Description { get; set; }
    public decimal? DebitAmount { get; set; }
    public decimal? CreditAmount { get; set; }
    public decimal? AdditionalCurrencyDebitAmount { get; set; }
    public decimal? AdditionalCurrencyCreditAmount { get; set; }
}
