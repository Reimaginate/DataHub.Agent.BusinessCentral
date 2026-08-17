using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "CustomerPaymentJournal")]
public sealed class CustomerPaymentJournal : DataHubEntity
{
    public CustomerPaymentJournal() => entityType = nameof(CustomerPaymentJournal);

    public string? Code { get; set; }
    public string? DisplayName { get; set; }
    public EntityReference? BalancingAccount { get; set; }
    public string? BalancingAccountNumber { get; set; }
}
