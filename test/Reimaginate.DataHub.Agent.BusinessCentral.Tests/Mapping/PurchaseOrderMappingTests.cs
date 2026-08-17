using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrder;
using BusinessCentralPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrderLine;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrder;
using DataHubPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrderLine;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class PurchaseOrderMappingTests
{
    [Theory]
    [InlineData(typeof(DataHubPurchaseOrder), "PurchaseOrder")]
    [InlineData(typeof(DataHubPurchaseOrderLine), "PurchaseOrderLine")]
    public void PurchaseOrderModelsDeclareBusinessCentralRelatedTypes(
        Type dataHubType,
        string businessCentralType)
    {
        var attribute = Assert.Single(dataHubType
            .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), inherit: true)
            .Cast<RelatedEntityTypeAttribute>());

        Assert.Equal("BusinessCentral", attribute.DataSource);
        Assert.Equal(businessCentralType, attribute.TypeName);
    }

    [Fact]
    public async Task DataHubPurchaseOrderResolvesSupplierAndMapsOnlyOwnedFields()
    {
        var vendorId = Guid.NewGuid();
        var source = NewPurchaseOrder();

        var result = await new MapPurchaseOrderToBusinessCentralPurchaseOrder().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubSupplier>(source.Supplier!.EntityId, "vendor", vendorId));

        Assert.Equal(
            BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubPurchaseOrder), source.id),
            result.DataHubCorrelationId);
        Assert.Equal(vendorId, result.VendorId);
        Assert.Equal(source.OrderDate, result.OrderDate);
        Assert.Equal(source.PostingDate, result.PostingDate);
        Assert.Equal(source.RequestedReceiptDate, result.RequestedReceiptDate);
        Assert.Null(result.Number);
        Assert.Null(result.Status);
        Assert.Null(result.FullyReceived);
        Assert.Null(result.PricesIncludeTax);
        Assert.Null(result.DiscountAmount);
        Assert.Null(result.TotalAmountIncludingTax);
        Assert.DoesNotContain("number", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("discountAmount", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessCentralPurchaseOrderMapsSupplierAndManagedFields()
    {
        var vendorId = Guid.NewGuid();
        var modified = new DateTimeOffset(2026, 8, 14, 1, 2, 3, TimeSpan.Zero);
        var source = new BusinessCentralPurchaseOrder
        {
            Id = Guid.NewGuid().ToString(),
            Number = "PO-100",
            OrderDate = "2026-08-14",
            PostingDate = "2026-08-14",
            VendorId = vendorId,
            RequestedReceiptDate = "2026-08-21",
            DiscountAmount = 12.50m,
            PricesIncludeTax = false,
            Status = "Draft",
            FullyReceived = false,
            TotalAmountExcludingTax = 100m,
            TotalTaxAmount = 10m,
            TotalAmountIncludingTax = 110m,
            LastModifiedDateTime = modified
        };

        var result = await new MapBusinessCentralPurchaseOrderToPurchaseOrder()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(modified, result.createdOn);
        Assert.Equal(modified, result.lastUpdated);
        Assert.Equal(source.Number, result.OrderNumber);
        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.FullyReceived, result.FullyReceived);
        Assert.Equal(source.TotalAmountIncludingTax, result.TotalAmountIncludingTax);
        var supplier = Assert.IsType<ExternalEntityReference>(result.Supplier);
        Assert.Equal(typeof(DataHubSupplier).Name, supplier.EntityType);
        Assert.Equal(typeof(BusinessCentralVendor).Name, supplier.SourceEntityType);
        Assert.Equal("BusinessCentral", supplier.DataSource);
        Assert.Equal(vendorId.ToString(), supplier.EntityId);
    }

    [Fact]
    public async Task DataHubPurchaseOrderLineResolvesReferencesAndMapsOnlyOwnedFields()
    {
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var source = NewPurchaseOrderLine();
        var cache = Cache<DataHubPurchaseOrder>(source.PurchaseOrder!.EntityId, "purchaseorder", orderId);
        cache[typeof(DataHubProduct).Name] = CacheObjects(
            source.Product!.EntityId,
            "item",
            itemId);

        var result = await new MapPurchaseOrderLineToBusinessCentralPurchaseOrderLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(
            BusinessCentralMappingHelpers.CreateStableCorrelationId(nameof(DataHubPurchaseOrderLine), source.id),
            result.DataHubCorrelationId);
        Assert.Equal(orderId, result.DocumentId);
        Assert.Equal(itemId, result.ItemId);
        Assert.Equal("Item", result.LineType);
        Assert.Equal(source.Description, result.Description);
        Assert.Equal(source.Quantity, result.Quantity);
        Assert.Equal(source.DirectUnitCost, result.DirectUnitCost);
        Assert.Equal(source.ExpectedReceiptDate, result.ExpectedReceiptDate);
        Assert.Equal(source.DiscountPercent, result.DiscountPercent);
        Assert.DoesNotContain("discountAmount", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Null(result.Sequence);
        Assert.Null(result.AmountIncludingTax);
        Assert.Null(result.ReceiveQuantity);
        Assert.Null(result.InvoiceQuantity);
    }

    [Fact]
    public async Task PurchaseOrderLineMapsOnlyOneDiscountRepresentation()
    {
        var source = NewPurchaseOrderLine();
        source.DiscountPercent = null;
        source.DiscountAmount = 12.34m;
        var cache = PurchaseOrderLineCache(source);

        var amountResult = await new MapPurchaseOrderLineToBusinessCentralPurchaseOrderLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(12.34m, amountResult.DiscountAmount);
        Assert.DoesNotContain("discountPercent", amountResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);

        source.DiscountAmount = null;
        var clearedResult = await new MapPurchaseOrderLineToBusinessCentralPurchaseOrderLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(0m, clearedResult.DiscountPercent);
        Assert.DoesNotContain("discountAmount", clearedResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PurchaseOrderLineOmitsAnUndefinedCalculatedReceiptDate()
    {
        var source = NewPurchaseOrderLine();
        source.ExpectedReceiptDate = null;

        var result = await new MapPurchaseOrderLineToBusinessCentralPurchaseOrderLine()
            .MapAsync(source, CancellationToken.None, PurchaseOrderLineCache(source));

        Assert.DoesNotContain("expectedReceiptDate", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessCentralPurchaseOrderLineMapsReferencesAndManagedFields()
    {
        var documentId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var source = new BusinessCentralPurchaseOrderLine
        {
            Id = Guid.NewGuid().ToString(),
            DocumentId = documentId,
            ItemId = itemId,
            Sequence = 10000,
            Description = "Purchased service",
            Description2 = "August",
            Quantity = 2m,
            DirectUnitCost = 100m,
            DiscountPercent = 5m,
            AmountExcludingTax = 190m,
            TaxPercent = 10m,
            TotalTaxAmount = 19m,
            AmountIncludingTax = 209m,
            ExpectedReceiptDate = "2026-08-21",
            ReceivedQuantity = 1m,
            InvoicedQuantity = 0m
        };

        var result = await new MapBusinessCentralPurchaseOrderLineToPurchaseOrderLine()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(DateTimeOffset.UnixEpoch, result.createdOn);
        var order = Assert.IsType<ExternalEntityReference>(result.PurchaseOrder);
        Assert.Equal(typeof(DataHubPurchaseOrder).Name, order.EntityType);
        Assert.Equal(typeof(BusinessCentralPurchaseOrder).Name, order.SourceEntityType);
        Assert.Equal(documentId.ToString(), order.EntityId);
        var product = Assert.IsType<ExternalEntityReference>(result.Product);
        Assert.Equal(typeof(DataHubProduct).Name, product.EntityType);
        Assert.Equal(typeof(BusinessCentralItem).Name, product.SourceEntityType);
        Assert.Equal(itemId.ToString(), product.EntityId);
        Assert.Equal(source.DirectUnitCost, result.DirectUnitCost);
        Assert.Equal(source.AmountIncludingTax, result.AmountIncludingTax);
        Assert.Equal(source.ReceivedQuantity, result.ReceivedQuantity);
    }

    [Fact]
    public void PurchaseOrderLineDeclaresParentScopedMutationRoute()
    {
        var attribute = Assert.Single(typeof(BusinessCentralPurchaseOrderLine)
            .GetCustomAttributes(typeof(BusinessCentralParentUrlAttribute), inherit: true)
            .Cast<BusinessCentralParentUrlAttribute>());

        Assert.Equal("purchaseOrders", attribute.ParentUrl);
        Assert.Equal(nameof(BusinessCentralPurchaseOrderLine.DocumentId), attribute.ParentIdPropertyName);
    }

    [Fact]
    public async Task MissingPurchaseOrderReferencesFailClearly()
    {
        var order = NewPurchaseOrder();
        order.Supplier = null;
        var line = NewPurchaseOrderLine();
        line.Product = null;

        var orderFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapPurchaseOrderToBusinessCentralPurchaseOrder()
                .MapAsync(order, CancellationToken.None));
        var lineFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapPurchaseOrderLineToBusinessCentralPurchaseOrderLine()
                .MapAsync(line, CancellationToken.None));

        Assert.Contains("reference a supplier", orderFailure.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("both a purchase order and a product", lineFailure.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static DataHubPurchaseOrder NewPurchaseOrder() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        OrderNumber = "PO-100",
        OrderDate = "2026-08-14",
        PostingDate = "2026-08-14",
        RequestedReceiptDate = "2026-08-21",
        Supplier = new EntityReference
        {
            EntityType = typeof(DataHubSupplier).Name,
            EntityId = "supplier-1"
        },
        DiscountAmount = 12.50m,
        PricesIncludeTax = false,
        Status = "Draft",
        FullyReceived = false,
        TotalAmountIncludingTax = 110m
    };

    private static DataHubPurchaseOrderLine NewPurchaseOrderLine() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        PurchaseOrder = new EntityReference
        {
            EntityType = typeof(DataHubPurchaseOrder).Name,
            EntityId = "purchase-order-1"
        },
        Product = new EntityReference
        {
            EntityType = typeof(DataHubProduct).Name,
            EntityId = "product-1"
        },
        Description = "Purchased service",
        Description2 = "August",
        Quantity = 2m,
        DirectUnitCost = 100m,
        DiscountPercent = 5m,
        ExpectedReceiptDate = "2026-08-21"
    };

    private static Dictionary<string, object> PurchaseOrderLineCache(DataHubPurchaseOrderLine source)
    {
        var cache = Cache<DataHubPurchaseOrder>(
            source.PurchaseOrder!.EntityId,
            "purchaseorder",
            Guid.NewGuid());
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
        where TDataHubEntity : DataHubEntity => new()
        {
            [typeof(TDataHubEntity).Name] = CacheObjects(
                dataHubId,
                businessCentralType,
                businessCentralId)
        };

    private static List<JObject> CacheObjects(
        string dataHubId,
        string businessCentralType,
        Guid businessCentralId) =>
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
