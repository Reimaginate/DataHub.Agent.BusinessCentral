using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "Contact")]
public sealed class Contact : DataHubEntity
{
    public Contact()
    {
        entityType = nameof(Contact);
    }

    public string? ContactNumber { get; set; }

    public string? Name { get; set; }

    public string? Type { get; set; }

    public string? JobTitle { get; set; }

    public string? CompanyNumber { get; set; }

    public string? CompanyName { get; set; }

    public string? BusinessRelation { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? StateOrProvince { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? BusinessPhone { get; set; }

    public string? MobilePhone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? SearchName { get; set; }

    public bool? PrivacyBlocked { get; set; }

    public string? TaxRegistrationNumber { get; set; }

    public DateTime? LastInteractionDate { get; set; }
}
