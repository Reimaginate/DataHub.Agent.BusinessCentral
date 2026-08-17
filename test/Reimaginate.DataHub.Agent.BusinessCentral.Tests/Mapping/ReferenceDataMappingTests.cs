using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BCBank = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.BankAccount;
using BCCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Currency;
using BCItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BCItemVariant = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.ItemVariant;
using BCLocation = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Location;
using BCPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentMethod;
using BCPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentTerm;
using BCUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.UnitOfMeasure;
using BCVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DHBank = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.BankAccount;
using DHCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Currency;
using DHLocation = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.InventoryLocation;
using DHPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PaymentMethod;
using DHPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PaymentTerm;
using DHProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DHProductVariant = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.ProductVariant;
using DHSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;
using DHUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.UnitOfMeasure;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class ReferenceDataMappingTests
{
    [Fact]
    public async Task ReferenceValuesMapInboundWithoutLosingBusinessCentralSemantics()
    {
        var currency = await new MapBusinessCentralCurrencyToCurrency().MapAsync(
            new BCCurrency { Id = Guid.NewGuid().ToString(), Code = "AUD", Symbol = "$", AmountRoundingPrecision = 0.01m }, CancellationToken.None);
        var terms = await new MapBusinessCentralPaymentTermToPaymentTerm().MapAsync(
            new BCPaymentTerm { Id = Guid.NewGuid().ToString(), Code = "30D", DueDateCalculation = "30D", DiscountPercent = 2m }, CancellationToken.None);
        var method = await new MapBusinessCentralPaymentMethodToPaymentMethod().MapAsync(
            new BCPaymentMethod { Id = Guid.NewGuid().ToString(), Code = "EFT", DisplayName = "Electronic" }, CancellationToken.None);
        var unit = await new MapBusinessCentralUnitOfMeasureToUnitOfMeasure().MapAsync(
            new BCUnitOfMeasure { Id = Guid.NewGuid().ToString(), Code = "EA", InternationalStandardCode = "H87" }, CancellationToken.None);
        var location = await new MapBusinessCentralLocationToInventoryLocation().MapAsync(
            new BCLocation { Id = Guid.NewGuid().ToString(), Code = "MAIN", City = "Sydney" }, CancellationToken.None);

        Assert.Equal("AUD", currency.Code);
        Assert.Equal(0.01m, currency.AmountRoundingPrecision);
        Assert.Equal("30D", terms.DueDateCalculation);
        Assert.Equal(2m, terms.DiscountPercent);
        Assert.Equal("EFT", method.Code);
        Assert.Equal("H87", unit.InternationalStandardCode);
        Assert.Equal("Sydney", location.City);
    }

    [Fact]
    public async Task ItemVariantMapsItsExternalProductReference()
    {
        var itemId = Guid.NewGuid();
        var result = await new MapBusinessCentralItemVariantToProductVariant().MapAsync(
            new BCItemVariant { Id = Guid.NewGuid().ToString(), ItemId = itemId, ItemNumber = "ITEM", Code = "BLUE" },
            CancellationToken.None);

        var reference = Assert.IsType<ExternalEntityReference>(result.Product);
        Assert.Equal(typeof(DHProduct).Name, reference.EntityType);
        Assert.Equal(typeof(BCItem).Name, reference.SourceEntityType);
        Assert.Equal(itemId.ToString(), reference.EntityId);
    }

    [Fact]
    public async Task ExistingMasterDataMappingsExposeReferenceRelationships()
    {
        var currencyId = Guid.NewGuid();
        var termsId = Guid.NewGuid();
        var methodId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var supplier = await new MapVendorToSupplier().MapAsync(new BCVendor
        {
            Id = Guid.NewGuid().ToString(), CurrencyId = currencyId, CurrencyCode = "AUD",
            PaymentTermsId = termsId, PaymentMethodId = methodId
        }, CancellationToken.None);
        var product = await new MapItemToProduct().MapAsync(new BCItem
        {
            Id = Guid.NewGuid().ToString(), BaseUnitOfMeasureId = unitId, BaseUnitOfMeasureCode = "EA"
        }, CancellationToken.None);
        var bank = await new MapBusinessCentralBankAccountToBankAccount().MapAsync(new BCBank
        {
            Id = Guid.NewGuid().ToString(), CurrencyId = currencyId, CurrencyCode = "AUD"
        }, CancellationToken.None);

        AssertExternal<DHCurrency, BCCurrency>(supplier.Currency, currencyId);
        AssertExternal<DHPaymentTerm, BCPaymentTerm>(supplier.PaymentTerms, termsId);
        AssertExternal<DHPaymentMethod, BCPaymentMethod>(supplier.PaymentMethod, methodId);
        AssertExternal<DHUnitOfMeasure, BCUnitOfMeasure>(product.BaseUnitOfMeasure, unitId);
        AssertExternal<DHCurrency, BCCurrency>(bank.Currency, currencyId);
    }

    [Fact]
    public async Task EveryReferenceMapperRejectsOutboundWrites()
    {
        var currency = new MapCurrencyToBusinessCentralCurrency();
        var term = new MapPaymentTermToBusinessCentralPaymentTerm();
        var method = new MapPaymentMethodToBusinessCentralPaymentMethod();
        var unit = new MapUnitOfMeasureToBusinessCentralUnitOfMeasure();
        var location = new MapInventoryLocationToBusinessCentralLocation();
        var variant = new MapProductVariantToBusinessCentralItemVariant();

        Assert.IsAssignableFrom<IDataHubTypeMapper<DHCurrency, BCCurrency>>(currency);
        Assert.IsAssignableFrom<IDataHubTypeMapper<DHPaymentTerm, BCPaymentTerm>>(term);
        Assert.IsAssignableFrom<IDataHubTypeMapper<DHPaymentMethod, BCPaymentMethod>>(method);
        Assert.IsAssignableFrom<IDataHubTypeMapper<DHUnitOfMeasure, BCUnitOfMeasure>>(unit);
        Assert.IsAssignableFrom<IDataHubTypeMapper<DHLocation, BCLocation>>(location);
        Assert.IsAssignableFrom<IDataHubTypeMapper<DHProductVariant, BCItemVariant>>(variant);

        await Assert.ThrowsAsync<InvalidOperationException>(() => currency.MapAsync(new DHCurrency(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => term.MapAsync(new DHPaymentTerm(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => method.MapAsync(new DHPaymentMethod(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => unit.MapAsync(new DHUnitOfMeasure(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => location.MapAsync(new DHLocation(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => variant.MapAsync(new DHProductVariant(), CancellationToken.None));
    }

    private static void AssertExternal<TDataHub, TBusinessCentral>(EntityReference? reference, Guid id)
    {
        var external = Assert.IsType<ExternalEntityReference>(reference);
        Assert.Equal(typeof(TDataHub).Name, external.EntityType);
        Assert.Equal(typeof(TBusinessCentral).Name, external.SourceEntityType);
        Assert.Equal(id.ToString(), external.EntityId);
    }
}
