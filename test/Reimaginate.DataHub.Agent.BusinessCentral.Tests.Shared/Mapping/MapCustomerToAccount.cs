using Reimaginate.Mapper;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapCustomerToAccount : ITypeMapper<BusinessCentralCustomer, DataHubAccount>
{
    public Task<DataHubAccount> MapAsync(
        BusinessCentralCustomer from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubAccount
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Name = from.DisplayName,
            AccountNumber = from.Number,
            AddressLine1 = from.AddressLine1,
            AddressLine2 = from.AddressLine2,
            City = from.City,
            StateOrProvince = from.State,
            PostalCode = from.PostalCode,
            Country = from.Country,
            MainPhone = from.PhoneNumber,
            Email = from.Email,
            Website = from.Website
        });
    }
}
