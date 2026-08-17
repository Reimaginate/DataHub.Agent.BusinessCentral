using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemoLine;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemo;
using DataHubSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemoLine;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class SalesCreditMemoMappingTests
{
    [Theory]
    [InlineData(typeof(DataHubSalesCreditMemo), "SalesCreditMemo")]
    [InlineData(typeof(DataHubSalesCreditMemoLine), "SalesCreditMemoLine")]
    public void ModelsDeclareBusinessCentralRelatedTypes(Type dataHubType, string businessCentralType)
    {
        var attribute = Assert.Single(dataHubType
            .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), inherit: true)
            .Cast<RelatedEntityTypeAttribute>());

        Assert.Equal("BusinessCentral", attribute.DataSource);
        Assert.Equal(businessCentralType, attribute.TypeName);
    }

    [Fact]
    public void RoutesIncrementalMarkerDatesAndFailClosedCreateContractMatchStandardApi()
    {
        var headerRoute = Assert.Single(typeof(BusinessCentralSalesCreditMemo)
            .GetCustomAttributes(typeof(BusinessCentralUrlAttribute), inherit: true)
            .Cast<BusinessCentralUrlAttribute>());
        var lineRoute = Assert.Single(typeof(BusinessCentralSalesCreditMemoLine)
            .GetCustomAttributes(typeof(BusinessCentralUrlAttribute), inherit: true)
            .Cast<BusinessCentralUrlAttribute>());
        var parentRoute = Assert.Single(typeof(BusinessCentralSalesCreditMemoLine)
            .GetCustomAttributes(typeof(BusinessCentralParentUrlAttribute), inherit: true)
            .Cast<BusinessCentralParentUrlAttribute>());
        var lastModified = Assert.Single(typeof(BusinessCentralSalesCreditMemo)
            .GetCustomAttributes(typeof(BusinessCentralLastModifiedAttribute), inherit: true)
            .Cast<BusinessCentralLastModifiedAttribute>());

        Assert.Equal("salesCreditMemos", headerRoute.Url);
        Assert.Equal("salesCreditMemoLines", lineRoute.Url);
        Assert.Equal("salesCreditMemos", parentRoute.ParentUrl);
        Assert.Equal(nameof(BusinessCentralSalesCreditMemoLine.DocumentId), parentRoute.ParentIdPropertyName);
        Assert.Equal("lastModifiedDateTime", lastModified.PropertyName);

        var recoveryFields = new[]
            {
                typeof(BusinessCentralSalesCreditMemo),
                typeof(BusinessCentralSalesCreditMemoLine)
            }
            .SelectMany(type => type.GetProperties())
            .SelectMany(property => property
                .GetCustomAttributes(typeof(BusinessCentralCreateRecoveryKeyAttribute), inherit: true))
            .ToArray();
        Assert.Empty(recoveryFields);

        AssertDateProperty(typeof(BusinessCentralSalesCreditMemo), nameof(BusinessCentralSalesCreditMemo.CreditMemoDate));
        AssertDateProperty(typeof(BusinessCentralSalesCreditMemo), nameof(BusinessCentralSalesCreditMemo.PostingDate));
        AssertDateProperty(typeof(BusinessCentralSalesCreditMemo), nameof(BusinessCentralSalesCreditMemo.DueDate));
        AssertDateProperty(typeof(BusinessCentralSalesCreditMemoLine), nameof(BusinessCentralSalesCreditMemoLine.ShipmentDate));
    }

    [Fact]
    public async Task DataHubHeaderMapsOnlyEditableFieldsAndOptionalOriginalInvoice()
    {
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var source = NewCreditMemo();
        source.OriginalSalesInvoice = new EntityReference
        {
            EntityType = typeof(DataHubSalesInvoice).Name,
            EntityId = "invoice-1"
        };
        var cache = Cache<DataHubAccount>(source.Customer!.EntityId, "customer", customerId);
        cache[typeof(DataHubSalesInvoice).Name] = CacheObjects(
            source.OriginalSalesInvoice.EntityId,
            "salesinvoice",
            invoiceId);

        var result = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(invoiceId, result.InvoiceId);
        Assert.Equal(source.ExternalDocumentNumber, result.ExternalDocumentNumber);
        Assert.Equal(source.CreditMemoDate, result.CreditMemoDate);
        Assert.Equal(source.PostingDate, result.PostingDate);
        Assert.Equal(source.PhoneNumber, result.PhoneNumber);
        Assert.Equal(source.Email, result.Email);
        Assert.Equal(source.DiscountAmount, result.DiscountAmount);
        Assert.Null(result.Number);
        Assert.Null(result.DueDate);
        Assert.Null(result.InvoiceNumber);
        Assert.Null(result.Status);
        Assert.Null(result.PricesIncludeTax);
        Assert.Null(result.DiscountAppliedBeforeTax);
        Assert.Null(result.TotalAmountExcludingTax);
        Assert.Null(result.TotalTaxAmount);
        Assert.Null(result.TotalAmountIncludingTax);
        Assert.DoesNotContain("number", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("dueDate", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("totalAmountIncludingTax", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingOptionalOriginalInvoiceIsNotIncludedInOutboundChangeState()
    {
        var source = NewCreditMemo();
        source.OriginalSalesInvoice = null;

        var result = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubAccount>(source.Customer!.EntityId, "customer", Guid.NewGuid()));

        Assert.Null(result.InvoiceId);
        Assert.DoesNotContain("invoiceId", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NullHeaderDiscountMapsToBusinessCentralNormalizedZero()
    {
        var source = NewCreditMemo();
        source.DiscountAmount = null;

        var result = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubAccount>(source.Customer!.EntityId, "customer", Guid.NewGuid()));

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Contains("discountAmount", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NullDateChangesRemainExplicitForBusinessCentralRequiredFieldValidation()
    {
        var header = NewCreditMemo();
        header.CreditMemoDate = null;
        header.PostingDate = null;
        var mappedHeader = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo().MapAsync(
            header,
            CancellationToken.None,
            Cache<DataHubAccount>(header.Customer!.EntityId, "customer", Guid.NewGuid()));

        var line = NewCreditMemoLine();
        line.ShipmentDate = null;
        var memoId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var lineCache = Cache<DataHubSalesCreditMemo>(
            line.SalesCreditMemo!.EntityId,
            "salescreditmemo",
            memoId);
        lineCache[typeof(DataHubProduct).Name] = CacheObjects(
            line.Product!.EntityId,
            "item",
            itemId);
        var mappedLine = await new MapSalesCreditMemoLineToBusinessCentralSalesCreditMemoLine()
            .MapAsync(line, CancellationToken.None, lineCache);

        Assert.Null(mappedHeader.CreditMemoDate);
        Assert.Null(mappedHeader.PostingDate);
        Assert.Contains("creditMemoDate", mappedHeader.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Contains("postingDate", mappedHeader.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Null(mappedLine.ShipmentDate);
        Assert.Contains("shipmentDate", mappedLine.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UntrackedMemoWithoutExternalNumberGetsStableReconciliationValue(
        string? externalDocumentNumber)
    {
        var source = NewCreditMemo();
        source.id = "common-prefix-0123456789abcdef-first-credit-memo";
        source.ExternalDocumentNumber = externalDocumentNumber;
        var cache = Cache<DataHubAccount>(source.Customer!.EntityId, "customer", Guid.NewGuid());

        var first = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo()
            .MapAsync(source, CancellationToken.None, cache);
        var second = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.NotNull(first.ExternalDocumentNumber);
        Assert.StartsWith("DHIT-CM-", first.ExternalDocumentNumber, StringComparison.Ordinal);
        Assert.True(first.ExternalDocumentNumber.Length <= 35);
        Assert.Equal(first.ExternalDocumentNumber, second.ExternalDocumentNumber);
    }

    [Fact]
    public async Task ReconciliationValueUsesCompleteDataHubId()
    {
        var firstSource = NewCreditMemo();
        firstSource.id = "common-prefix-0123456789abcdef-first-credit-memo";
        firstSource.ExternalDocumentNumber = null;
        var secondSource = NewCreditMemo();
        secondSource.id = "common-prefix-0123456789abcdef-second-credit-memo";
        secondSource.ExternalDocumentNumber = null;
        var customerId = Guid.NewGuid();
        var firstCache = Cache<DataHubAccount>(firstSource.Customer!.EntityId, "customer", customerId);
        var secondCache = Cache<DataHubAccount>(secondSource.Customer!.EntityId, "customer", customerId);

        var first = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo()
            .MapAsync(firstSource, CancellationToken.None, firstCache);
        var second = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo()
            .MapAsync(secondSource, CancellationToken.None, secondCache);

        Assert.NotEqual(first.ExternalDocumentNumber, second.ExternalDocumentNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task TrackedMemoPreservesBlankExternalNumber(string? externalDocumentNumber)
    {
        var source = NewCreditMemo();
        source.ExternalDocumentNumber = externalDocumentNumber;
        source.alternateKeys =
        [
            new AlternateKey
            {
                Key = "businesscentral.salescreditmemo",
                Value = Guid.NewGuid().ToString()
            }
        ];

        var result = await new MapSalesCreditMemoToBusinessCentralSalesCreditMemo().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubAccount>(source.Customer!.EntityId, "customer", Guid.NewGuid()));

        Assert.Equal(externalDocumentNumber, result.ExternalDocumentNumber);
    }

    [Fact]
    public async Task BusinessCentralHeaderMapsReferencesDatesSnapshotsAndCalculatedFields()
    {
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var modified = new DateTimeOffset(2026, 8, 15, 1, 2, 3, TimeSpan.Zero);
        var source = new BusinessCentralSalesCreditMemo
        {
            Id = Guid.NewGuid().ToString(),
            Number = "SCM-100",
            ExternalDocumentNumber = "DHIT-CM-100",
            CreditMemoDate = "2026-08-15",
            PostingDate = "2026-08-16",
            DueDate = "2026-09-15",
            CustomerId = customerId,
            CustomerNumber = "C100",
            CustomerName = "Adatum",
            InvoiceId = invoiceId,
            InvoiceNumber = "SI-100",
            PhoneNumber = "+61 2 9000 0000",
            Email = "credit@example.test",
            Status = "Draft",
            PricesIncludeTax = false,
            DiscountAmount = 5m,
            DiscountAppliedBeforeTax = true,
            TotalAmountExcludingTax = 95m,
            TotalTaxAmount = 9.5m,
            TotalAmountIncludingTax = 104.5m,
            LastModifiedDateTime = modified
        };

        var result = await new MapBusinessCentralSalesCreditMemoToSalesCreditMemo()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(modified, result.createdOn);
        Assert.Equal(modified, result.lastUpdated);
        Assert.Equal(source.Number, result.CreditMemoNumber);
        Assert.Equal(source.ExternalDocumentNumber, result.ExternalDocumentNumber);
        Assert.Equal(source.CreditMemoDate, result.CreditMemoDate);
        Assert.Equal(source.PostingDate, result.PostingDate);
        Assert.Equal(source.DueDate, result.DueDate);
        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.PricesIncludeTax, result.PricesIncludeTax);
        Assert.Equal(source.DiscountAppliedBeforeTax, result.DiscountAppliedBeforeTax);
        Assert.Equal(source.TotalAmountIncludingTax, result.TotalAmountIncludingTax);
        AssertExternalReference<DataHubAccount, BusinessCentralCustomer>(result.Customer, customerId);
        AssertExternalReference<DataHubSalesInvoice, BusinessCentralSalesInvoice>(
            result.OriginalSalesInvoice,
            invoiceId);
    }

    [Fact]
    public async Task DataHubLineResolvesParentAndProductAndMapsOneDiscountRepresentation()
    {
        var source = NewCreditMemoLine();
        var memoId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cache = Cache<DataHubSalesCreditMemo>(source.SalesCreditMemo!.EntityId, "salescreditmemo", memoId);
        cache[typeof(DataHubProduct).Name] = CacheObjects(source.Product!.EntityId, "item", itemId);

        var percentResult = await new MapSalesCreditMemoLineToBusinessCentralSalesCreditMemoLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(memoId, percentResult.DocumentId);
        Assert.Equal(itemId, percentResult.ItemId);
        Assert.Equal("Item", percentResult.LineType);
        Assert.Equal(source.Description, percentResult.Description);
        Assert.Equal(source.Quantity, percentResult.Quantity);
        Assert.Equal(source.UnitPrice, percentResult.UnitPrice);
        Assert.Equal(source.ShipmentDate, percentResult.ShipmentDate);
        Assert.Equal(source.DiscountPercent, percentResult.DiscountPercent);
        Assert.DoesNotContain("discountAmount", percentResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Null(percentResult.Sequence);
        Assert.Null(percentResult.AmountIncludingTax);

        source.DiscountPercent = null;
        source.ManualDiscountAmount = 12.34m;
        var amountResult = await new MapSalesCreditMemoLineToBusinessCentralSalesCreditMemoLine()
            .MapAsync(source, CancellationToken.None, cache);
        Assert.Equal(source.ManualDiscountAmount, amountResult.DiscountAmount);
        Assert.DoesNotContain("discountPercent", amountResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);

        source.ManualDiscountAmount = null;
        var clearedResult = await new MapSalesCreditMemoLineToBusinessCentralSalesCreditMemoLine()
            .MapAsync(source, CancellationToken.None, cache);
        Assert.Equal(0m, clearedResult.DiscountPercent);
        Assert.DoesNotContain("discountAmount", clearedResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessCentralLineMapsReferencesShipmentAndCalculatedValues()
    {
        var memoId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var source = new BusinessCentralSalesCreditMemoLine
        {
            Id = Guid.NewGuid().ToString(),
            DocumentId = memoId,
            ItemId = itemId,
            Sequence = 10000,
            Description = "Returned consulting",
            Description2 = "August",
            Quantity = 2m,
            UnitPrice = 100m,
            DiscountAmount = 10m,
            DiscountPercent = 5m,
            AmountExcludingTax = 190m,
            TaxPercent = 10m,
            TotalTaxAmount = 19m,
            AmountIncludingTax = 209m,
            ShipmentDate = "2026-08-14"
        };

        var result = await new MapBusinessCentralSalesCreditMemoLineToSalesCreditMemoLine()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(DateTimeOffset.UnixEpoch, result.createdOn);
        Assert.Equal(source.UnitPrice, result.UnitPrice);
        Assert.Equal(source.DiscountAmount, result.ManualDiscountAmount);
        Assert.Equal(source.AmountExcludingTax, result.AmountExcludingTax);
        Assert.Equal(source.AmountIncludingTax, result.AmountIncludingTax);
        Assert.Equal(source.ShipmentDate, result.ShipmentDate);
        AssertExternalReference<DataHubSalesCreditMemo, BusinessCentralSalesCreditMemo>(
            result.SalesCreditMemo,
            memoId);
        AssertExternalReference<DataHubProduct, BusinessCentralItem>(result.Product, itemId);
    }

    [Fact]
    public async Task MissingRequiredReferencesFailClearlyWhileOriginalInvoiceRemainsOptional()
    {
        var missingCustomer = NewCreditMemo();
        missingCustomer.Customer = null;
        var unresolvedInvoice = NewCreditMemo();
        unresolvedInvoice.OriginalSalesInvoice = new EntityReference
        {
            EntityType = typeof(DataHubSalesInvoice).Name,
            EntityId = "missing-invoice"
        };
        var line = NewCreditMemoLine();
        line.Product = null;

        var customerFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapSalesCreditMemoToBusinessCentralSalesCreditMemo()
                .MapAsync(missingCustomer, CancellationToken.None));
        var invoiceFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapSalesCreditMemoToBusinessCentralSalesCreditMemo().MapAsync(
                unresolvedInvoice,
                CancellationToken.None,
                Cache<DataHubAccount>(unresolvedInvoice.Customer!.EntityId, "customer", Guid.NewGuid())));
        var lineFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapSalesCreditMemoLineToBusinessCentralSalesCreditMemoLine()
                .MapAsync(line, CancellationToken.None));

        Assert.Contains("reference a customer", customerFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("original sales invoice", invoiceFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("both a sales credit memo and a product", lineFailure.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertDateProperty(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.Single(property!.GetCustomAttributes(typeof(BusinessCentralDateAttribute), inherit: true));
    }

    private static void AssertExternalReference<TDataHubEntity, TBusinessCentralEntity>(
        EntityReference? reference,
        Guid expectedId)
        where TDataHubEntity : DataHubEntity
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var external = Assert.IsType<ExternalEntityReference>(reference);
        Assert.Equal(typeof(TDataHubEntity).Name, external.EntityType);
        Assert.Equal(typeof(TBusinessCentralEntity).Name, external.SourceEntityType);
        Assert.Equal("BusinessCentral", external.DataSource);
        Assert.Equal(expectedId.ToString(), external.EntityId);
    }

    private static DataHubSalesCreditMemo NewCreditMemo() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        ExternalDocumentNumber = "DHIT-CM-100",
        CreditMemoDate = "2026-08-15",
        PostingDate = "2026-08-16",
        DueDate = "2026-09-15",
        Customer = new EntityReference
        {
            EntityType = typeof(DataHubAccount).Name,
            EntityId = "account-1"
        },
        PhoneNumber = "+61 2 9000 0000",
        Email = "credit@example.test",
        DiscountAmount = 5m
    };

    private static DataHubSalesCreditMemoLine NewCreditMemoLine() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        SalesCreditMemo = new EntityReference
        {
            EntityType = typeof(DataHubSalesCreditMemo).Name,
            EntityId = "credit-memo-1"
        },
        Product = new EntityReference
        {
            EntityType = typeof(DataHubProduct).Name,
            EntityId = "product-1"
        },
        Description = "Returned item",
        Description2 = "Damaged",
        Quantity = 2m,
        UnitPrice = 100m,
        DiscountPercent = 5m,
        ShipmentDate = "2026-08-14"
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
