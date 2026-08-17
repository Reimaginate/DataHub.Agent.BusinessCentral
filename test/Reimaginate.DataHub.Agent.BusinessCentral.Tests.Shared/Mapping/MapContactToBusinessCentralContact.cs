using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using DataHubContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Contact;
using BusinessCentralContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Contact;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapContactToBusinessCentralContact :
    ITypeMapper<DataHubContact, BusinessCentralContact>,
    IDataHubTypeMapper<DataHubContact, BusinessCentralContact>
{
    public List<string> MappedEntityReferences { get; } = [];

    public Task<BusinessCentralContact> MapAsync(
        DataHubContact from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (string.IsNullOrWhiteSpace(from.Name))
        {
            throw new InvalidOperationException(
                "A Data Hub contact must have a name before it can be synced as a Business Central contact.");
        }

        return Task.FromResult(new BusinessCentralContact
        {
            Number = string.IsNullOrWhiteSpace(from.ContactNumber)
                ? CreateContactNumber(from.id)
                : from.ContactNumber,
            Type = string.IsNullOrWhiteSpace(from.Type) ? "Person" : from.Type,
            DisplayName = from.Name,
            JobTitle = from.JobTitle,
            CompanyNumber = from.CompanyNumber,
            CompanyName = from.CompanyName,
            AddressLine1 = from.AddressLine1,
            AddressLine2 = from.AddressLine2,
            City = from.City,
            State = from.StateOrProvince,
            Country = from.Country,
            PostalCode = from.PostalCode,
            PhoneNumber = from.BusinessPhone,
            MobilePhoneNumber = from.MobilePhone,
            Email = from.Email,
            Website = from.Website,
            PrivacyBlocked = from.PrivacyBlocked,
            TaxRegistrationNumber = from.TaxRegistrationNumber
        });
    }

    private static string CreateContactNumber(string contactId)
    {
        var suffix = new string(contactId.Where(char.IsLetterOrDigit).Take(15).ToArray());
        return $"DHIT-{suffix}".ToUpperInvariant();
    }
}
