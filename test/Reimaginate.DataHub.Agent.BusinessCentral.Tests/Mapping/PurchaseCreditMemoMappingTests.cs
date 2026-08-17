using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemoLine;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemo;
using DataHubPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemoLine;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class PurchaseCreditMemoMappingTests
{
    [Fact]
    public void ContractUsesStandardRoutesMarkerDatesAndFailClosedCreates()
    {
        Assert.Equal("purchaseCreditMemos", Url<BusinessCentralPurchaseCreditMemo>());
        Assert.Equal("purchaseCreditMemoLines", Url<BusinessCentralPurchaseCreditMemoLine>());
        var parent = Assert.Single(typeof(BusinessCentralPurchaseCreditMemoLine)
            .GetCustomAttributes(typeof(BusinessCentralParentUrlAttribute), true)
            .Cast<BusinessCentralParentUrlAttribute>());
        Assert.Equal("purchaseCreditMemos", parent.ParentUrl);
        Assert.Equal(nameof(BusinessCentralPurchaseCreditMemoLine.DocumentId), parent.ParentIdPropertyName);
        Assert.Empty(new[] { typeof(BusinessCentralPurchaseCreditMemo), typeof(BusinessCentralPurchaseCreditMemoLine) }
            .SelectMany(type => type.GetProperties())
            .SelectMany(property => property.GetCustomAttributes(typeof(BusinessCentralCreateRecoveryKeyAttribute), true)));
        AssertDate(nameof(BusinessCentralPurchaseCreditMemo.CreditMemoDate));
        AssertDate(nameof(BusinessCentralPurchaseCreditMemo.PostingDate));
        AssertDate(nameof(BusinessCentralPurchaseCreditMemo.DueDate));
    }

    [Fact]
    public async Task HeaderMapsEditableReferencesAndOmitsBusinessCentralOwnedValues()
    {
        var source = NewHeader();
        source.OriginalPurchaseInvoice = new EntityReference
        {
            EntityType = typeof(DataHubPurchaseInvoice).Name,
            EntityId = "invoice-1"
        };
        var vendorId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var cache = Cache<DataHubSupplier>(source.Supplier!.EntityId, "vendor", vendorId);
        cache[typeof(DataHubPurchaseInvoice).Name] = CacheObjects("invoice-1", "purchaseinvoice", invoiceId);

        var result = await new MapPurchaseCreditMemoToBusinessCentralPurchaseCreditMemo()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(vendorId, result.VendorId);
        Assert.Equal(invoiceId, result.InvoiceId);
        Assert.Equal(source.VendorCreditMemoNumber, result.VendorCreditMemoNumber);
        Assert.Equal(source.CreditMemoDate, result.CreditMemoDate);
        Assert.Equal(source.PostingDate, result.PostingDate);
        Assert.Equal(source.DiscountAmount, result.DiscountAmount);
        Assert.Null(result.Number);
        Assert.Null(result.DueDate);
        Assert.Null(result.Status);
        Assert.Null(result.TotalAmountIncludingTax);
        Assert.DoesNotContain("dueDate", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UntrackedBlankVendorCreditNumberGetsStableBoundedReconciliationValue()
    {
        var source = NewHeader();
        source.id = "purchase-credit-memo-with-a-long-data-hub-id";
        source.VendorCreditMemoNumber = null;
        var cache = Cache<DataHubSupplier>(source.Supplier!.EntityId, "vendor", Guid.NewGuid());

        var first = await new MapPurchaseCreditMemoToBusinessCentralPurchaseCreditMemo()
            .MapAsync(source, CancellationToken.None, cache);
        var second = await new MapPurchaseCreditMemoToBusinessCentralPurchaseCreditMemo()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.StartsWith("DHIT-PCM-", first.VendorCreditMemoNumber, StringComparison.Ordinal);
        Assert.Equal(35, first.VendorCreditMemoNumber!.Length);
        Assert.Equal(first.VendorCreditMemoNumber, second.VendorCreditMemoNumber);
    }

    [Fact]
    public async Task HeaderInboundMapsReferencesAndCalculatedSnapshots()
    {
        var vendorId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var modified = DateTimeOffset.UtcNow;
        var source = new BusinessCentralPurchaseCreditMemo
        {
            Id = Guid.NewGuid().ToString(),
            Number = "PCM-1",
            VendorCreditMemoNumber = "DHIT-PCM-1",
            VendorId = vendorId,
            InvoiceId = invoiceId,
            CreditMemoDate = "2026-08-15",
            PostingDate = "2026-08-15",
            DueDate = "2026-09-15",
            Status = "Draft",
            TotalAmountIncludingTax = 110m,
            LastModifiedDateTime = modified
        };

        var result = await new MapBusinessCentralPurchaseCreditMemoToPurchaseCreditMemo()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Number, result.CreditMemoNumber);
        Assert.Equal(source.DueDate, result.DueDate);
        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.TotalAmountIncludingTax, result.TotalAmountIncludingTax);
        Assert.Equal(modified, result.lastUpdated);
        AssertReference<DataHubSupplier, BusinessCentralVendor>(result.Supplier, vendorId);
        AssertReference<DataHubPurchaseInvoice, BusinessCentralPurchaseInvoice>(result.OriginalPurchaseInvoice, invoiceId);
    }

    [Fact]
    public async Task LineMapsParentItemAndOneDiscountRepresentationBothWays()
    {
        var memoId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var source = new DataHubPurchaseCreditMemoLine
        {
            id = "line-1",
            PurchaseCreditMemo = new EntityReference { EntityType = typeof(DataHubPurchaseCreditMemo).Name, EntityId = "memo-1" },
            Product = new EntityReference { EntityType = typeof(DataHubProduct).Name, EntityId = "product-1" },
            Description = "Returned item",
            Quantity = 2m,
            UnitCost = 50m,
            DiscountAmount = 5m
        };
        var cache = Cache<DataHubPurchaseCreditMemo>("memo-1", "purchasecreditmemo", memoId);
        cache[typeof(DataHubProduct).Name] = CacheObjects("product-1", "item", itemId);

        var outbound = await new MapPurchaseCreditMemoLineToBusinessCentralPurchaseCreditMemoLine()
            .MapAsync(source, CancellationToken.None, cache);
        var inbound = await new MapBusinessCentralPurchaseCreditMemoLineToPurchaseCreditMemoLine()
            .MapAsync(new BusinessCentralPurchaseCreditMemoLine
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = memoId,
                ItemId = itemId,
                Sequence = 10000,
                Quantity = 2m,
                UnitCost = 50m,
                DiscountAmount = 5m,
                AmountIncludingTax = 104.5m
            }, CancellationToken.None);

        Assert.Equal(memoId, outbound.DocumentId);
        Assert.Equal(itemId, outbound.ItemId);
        Assert.Equal("Item", outbound.LineType);
        Assert.Equal(5m, outbound.DiscountAmount);
        Assert.DoesNotContain("discountPercent", outbound.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(10000, inbound.Sequence);
        Assert.Equal(104.5m, inbound.AmountIncludingTax);
        AssertReference<DataHubPurchaseCreditMemo, BusinessCentralPurchaseCreditMemo>(inbound.PurchaseCreditMemo, memoId);
        AssertReference<DataHubProduct, BusinessCentralItem>(inbound.Product, itemId);
    }

    private static DataHubPurchaseCreditMemo NewHeader() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        VendorCreditMemoNumber = "DHIT-PCM-UNIT",
        CreditMemoDate = "2026-08-15",
        PostingDate = "2026-08-15",
        Supplier = new EntityReference { EntityType = typeof(DataHubSupplier).Name, EntityId = "supplier-1" },
        DiscountAmount = 5m
    };

    private static string Url<T>() => Assert.Single(typeof(T)
        .GetCustomAttributes(typeof(BusinessCentralUrlAttribute), true)
        .Cast<BusinessCentralUrlAttribute>()).Url;

    private static void AssertDate(string name) => Assert.Single(typeof(BusinessCentralPurchaseCreditMemo)
        .GetProperty(name)!.GetCustomAttributes(typeof(BusinessCentralDateAttribute), true));

    private static void AssertReference<TDataHub, TBusinessCentral>(EntityReference? reference, Guid id)
    {
        var external = Assert.IsType<ExternalEntityReference>(reference);
        Assert.Equal(typeof(TDataHub).Name, external.EntityType);
        Assert.Equal(typeof(TBusinessCentral).Name, external.SourceEntityType);
        Assert.Equal(id.ToString(), external.EntityId);
    }

    private static Dictionary<string, object> Cache<T>(string id, string type, Guid externalId) where T : DataHubEntity =>
        new() { [typeof(T).Name] = CacheObjects(id, type, externalId) };

    private static List<JObject> CacheObjects(string id, string type, Guid externalId) =>
    [
        new JObject
        {
            [nameof(DataHubEntity.id)] = id,
            [nameof(DataHubEntity.alternateKeys)] = new JArray
            {
                new JObject
                {
                    [nameof(AlternateKey.Key)] = $"businesscentral.{type}",
                    [nameof(AlternateKey.Value)] = externalId.ToString()
                }
            }
        }
    ];
}
