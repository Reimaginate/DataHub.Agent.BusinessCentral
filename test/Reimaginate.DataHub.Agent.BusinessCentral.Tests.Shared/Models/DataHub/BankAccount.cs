using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "BankAccount")]
public sealed class BankAccount : DataHubEntity
{
    public BankAccount() => entityType = nameof(BankAccount);

    public string? Number { get; set; }
    public string? DisplayName { get; set; }
    public string? BankAccountNumber { get; set; }
    public bool? Blocked { get; set; }
    public string? CurrencyCode { get; set; }
    public EntityReference? Currency { get; set; }
    public string? Iban { get; set; }
    public bool? IntercompanyEnabled { get; set; }
}
