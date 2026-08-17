using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using BCItem = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.Item;
using BCCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.Customer;
using BCSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrder;
using BCSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrderLine;
using DHAccount = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.Account;
using DHProduct = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.Product;
using DHSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrder;
using DHSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Mapping;

public sealed class MapAccountToCustomer :
    ITypeMapper<DHAccount, BCCustomer>,
    IDataHubTypeMapper<DHAccount, BCCustomer>
{
    public List<string> MappedEntityReferences { get; } = [];

    public Task<BCCustomer> MapAsync(
        DHAccount from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from.Name);
        return Task.FromResult(new BCCustomer
        {
            Number = from.AccountNumber ?? ReferenceMapping.CreateNumber("C", from.id),
            DisplayName = from.Name,
            Email = from.Email,
            PhoneNumber = from.Phone
        });
    }
}

public sealed class MapCustomerToAccount : ITypeMapper<BCCustomer, DHAccount>
{
    public Task<DHAccount> MapAsync(
        BCCustomer from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHAccount
        {
            id = from.Id!,
            createdOn = from.LastModifiedAt ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedAt ?? DateTimeOffset.UtcNow,
            AccountNumber = from.Number,
            Name = from.DisplayName,
            Email = from.Email,
            Phone = from.PhoneNumber
        });
}

public sealed class MapProductToItem :
    ITypeMapper<DHProduct, BCItem>,
    IDataHubTypeMapper<DHProduct, BCItem>
{
    public List<string> MappedEntityReferences { get; } = [];

    public Task<BCItem> MapAsync(
        DHProduct from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from.Name);
        return Task.FromResult(new BCItem
        {
            Number = from.ProductNumber ?? ReferenceMapping.CreateNumber("I", from.id),
            DisplayName = from.Name,
            Type = "Service",
            UnitPrice = from.UnitPrice
        });
    }
}

public sealed class MapItemToProduct : ITypeMapper<BCItem, DHProduct>
{
    public Task<DHProduct> MapAsync(
        BCItem from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHProduct
        {
            id = from.Id!,
            createdOn = from.LastModifiedAt ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedAt ?? DateTimeOffset.UtcNow,
            ProductNumber = from.Number,
            Name = from.DisplayName,
            UnitPrice = from.UnitPrice
        });
}

public sealed class MapSalesOrderToBusinessCentral :
    ITypeMapper<DHSalesOrder, BCSalesOrder>,
    IDataHubTypeMapper<DHSalesOrder, BCSalesOrder>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DHSalesOrder.Customer)];

    public Task<BCSalesOrder> MapAsync(
        DHSalesOrder from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var customerId = ReferenceMapping.ResolveBusinessCentralId<DHAccount, BCCustomer>(from.Customer, cache)
            ?? throw new InvalidOperationException("The sales order customer has no Business Central customer id.");
        return Task.FromResult(new BCSalesOrder
        {
            DataHubCorrelationId = ReferenceMapping.CreateCorrelationId(nameof(DHSalesOrder), from.id),
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            OrderDate = from.OrderDate,
            CustomerId = customerId
        });
    }
}

public sealed class MapBusinessCentralToSalesOrder : ITypeMapper<BCSalesOrder, DHSalesOrder>
{
    public Task<DHSalesOrder> MapAsync(
        BCSalesOrder from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHSalesOrder
        {
            id = from.Id!,
            createdOn = from.LastModifiedAt ?? DateTimeOffset.UtcNow,
            lastUpdated = from.LastModifiedAt ?? DateTimeOffset.UtcNow,
            OrderNumber = from.Number,
            ExternalDocumentNumber = from.ExternalDocumentNumber,
            OrderDate = from.OrderDate,
            Customer = ReferenceMapping.ToDataHubReference<DHAccount, BCCustomer>(from.CustomerId),
            Status = from.Status,
            TotalAmountIncludingTax = from.TotalAmountIncludingTax
        });
}

public sealed class MapSalesOrderLineToBusinessCentral :
    ITypeMapper<DHSalesOrderLine, BCSalesOrderLine>,
    IDataHubTypeMapper<DHSalesOrderLine, BCSalesOrderLine>
{
    public List<string> MappedEntityReferences { get; } =
        [nameof(DHSalesOrderLine.SalesOrder), nameof(DHSalesOrderLine.Product)];

    public Task<BCSalesOrderLine> MapAsync(
        DHSalesOrderLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var documentId = ReferenceMapping.ResolveBusinessCentralId<DHSalesOrder, BCSalesOrder>(from.SalesOrder, cache)
            ?? throw new InvalidOperationException("The line has no Business Central sales order id.");
        var itemId = ReferenceMapping.ResolveBusinessCentralId<DHProduct, BCItem>(from.Product, cache)
            ?? throw new InvalidOperationException("The line has no Business Central item id.");
        return Task.FromResult(new BCSalesOrderLine
        {
            DataHubCorrelationId = ReferenceMapping.CreateCorrelationId(nameof(DHSalesOrderLine), from.id),
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Description = from.Description,
            Quantity = from.Quantity,
            UnitPrice = from.UnitPrice
        });
    }
}

public sealed class MapBusinessCentralToSalesOrderLine : ITypeMapper<BCSalesOrderLine, DHSalesOrderLine>
{
    public Task<DHSalesOrderLine> MapAsync(
        BCSalesOrderLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHSalesOrderLine
        {
            id = from.Id!,
            createdOn = DateTimeOffset.UnixEpoch,
            lastUpdated = DateTimeOffset.UnixEpoch,
            SalesOrder = ReferenceMapping.ToDataHubReference<DHSalesOrder, BCSalesOrder>(from.DocumentId),
            Product = ReferenceMapping.ToDataHubReference<DHProduct, BCItem>(from.ItemId),
            Sequence = from.Sequence,
            Description = from.Description,
            Quantity = from.Quantity,
            UnitPrice = from.UnitPrice,
            AmountIncludingTax = from.AmountIncludingTax
        });
}

public static class ReferenceMapping
{
    public static string CreateNumber(string prefix, string id)
    {
        var suffix = new string(id.Where(char.IsLetterOrDigit).Take(20).ToArray());
        return $"DH-{prefix}-{suffix}".ToUpperInvariant();
    }

    public static Guid CreateCorrelationId(string entityType, string id)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{entityType}:{id}"));
        var bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes);
    }

    public static ExternalEntityReference? ToDataHubReference<TDataHub, TBusinessCentral>(Guid? id)
        where TDataHub : DataHubEntity
        where TBusinessCentral : BusinessCentralDocument
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return null;
        }
        return new ExternalEntityReference
        {
            DataSource = "BusinessCentral",
            EntityType = typeof(TDataHub).Name,
            SourceEntityType = typeof(TBusinessCentral).Name,
            EntityId = id.Value.ToString()
        };
    }

    public static Guid? ResolveBusinessCentralId<TDataHub, TBusinessCentral>(
        EntityReference? reference,
        Dictionary<string, object>? cache)
        where TDataHub : DataHubEntity
        where TBusinessCentral : BusinessCentralDocument
    {
        if (reference is ExternalEntityReference external &&
            external.DataSource.Equals("BusinessCentral", StringComparison.OrdinalIgnoreCase) &&
            external.SourceEntityType.Equals(typeof(TBusinessCentral).Name, StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(external.EntityId, out var externalId))
        {
            return externalId;
        }
        if (reference is null || cache?.TryGetValue(typeof(TDataHub).Name, out var value) != true ||
            value is not IEnumerable<JObject> entities)
        {
            return null;
        }

        var expectedKey = $"BusinessCentral.{typeof(TBusinessCentral).Name}";
        var entity = entities.FirstOrDefault(candidate =>
            candidate.Value<string>(nameof(DataHubEntity.id)) == reference.EntityId);
        var alternateId = entity?[nameof(DataHubEntity.alternateKeys)]?
            .Children<JObject>()
            .Where(key => string.Equals(
                key.Value<string>(nameof(AlternateKey.Key)) ?? key.Value<string>("key"),
                expectedKey,
                StringComparison.OrdinalIgnoreCase))
            .Select(key => key.Value<string>(nameof(AlternateKey.Value)) ?? key.Value<string>("value"))
            .FirstOrDefault();
        return Guid.TryParse(alternateId, out var resolved) ? resolved : null;
    }
}
