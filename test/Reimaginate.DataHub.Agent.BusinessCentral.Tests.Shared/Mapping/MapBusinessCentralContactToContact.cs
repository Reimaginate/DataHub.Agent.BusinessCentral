using Reimaginate.Mapper;
using DataHubContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Contact;
using BusinessCentralContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Contact;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralContactToContact : ITypeMapper<BusinessCentralContact, DataHubContact>
{
    public Task<DataHubContact> MapAsync(
        BusinessCentralContact from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubContact
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            ContactNumber = from.Number,
            Name = from.DisplayName,
            Type = from.Type,
            JobTitle = from.JobTitle,
            CompanyNumber = from.CompanyNumber,
            CompanyName = from.CompanyName,
            BusinessRelation = from.ContactBusinessRelation,
            AddressLine1 = from.AddressLine1,
            AddressLine2 = from.AddressLine2,
            City = from.City,
            StateOrProvince = from.State,
            Country = from.Country,
            PostalCode = from.PostalCode,
            BusinessPhone = from.PhoneNumber,
            MobilePhone = from.MobilePhoneNumber,
            Email = from.Email,
            Website = from.Website,
            SearchName = from.SearchName,
            PrivacyBlocked = from.PrivacyBlocked,
            TaxRegistrationNumber = from.TaxRegistrationNumber,
            LastInteractionDate = from.LastInteractionDate
        });
    }
}
