using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "GeneralJournal")]
public sealed class GeneralJournal : DataHubEntity
{
    public GeneralJournal() => entityType = nameof(GeneralJournal);
    public string? Code { get; set; }
    public string? DisplayName { get; set; }
    public string? TemplateDisplayName { get; set; }
    public EntityReference? BalancingAccount { get; set; }
    public string? BalancingAccountNumber { get; set; }
}
