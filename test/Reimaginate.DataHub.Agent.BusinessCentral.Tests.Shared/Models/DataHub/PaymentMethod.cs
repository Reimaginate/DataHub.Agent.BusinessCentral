using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "PaymentMethod")]
public sealed class PaymentMethod : DataHubEntity
{
    public PaymentMethod() => entityType = nameof(PaymentMethod);
    public string? Code { get; set; }
    public string? DisplayName { get; set; }
}
