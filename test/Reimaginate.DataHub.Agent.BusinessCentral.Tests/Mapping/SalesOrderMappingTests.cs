using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrder;
using BusinessCentralSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrderLine;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Contact;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrder;
using DataHubSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class SalesOrderMappingTests
{
    [Theory]
    [InlineData(typeof(DataHubAccount), "Customer")]
    [InlineData(typeof(DataHubContact), "Contact")]
    [InlineData(typeof(DataHubProduct), "Item")]
    [InlineData(typeof(DataHubSalesOrder), "SalesOrder")]
    [InlineData(typeof(DataHubSalesOrderLine), "SalesOrderLine")]
    public void DependencyModelsDeclareBusinessCentralRelatedTypes(Type dataHubType, string businessCentralType)
    {
        var attribute = Assert.Single(dataHubType
            .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), inherit: true)
            .Cast<RelatedEntityTypeAttribute>());

        Assert.Equal("BusinessCentral", attribute.DataSource);
        Assert.Equal(businessCentralType, attribute.TypeName);
    }

    [Fact]
    public async Task DataHubSalesOrderResolvesCustomerAndMapsOnlyOwnedFields()
    {
        var customerId = Guid.NewGuid();
        var source = NewDataHubSalesOrder();
        var cache = Cache<DataHubAccount>(source.Customer!.EntityId, "customer", customerId);

        var result = await new MapSalesOrderToBusinessCentralSalesOrder()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(
            BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubSalesOrder), source.id),
            result.DataHubCorrelationId);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(source.ExternalDocumentNumber, result.ExternalDocumentNumber);
        Assert.Equal(source.OrderDate, result.OrderDate);
        Assert.Equal(source.PhoneNumber, result.PhoneNumber);
        Assert.Equal(source.Email, result.Email);
        Assert.Null(result.Number);
        Assert.DoesNotContain("number", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Null(result.Status);
        Assert.Null(result.TotalAmountExcludingTax);
        Assert.Null(result.TotalTaxAmount);
        Assert.Null(result.TotalAmountIncludingTax);
    }

    [Fact]
    public async Task BusinessCentralSalesOrderMapsReferencesAndCalculatedFields()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var modified = new DateTimeOffset(2026, 8, 14, 1, 2, 3, TimeSpan.Zero);
        var source = new BusinessCentralSalesOrder
        {
            Id = id.ToString(),
            Number = "SO-100",
            ExternalDocumentNumber = "PO-200",
            OrderDate = "2026-08-14",
            CustomerId = customerId,
            PhoneNumber = "+61 2 9000 0000",
            Email = "orders@example.test",
            Status = "Draft",
            TotalAmountExcludingTax = 100m,
            TotalTaxAmount = 10m,
            TotalAmountIncludingTax = 110m,
            LastModifiedDateTime = modified
        };

        var result = await new MapBusinessCentralSalesOrderToSalesOrder()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(modified, result.createdOn);
        Assert.Equal(modified, result.lastUpdated);
        Assert.Equal(source.Number, result.OrderNumber);
        Assert.Equal(source.ExternalDocumentNumber, result.ExternalDocumentNumber);
        Assert.Equal(source.OrderDate, result.OrderDate);
        var customerReference = Assert.IsType<ExternalEntityReference>(result.Customer);
        Assert.Equal(typeof(DataHubAccount).Name, customerReference.EntityType);
        Assert.Equal(typeof(BusinessCentralCustomer).Name, customerReference.SourceEntityType);
        Assert.Equal("BusinessCentral", customerReference.DataSource);
        Assert.Equal(customerId.ToString(), customerReference.EntityId);
        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.TotalAmountIncludingTax, result.TotalAmountIncludingTax);
    }

    [Fact]
    public async Task DataHubSalesOrderLineResolvesOrderAndProductReferences()
    {
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var source = NewDataHubSalesOrderLine();
        var cache = Cache<DataHubSalesOrder>(source.SalesOrder!.EntityId, "salesorder", orderId);
        cache[typeof(DataHubProduct).Name] = CacheObjects(
            source.Product!.EntityId,
            "item",
            itemId);

        var result = await new MapSalesOrderLineToBusinessCentralSalesOrderLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(
            BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubSalesOrderLine), source.id),
            result.DataHubCorrelationId);
        Assert.Equal(orderId, result.DocumentId);
        Assert.Equal(itemId, result.ItemId);
        Assert.Equal("Item", result.LineType);
        Assert.Equal(source.Description, result.Description);
        Assert.Equal(source.Description2, result.Description2);
        Assert.Equal(source.Quantity, result.Quantity);
        Assert.Equal(source.UnitPrice, result.UnitPrice);
        Assert.Equal(source.DiscountPercent, result.DiscountPercent);
        Assert.Null(result.DiscountAmount);
        Assert.DoesNotContain("discountAmount", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Null(result.Sequence);
        Assert.Null(result.AmountExcludingTax);
        Assert.Null(result.TotalTaxAmount);
        Assert.Null(result.AmountIncludingTax);
    }

    [Fact]
    public async Task SalesOrderLineMapsOnlyOneDiscountRepresentation()
    {
        var source = NewDataHubSalesOrderLine();
        source.DiscountPercent = null;
        source.DiscountAmount = 12.34m;
        var cache = SalesOrderLineCache(source);

        var amountResult = await new MapSalesOrderLineToBusinessCentralSalesOrderLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(12.34m, amountResult.DiscountAmount);
        Assert.DoesNotContain("discountPercent", amountResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);

        source.DiscountAmount = null;
        var clearedResult = await new MapSalesOrderLineToBusinessCentralSalesOrderLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(0m, clearedResult.DiscountPercent);
        Assert.DoesNotContain("discountAmount", clearedResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessCentralSalesOrderLineMapsReferencesAndCalculatedAmounts()
    {
        var documentId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var source = new BusinessCentralSalesOrderLine
        {
            Id = Guid.NewGuid().ToString(),
            DocumentId = documentId,
            ItemId = itemId,
            Sequence = 10000,
            Description = "Consulting",
            Description2 = "August",
            Quantity = 2m,
            UnitPrice = 100m,
            DiscountPercent = 5m,
            AmountExcludingTax = 190m,
            TotalTaxAmount = 19m,
            AmountIncludingTax = 209m
        };

        var result = await new MapBusinessCentralSalesOrderLineToSalesOrderLine()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(DateTimeOffset.UnixEpoch, result.createdOn);
        var orderReference = Assert.IsType<ExternalEntityReference>(result.SalesOrder);
        Assert.Equal(typeof(DataHubSalesOrder).Name, orderReference.EntityType);
        Assert.Equal(typeof(BusinessCentralSalesOrder).Name, orderReference.SourceEntityType);
        Assert.Equal(documentId.ToString(), orderReference.EntityId);
        var productReference = Assert.IsType<ExternalEntityReference>(result.Product);
        Assert.Equal(typeof(DataHubProduct).Name, productReference.EntityType);
        Assert.Equal(typeof(BusinessCentralItem).Name, productReference.SourceEntityType);
        Assert.Equal(itemId.ToString(), productReference.EntityId);
        Assert.Equal(source.AmountExcludingTax, result.AmountExcludingTax);
        Assert.Equal(source.TotalTaxAmount, result.TotalTaxAmount);
        Assert.Equal(source.AmountIncludingTax, result.AmountIncludingTax);
    }

    [Fact]
    public async Task MissingRequiredSalesOrderReferencesFailClearly()
    {
        var order = NewDataHubSalesOrder();
        order.Customer = null;
        var line = NewDataHubSalesOrderLine();
        line.Product = null;

        var orderFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapSalesOrderToBusinessCentralSalesOrder().MapAsync(order, CancellationToken.None));
        var lineFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapSalesOrderLineToBusinessCentralSalesOrderLine().MapAsync(line, CancellationToken.None));

        Assert.Contains("reference a customer", orderFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("both a sales order and a product", lineFailure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StableCorrelationIdsAreRepeatableAndEntityScoped()
    {
        const string dataHubId = "4a0da9a4f46f45d087b2ce8d3441544c";

        var first = BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubSalesOrder), dataHubId);
        var repeated = BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubSalesOrder), dataHubId);
        var line = BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubSalesOrderLine), dataHubId);

        Assert.NotEqual(Guid.Empty, first);
        Assert.Equal(first, repeated);
        Assert.NotEqual(first, line);
    }

    [Fact]
    public void StableCorrelationIdsRejectMissingDataHubIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubSalesOrder), " "));
    }

    private static DataHubSalesOrder NewDataHubSalesOrder()
    {
        return new DataHubSalesOrder
        {
            id = Guid.NewGuid().ToString("N"),
            ExternalDocumentNumber = "DHIT-PO-100",
            OrderDate = "2026-08-14",
            Customer = new EntityReference
            {
                EntityType = typeof(DataHubAccount).Name,
                EntityId = "account-1"
            },
            PhoneNumber = "+61 2 9000 0000",
            Email = "orders@example.test"
        };
    }

    private static DataHubSalesOrderLine NewDataHubSalesOrderLine()
    {
        return new DataHubSalesOrderLine
        {
            id = Guid.NewGuid().ToString("N"),
            SalesOrder = new EntityReference
            {
                EntityType = typeof(DataHubSalesOrder).Name,
                EntityId = "order-1"
            },
            Product = new EntityReference
            {
                EntityType = typeof(DataHubProduct).Name,
                EntityId = "product-1"
            },
            Description = "Consulting",
            Description2 = "August",
            Quantity = 2m,
            UnitPrice = 100m,
            DiscountPercent = 5m
        };
    }

    private static Dictionary<string, object> SalesOrderLineCache(DataHubSalesOrderLine source)
    {
        var cache = Cache<DataHubSalesOrder>(source.SalesOrder!.EntityId, "salesorder", Guid.NewGuid());
        cache[typeof(DataHubProduct).Name] = CacheObjects(
            source.Product!.EntityId,
            "item",
            Guid.NewGuid());
        return cache;
    }

    private static Dictionary<string, object> Cache<TDataHubEntity>(
        string dataHubId,
        string businessCentralType,
        Guid businessCentralId)
        where TDataHubEntity : DataHubEntity
    {
        return new Dictionary<string, object>
        {
            [typeof(TDataHubEntity).Name] = CacheObjects(
                dataHubId,
                businessCentralType,
                businessCentralId)
        };
    }

    private static List<JObject> CacheObjects(
        string dataHubId,
        string businessCentralType,
        Guid businessCentralId)
    {
        return
        [
            new JObject
            {
                [nameof(DataHubEntity.id)] = dataHubId,
                [nameof(DataHubEntity.alternateKeys)] = new JArray
                {
                    new JObject
                    {
                        [nameof(AlternateKey.Key)] = $"businesscentral.{businessCentralType}",
                        [nameof(AlternateKey.Value)] = businessCentralId.ToString()
                    }
                }
            }
        ];
    }
}
