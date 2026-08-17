using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BCCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Currency;
using BCItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BCItemVariant = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.ItemVariant;
using BCLocation = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Location;
using BCPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentMethod;
using BCPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentTerm;
using BCUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.UnitOfMeasure;
using DHCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Currency;
using DHLocation = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.InventoryLocation;
using DHPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PaymentMethod;
using DHPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PaymentTerm;
using DHProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DHProductVariant = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.ProductVariant;
using DHUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.UnitOfMeasure;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapBusinessCentralCurrencyToCurrency : ITypeMapper<BCCurrency, DHCurrency>
{
    public Task<DHCurrency> MapAsync(BCCurrency from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHCurrency
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code, DisplayName = from.DisplayName, Symbol = from.Symbol,
            AmountDecimalPlaces = from.AmountDecimalPlaces, AmountRoundingPrecision = from.AmountRoundingPrecision
        });
}

public sealed class MapBusinessCentralPaymentTermToPaymentTerm : ITypeMapper<BCPaymentTerm, DHPaymentTerm>
{
    public Task<DHPaymentTerm> MapAsync(BCPaymentTerm from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHPaymentTerm
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code, DisplayName = from.DisplayName, DueDateCalculation = from.DueDateCalculation,
            DiscountDateCalculation = from.DiscountDateCalculation, DiscountPercent = from.DiscountPercent,
            CalculateDiscountOnCreditMemos = from.CalculateDiscountOnCreditMemos
        });
}

public sealed class MapBusinessCentralPaymentMethodToPaymentMethod : ITypeMapper<BCPaymentMethod, DHPaymentMethod>
{
    public Task<DHPaymentMethod> MapAsync(BCPaymentMethod from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHPaymentMethod
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code, DisplayName = from.DisplayName
        });
}

public sealed class MapBusinessCentralUnitOfMeasureToUnitOfMeasure : ITypeMapper<BCUnitOfMeasure, DHUnitOfMeasure>
{
    public Task<DHUnitOfMeasure> MapAsync(BCUnitOfMeasure from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHUnitOfMeasure
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code, DisplayName = from.DisplayName,
            InternationalStandardCode = from.InternationalStandardCode, Symbol = from.Symbol
        });
}

public sealed class MapBusinessCentralLocationToInventoryLocation : ITypeMapper<BCLocation, DHLocation>
{
    public Task<DHLocation> MapAsync(BCLocation from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHLocation
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Code = from.Code, DisplayName = from.DisplayName, Contact = from.Contact,
            AddressLine1 = from.AddressLine1, AddressLine2 = from.AddressLine2, City = from.City,
            StateOrProvince = from.State, Country = from.Country, PostalCode = from.PostalCode,
            PhoneNumber = from.PhoneNumber, Email = from.Email, Website = from.Website
        });
}

public sealed class MapBusinessCentralItemVariantToProductVariant : ITypeMapper<BCItemVariant, DHProductVariant>
{
    public Task<DHProductVariant> MapAsync(BCItemVariant from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHProductVariant
        {
            id = from.Id!, createdOn = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Product = BusinessCentralMappingHelpers.ToDataHubReference<DHProduct, BCItem>(from.ItemId),
            ItemNumber = from.ItemNumber, Code = from.Code, Description = from.Description
        });
}

public sealed class MapCurrencyToBusinessCentralCurrency :
    ITypeMapper<DHCurrency, BCCurrency>,
    IDataHubTypeMapper<DHCurrency, BCCurrency>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BCCurrency> MapAsync(DHCurrency from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw ReadOnly(nameof(BCCurrency));
    private static InvalidOperationException ReadOnly(string type) => new($"Business Central {type} reference data is inbound-only in the reference solution.");
}

public sealed class MapPaymentTermToBusinessCentralPaymentTerm :
    ITypeMapper<DHPaymentTerm, BCPaymentTerm>,
    IDataHubTypeMapper<DHPaymentTerm, BCPaymentTerm>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BCPaymentTerm> MapAsync(DHPaymentTerm from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new InvalidOperationException("Business Central PaymentTerm reference data is inbound-only in the reference solution.");
}

public sealed class MapPaymentMethodToBusinessCentralPaymentMethod :
    ITypeMapper<DHPaymentMethod, BCPaymentMethod>,
    IDataHubTypeMapper<DHPaymentMethod, BCPaymentMethod>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BCPaymentMethod> MapAsync(DHPaymentMethod from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new InvalidOperationException("Business Central PaymentMethod reference data is inbound-only in the reference solution.");
}

public sealed class MapUnitOfMeasureToBusinessCentralUnitOfMeasure :
    ITypeMapper<DHUnitOfMeasure, BCUnitOfMeasure>,
    IDataHubTypeMapper<DHUnitOfMeasure, BCUnitOfMeasure>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BCUnitOfMeasure> MapAsync(DHUnitOfMeasure from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new InvalidOperationException("Business Central UnitOfMeasure reference data is inbound-only in the reference solution.");
}

public sealed class MapInventoryLocationToBusinessCentralLocation :
    ITypeMapper<DHLocation, BCLocation>,
    IDataHubTypeMapper<DHLocation, BCLocation>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BCLocation> MapAsync(DHLocation from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new InvalidOperationException("Business Central Location reference data is inbound-only in the reference solution.");
}

public sealed class MapProductVariantToBusinessCentralItemVariant :
    ITypeMapper<DHProductVariant, BCItemVariant>,
    IDataHubTypeMapper<DHProductVariant, BCItemVariant>
{
    public List<string> MappedEntityReferences { get; } = [];
    public Task<BCItemVariant> MapAsync(DHProductVariant from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        throw new InvalidOperationException("Business Central ItemVariant reference data is inbound-only in the reference solution.");
}
