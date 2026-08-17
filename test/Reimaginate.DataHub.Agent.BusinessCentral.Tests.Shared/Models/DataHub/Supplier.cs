using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "Vendor")]
public sealed class Supplier : DataHubEntity
{
    public Supplier()
    {
        entityType = nameof(Supplier);
    }

    public string? SupplierNumber { get; set; }

    public string? Name { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? StateOrProvince { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? MainPhone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? TaxRegistrationNumber { get; set; }

    public EntityReference? Currency { get; set; }

    public string? CurrencyCode { get; set; }

    public EntityReference? PaymentTerms { get; set; }

    public EntityReference? PaymentMethod { get; set; }
}
