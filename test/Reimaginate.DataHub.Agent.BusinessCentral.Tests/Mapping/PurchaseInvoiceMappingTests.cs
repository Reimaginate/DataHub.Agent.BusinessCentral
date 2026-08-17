using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoiceLine;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class PurchaseInvoiceMappingTests
{
    [Theory]
    [InlineData(typeof(DataHubPurchaseInvoice), "PurchaseInvoice")]
    [InlineData(typeof(DataHubPurchaseInvoiceLine), "PurchaseInvoiceLine")]
    public void InvoiceModelsDeclareBusinessCentralRelatedTypes(Type dataHubType, string businessCentralType)
    {
        var attribute = Assert.Single(dataHubType
            .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), inherit: true)
            .Cast<RelatedEntityTypeAttribute>());

        Assert.Equal("BusinessCentral", attribute.DataSource);
        Assert.Equal(businessCentralType, attribute.TypeName);
    }

    [Fact]
    public void PurchaseInvoiceRoutesDeclareRecoveryAndParentMetadata()
    {
        var recoveryKey = Assert.Single(typeof(BusinessCentralPurchaseInvoice)
            .GetProperty(nameof(BusinessCentralPurchaseInvoice.VendorInvoiceNumber))!
            .GetCustomAttributes(typeof(BusinessCentralCreateRecoveryKeyAttribute), inherit: true)
            .Cast<BusinessCentralCreateRecoveryKeyAttribute>());
        var parentRoute = Assert.Single(typeof(BusinessCentralPurchaseInvoiceLine)
            .GetCustomAttributes(typeof(BusinessCentralParentUrlAttribute), inherit: true)
            .Cast<BusinessCentralParentUrlAttribute>());

        Assert.Equal("vendorInvoiceNumber", recoveryKey.FieldName);
        Assert.Equal("purchaseInvoices", parentRoute.ParentUrl);
        Assert.Equal(nameof(BusinessCentralPurchaseInvoiceLine.DocumentId), parentRoute.ParentIdPropertyName);
    }

    [Fact]
    public async Task DataHubInvoiceMapsOnlyEditableDraftFields()
    {
        var vendorId = Guid.NewGuid();
        var source = NewInvoice();

        var result = await new MapPurchaseInvoiceToBusinessCentralPurchaseInvoice().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubSupplier>(source.Supplier!.EntityId, "vendor", vendorId));

        Assert.Equal(vendorId, result.VendorId);
        Assert.Equal(source.VendorInvoiceNumber, result.VendorInvoiceNumber);
        Assert.Equal(source.PostingDate, result.PostingDate);
        Assert.Equal(source.InvoiceDate, result.InvoiceDate);
        Assert.Equal(source.DueDate, result.DueDate);
        Assert.Null(result.Number);
        Assert.Null(result.Status);
        Assert.Null(result.PricesIncludeTax);
        Assert.Null(result.DiscountAmount);
        Assert.Null(result.TotalAmountIncludingTax);
        Assert.DoesNotContain("number", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("discountAmount", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessCentralInvoiceMapsSupplierAndCalculatedFields()
    {
        var vendorId = Guid.NewGuid();
        var source = new BusinessCentralPurchaseInvoice
        {
            Id = Guid.NewGuid().ToString(),
            Number = "PINV-100",
            VendorInvoiceNumber = "DHIT-PINV-100",
            PostingDate = "2026-08-14",
            InvoiceDate = "2026-08-14",
            DueDate = "2026-09-13",
            VendorId = vendorId,
            Status = "Draft",
            PricesIncludeTax = false,
            DiscountAmount = 5m,
            DiscountAppliedBeforeTax = true,
            TotalAmountExcludingTax = 95m,
            TotalTaxAmount = 9.5m,
            TotalAmountIncludingTax = 104.5m,
            LastModifiedDateTime = new DateTimeOffset(2026, 8, 14, 1, 2, 3, TimeSpan.Zero)
        };

        var result = await new MapBusinessCentralPurchaseInvoiceToPurchaseInvoice()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Number, result.InvoiceNumber);
        Assert.Equal(source.VendorInvoiceNumber, result.VendorInvoiceNumber);
        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.DiscountAppliedBeforeTax, result.DiscountAppliedBeforeTax);
        Assert.Equal(source.TotalAmountIncludingTax, result.TotalAmountIncludingTax);
        var supplier = Assert.IsType<ExternalEntityReference>(result.Supplier);
        Assert.Equal(typeof(DataHubSupplier).Name, supplier.EntityType);
        Assert.Equal(typeof(BusinessCentralVendor).Name, supplier.SourceEntityType);
        Assert.Equal(vendorId.ToString(), supplier.EntityId);
    }

    [Fact]
    public async Task InvoiceLineMapsReferencesUnitCostAndOneDiscountRepresentation()
    {
        var source = NewLine();
        var invoiceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cache = Cache<DataHubPurchaseInvoice>(
            source.PurchaseInvoice!.EntityId,
            "purchaseinvoice",
            invoiceId);
        cache[typeof(DataHubProduct).Name] = CacheObjects(source.Product!.EntityId, "item", itemId);

        var result = await new MapPurchaseInvoiceLineToBusinessCentralPurchaseInvoiceLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(invoiceId, result.DocumentId);
        Assert.Equal(itemId, result.ItemId);
        Assert.Equal("Item", result.LineType);
        Assert.Equal(source.UnitCost, result.UnitCost);
        Assert.Equal(source.DiscountPercent, result.DiscountPercent);
        Assert.DoesNotContain("discountAmount", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Null(result.Sequence);
        Assert.Null(result.AmountIncludingTax);
    }

    [Fact]
    public async Task MissingInvoiceReferencesFailClearly()
    {
        var invoice = NewInvoice();
        invoice.Supplier = null;
        var line = NewLine();
        line.Product = null;

        var invoiceFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapPurchaseInvoiceToBusinessCentralPurchaseInvoice()
                .MapAsync(invoice, CancellationToken.None));
        var lineFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapPurchaseInvoiceLineToBusinessCentralPurchaseInvoiceLine()
                .MapAsync(line, CancellationToken.None));

        Assert.Contains("reference a supplier", invoiceFailure.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("both a purchase invoice and a product", lineFailure.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static DataHubPurchaseInvoice NewInvoice() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        VendorInvoiceNumber = "DHIT-PINV-100",
        PostingDate = "2026-08-14",
        InvoiceDate = "2026-08-14",
        DueDate = "2026-09-13",
        Supplier = new EntityReference
        {
            EntityType = typeof(DataHubSupplier).Name,
            EntityId = "supplier-1"
        },
        PricesIncludeTax = false,
        DiscountAmount = 5m
    };

    private static DataHubPurchaseInvoiceLine NewLine() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        PurchaseInvoice = new EntityReference
        {
            EntityType = typeof(DataHubPurchaseInvoice).Name,
            EntityId = "purchase-invoice-1"
        },
        Product = new EntityReference
        {
            EntityType = typeof(DataHubProduct).Name,
            EntityId = "product-1"
        },
        Description = "Draft purchase invoice item",
        Description2 = "August",
        Quantity = 2m,
        UnitCost = 100m,
        DiscountPercent = 5m,
        ExpectedReceiptDate = "2026-08-21"
    };

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
