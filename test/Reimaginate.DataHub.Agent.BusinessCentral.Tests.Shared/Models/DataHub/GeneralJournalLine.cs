using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "GeneralJournalLine")]
public sealed class GeneralJournalLine : DataHubEntity
{
    public GeneralJournalLine() => entityType = nameof(GeneralJournalLine);
    public EntityReference? Journal { get; set; }
    public EntityReference? Account { get; set; }
    public EntityReference? BalancingAccount { get; set; }
    public int? LineNumber { get; set; }
    public string? AccountType { get; set; }
    public string? AccountNumber { get; set; }
    public string? PostingDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? ExternalDocumentNumber { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public string? Comment { get; set; }
    public string? TaxCode { get; set; }
    public string? BalanceAccountType { get; set; }
    public string? BalancingAccountNumber { get; set; }
}
