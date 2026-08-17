using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapSupplierToVendor :
    ITypeMapper<DataHubSupplier, BusinessCentralVendor>,
    IDataHubTypeMapper<DataHubSupplier, BusinessCentralVendor>
{
    public List<string> MappedEntityReferences { get; } = [];

    public Task<BusinessCentralVendor> MapAsync(
        DataHubSupplier from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        if (string.IsNullOrWhiteSpace(from.Name))
        {
            throw new InvalidOperationException(
                "A Data Hub supplier must have a name before it can be synced as a Business Central vendor.");
        }

        return Task.FromResult(new BusinessCentralVendor
        {
            Number = string.IsNullOrWhiteSpace(from.SupplierNumber)
                ? CreateVendorNumber(from.id)
                : from.SupplierNumber,
            DisplayName = from.Name,
            AddressLine1 = from.AddressLine1,
            AddressLine2 = from.AddressLine2,
            City = from.City,
            State = from.StateOrProvince,
            PostalCode = from.PostalCode,
            Country = from.Country,
            PhoneNumber = from.MainPhone,
            Email = from.Email,
            Website = from.Website,
            TaxRegistrationNumber = from.TaxRegistrationNumber
        });
    }

    private static string CreateVendorNumber(string supplierId)
    {
        var suffix = new string(supplierId.Where(char.IsLetterOrDigit).Take(15).ToArray());
        return $"DHIT-{suffix}".ToUpperInvariant();
    }
}
