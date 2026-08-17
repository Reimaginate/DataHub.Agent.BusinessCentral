using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Xunit;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class SupplierMappingTests
{
    [Fact]
    public async Task DataHubSupplierMapsOnlyOwnedFieldsToBusinessCentral()
    {
        var source = NewDataHubSupplier();

        var result = await new MapSupplierToVendor().MapAsync(source, CancellationToken.None);

        Assert.Equal("DHIT-0123456789ABCDE", result.Number);
        Assert.Equal(source.Name, result.DisplayName);
        Assert.Equal(source.AddressLine1, result.AddressLine1);
        Assert.Equal(source.AddressLine2, result.AddressLine2);
        Assert.Equal(source.City, result.City);
        Assert.Equal(source.StateOrProvince, result.State);
        Assert.Equal(source.PostalCode, result.PostalCode);
        Assert.Equal(source.Country, result.Country);
        Assert.Equal(source.MainPhone, result.PhoneNumber);
        Assert.Equal(source.Email, result.Email);
        Assert.Equal(source.Website, result.Website);
        Assert.Equal(source.TaxRegistrationNumber, result.TaxRegistrationNumber);
        Assert.Null(result.CurrencyId);
        Assert.Null(result.CurrencyCode);
        Assert.Null(result.Irs1099Code);
        Assert.Null(result.PaymentTermsId);
        Assert.Null(result.PaymentMethodId);
        Assert.Null(result.TaxLiable);
        Assert.Null(result.Blocked);
        Assert.Null(result.Balance);
    }

    [Fact]
    public async Task BusinessCentralVendorMapsSupportedFieldsToDataHub()
    {
        var modified = new DateTimeOffset(2026, 8, 14, 1, 2, 3, TimeSpan.Zero);
        var source = new BusinessCentralVendor
        {
            Id = Guid.NewGuid().ToString(),
            Number = "V-100",
            DisplayName = "Contoso Supplies",
            AddressLine1 = "1 Supply Street",
            AddressLine2 = "Warehouse 2",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            Country = "AU",
            PhoneNumber = "+61 2 9000 0060",
            Email = "purchasing@example.test",
            Website = "https://example.test/suppliers/contoso",
            TaxRegistrationNumber = "TAX-100",
            CurrencyCode = "AUD",
            Blocked = "Payment",
            Balance = 123.45m,
            LastModifiedDateTime = modified
        };

        var result = await new MapVendorToSupplier().MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(modified, result.createdOn);
        Assert.Equal(modified, result.lastUpdated);
        Assert.Equal(source.Number, result.SupplierNumber);
        Assert.Equal(source.DisplayName, result.Name);
        Assert.Equal(source.AddressLine1, result.AddressLine1);
        Assert.Equal(source.AddressLine2, result.AddressLine2);
        Assert.Equal(source.City, result.City);
        Assert.Equal(source.State, result.StateOrProvince);
        Assert.Equal(source.PostalCode, result.PostalCode);
        Assert.Equal(source.Country, result.Country);
        Assert.Equal(source.PhoneNumber, result.MainPhone);
        Assert.Equal(source.Email, result.Email);
        Assert.Equal(source.Website, result.Website);
        Assert.Equal(source.TaxRegistrationNumber, result.TaxRegistrationNumber);
    }

    [Fact]
    public async Task DataHubSupplierRequiresAName()
    {
        var source = NewDataHubSupplier();
        source.Name = null;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapSupplierToVendor().MapAsync(source, CancellationToken.None));

        Assert.Contains("must have a name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DataHubSupplier NewDataHubSupplier()
    {
        return new DataHubSupplier
        {
            id = "0123456789abcdef0123456789abcdef",
            Name = "Contoso Supplies",
            AddressLine1 = "1 Supply Street",
            AddressLine2 = "Warehouse 2",
            City = "Sydney",
            StateOrProvince = "NSW",
            PostalCode = "2000",
            Country = "AU",
            MainPhone = "+61 2 9000 0060",
            Email = "purchasing@example.test",
            Website = "https://example.test/suppliers/contoso",
            TaxRegistrationNumber = "TAX-100"
        };
    }
}
