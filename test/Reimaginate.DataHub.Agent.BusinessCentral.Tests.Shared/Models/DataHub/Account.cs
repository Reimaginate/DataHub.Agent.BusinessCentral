using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "Customer")]
public sealed class Account : DataHubEntity
{
    public Account()
    {
        entityType = nameof(Account);
    }

    public string? Name { get; set; }

    public string? AccountNumber { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? StateOrProvince { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? MainPhone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }
}
