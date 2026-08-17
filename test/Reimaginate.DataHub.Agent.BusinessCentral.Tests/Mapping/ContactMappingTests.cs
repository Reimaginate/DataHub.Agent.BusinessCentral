using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Xunit;
using BusinessCentralContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Contact;
using DataHubContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Contact;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class ContactMappingTests
{
    [Fact]
    public async Task DataHubContactMapsAllSupportedFieldsToBusinessCentral()
    {
        var source = NewDataHubContact();

        var result = await new MapContactToBusinessCentralContact()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal("DHIT-0123456789ABCDE", result.Number);
        Assert.Equal(source.Name, result.DisplayName);
        Assert.Equal(source.Type, result.Type);
        Assert.Equal(source.JobTitle, result.JobTitle);
        Assert.Equal(source.CompanyNumber, result.CompanyNumber);
        Assert.Equal(source.CompanyName, result.CompanyName);
        Assert.Null(result.ContactBusinessRelation);
        Assert.Equal(source.AddressLine1, result.AddressLine1);
        Assert.Equal(source.AddressLine2, result.AddressLine2);
        Assert.Equal(source.City, result.City);
        Assert.Equal(source.StateOrProvince, result.State);
        Assert.Equal(source.PostalCode, result.PostalCode);
        Assert.Equal(source.Country, result.Country);
        Assert.Equal(source.BusinessPhone, result.PhoneNumber);
        Assert.Equal(source.MobilePhone, result.MobilePhoneNumber);
        Assert.Equal(source.Email, result.Email);
        Assert.Equal(source.Website, result.Website);
        Assert.Null(result.SearchName);
        Assert.Equal(source.PrivacyBlocked, result.PrivacyBlocked);
        Assert.Equal(source.TaxRegistrationNumber, result.TaxRegistrationNumber);
        Assert.Null(result.LastInteractionDate);
    }

    [Fact]
    public async Task BusinessCentralContactMapsAllSupportedFieldsToDataHub()
    {
        var modified = new DateTimeOffset(2026, 8, 13, 1, 2, 3, TimeSpan.Zero);
        var source = new BusinessCentralContact
        {
            Id = Guid.NewGuid().ToString(),
            Number = "CT-100",
            DisplayName = "Ada Lovelace",
            Type = "Person",
            JobTitle = "Mathematician",
            CompanyNumber = "CT-001",
            CompanyName = "Analytical Engines",
            ContactBusinessRelation = "Customer",
            AddressLine1 = "1 Computing Lane",
            AddressLine2 = "Suite 2",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            Country = "AU",
            PhoneNumber = "+61 2 9000 0000",
            MobilePhoneNumber = "+61 400 000 000",
            Email = "ada@example.test",
            Website = "https://example.test/ada",
            SearchName = "ADA LOVELACE",
            PrivacyBlocked = true,
            TaxRegistrationNumber = "TAX-100",
            LastInteractionDate = new DateTime(2026, 8, 12),
            LastModifiedDateTime = modified
        };

        var result = await new MapBusinessCentralContactToContact()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(modified, result.createdOn);
        Assert.Equal(modified, result.lastUpdated);
        Assert.Equal(source.Number, result.ContactNumber);
        Assert.Equal(source.DisplayName, result.Name);
        Assert.Equal(source.Type, result.Type);
        Assert.Equal(source.JobTitle, result.JobTitle);
        Assert.Equal(source.CompanyNumber, result.CompanyNumber);
        Assert.Equal(source.CompanyName, result.CompanyName);
        Assert.Equal(source.ContactBusinessRelation, result.BusinessRelation);
        Assert.Equal(source.AddressLine1, result.AddressLine1);
        Assert.Equal(source.AddressLine2, result.AddressLine2);
        Assert.Equal(source.City, result.City);
        Assert.Equal(source.State, result.StateOrProvince);
        Assert.Equal(source.PostalCode, result.PostalCode);
        Assert.Equal(source.Country, result.Country);
        Assert.Equal(source.PhoneNumber, result.BusinessPhone);
        Assert.Equal(source.MobilePhoneNumber, result.MobilePhone);
        Assert.Equal(source.Email, result.Email);
        Assert.Equal(source.Website, result.Website);
        Assert.Equal(source.SearchName, result.SearchName);
        Assert.Equal(source.PrivacyBlocked, result.PrivacyBlocked);
        Assert.Equal(source.TaxRegistrationNumber, result.TaxRegistrationNumber);
        Assert.Equal(source.LastInteractionDate, result.LastInteractionDate);
    }

    [Fact]
    public async Task DataHubContactRequiresAName()
    {
        var source = NewDataHubContact();
        source.Name = null;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapContactToBusinessCentralContact().MapAsync(source, CancellationToken.None));

        Assert.Contains("must have a name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DataHubContact NewDataHubContact()
    {
        return new DataHubContact
        {
            id = "0123456789abcdef0123456789abcdef",
            Name = "Ada Lovelace",
            Type = "Person",
            JobTitle = "Mathematician",
            CompanyNumber = "CT-001",
            CompanyName = "Analytical Engines",
            BusinessRelation = "Customer",
            AddressLine1 = "1 Computing Lane",
            AddressLine2 = "Suite 2",
            City = "Sydney",
            StateOrProvince = "NSW",
            PostalCode = "2000",
            Country = "AU",
            BusinessPhone = "+61 2 9000 0000",
            MobilePhone = "+61 400 000 000",
            Email = "ada@example.test",
            Website = "https://example.test/ada",
            SearchName = "ADA LOVELACE",
            PrivacyBlocked = true,
            TaxRegistrationNumber = "TAX-100",
            LastInteractionDate = new DateTime(2026, 8, 12)
        };
    }
}
