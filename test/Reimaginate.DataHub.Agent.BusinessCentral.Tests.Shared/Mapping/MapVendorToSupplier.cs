using Reimaginate.Mapper;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using BusinessCentralCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Currency;
using BusinessCentralPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentMethod;
using BusinessCentralPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentTerm;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;
using DataHubCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Currency;
using DataHubPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PaymentMethod;
using DataHubPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PaymentTerm;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapVendorToSupplier : ITypeMapper<BusinessCentralVendor, DataHubSupplier>
{
    public Task<DataHubSupplier> MapAsync(
        BusinessCentralVendor from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubSupplier
        {
            id = from.Id!,
            createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            SupplierNumber = from.Number,
            Name = from.DisplayName,
            AddressLine1 = from.AddressLine1,
            AddressLine2 = from.AddressLine2,
            City = from.City,
            StateOrProvince = from.State,
            PostalCode = from.PostalCode,
            Country = from.Country,
            MainPhone = from.PhoneNumber,
            Email = from.Email,
            Website = from.Website,
            TaxRegistrationNumber = from.TaxRegistrationNumber,
            Currency = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubCurrency, BusinessCentralCurrency>(from.CurrencyId),
            CurrencyCode = from.CurrencyCode,
            PaymentTerms = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubPaymentTerm, BusinessCentralPaymentTerm>(from.PaymentTermsId),
            PaymentMethod = BusinessCentralMappingHelpers
                .ToDataHubReference<DataHubPaymentMethod, BusinessCentralPaymentMethod>(from.PaymentMethodId)
        });
    }
}
