using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapAccountToCustomer :
    ITypeMapper<DataHubAccount, BusinessCentralCustomer>,
    IDataHubTypeMapper<DataHubAccount, BusinessCentralCustomer>
{
    public List<string> MappedEntityReferences { get; } = [];

    public Task<BusinessCentralCustomer> MapAsync(
        DataHubAccount from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (string.IsNullOrWhiteSpace(from.Name))
        {
            throw new InvalidOperationException("A Data Hub account must have a name before it can be synced as a Business Central customer.");
        }

        return Task.FromResult(new BusinessCentralCustomer
        {
            Number = string.IsNullOrWhiteSpace(from.AccountNumber)
                ? CreateCustomerNumber(from.id)
                : from.AccountNumber,
            DisplayName = from.Name,
            Type = "Company",
            AddressLine1 = from.AddressLine1,
            AddressLine2 = from.AddressLine2,
            City = from.City,
            State = from.StateOrProvince,
            PostalCode = from.PostalCode,
            Country = from.Country,
            PhoneNumber = from.MainPhone,
            Email = from.Email,
            Website = from.Website
        });
    }

    private static string CreateCustomerNumber(string accountId)
    {
        var suffix = new string(accountId.Where(char.IsLetterOrDigit).Take(15).ToArray());
        return $"DHIT-{suffix}".ToUpperInvariant();
    }
}
