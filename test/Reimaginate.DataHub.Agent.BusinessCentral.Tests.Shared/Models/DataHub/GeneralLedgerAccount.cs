using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "GeneralLedgerAccount")]
public sealed class GeneralLedgerAccount : DataHubEntity
{
    public GeneralLedgerAccount() => entityType = nameof(GeneralLedgerAccount);

    public string? Number { get; set; }
    public string? DisplayName { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public bool? Blocked { get; set; }
    public string? AccountType { get; set; }
    public bool? DirectPosting { get; set; }
    public decimal? NetChange { get; set; }
}
