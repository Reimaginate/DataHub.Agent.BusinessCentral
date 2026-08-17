using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "VendorPaymentJournal")]
public sealed class VendorPaymentJournal : DataHubEntity
{
    public VendorPaymentJournal() => entityType = nameof(VendorPaymentJournal);
    public string? Code { get; set; }
    public string? DisplayName { get; set; }
    public EntityReference? BalancingAccount { get; set; }
    public string? BalancingAccountNumber { get; set; }
}
