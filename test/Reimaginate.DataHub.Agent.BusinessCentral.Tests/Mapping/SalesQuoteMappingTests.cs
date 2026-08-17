using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuote;
using BusinessCentralSalesQuoteLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuoteLine;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Quote;
using DataHubQuoteLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.QuoteLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class SalesQuoteMappingTests
{
    [Theory]
    [InlineData(typeof(DataHubQuote), "SalesQuote")]
    [InlineData(typeof(DataHubQuoteLine), "SalesQuoteLine")]
    public void QuoteModelsDeclareBusinessCentralRelatedTypes(Type dataHubType, string businessCentralType)
    {
        var attribute = Assert.Single(dataHubType
            .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), inherit: true)
            .Cast<RelatedEntityTypeAttribute>());

        Assert.Equal("BusinessCentral", attribute.DataSource);
        Assert.Equal(businessCentralType, attribute.TypeName);
    }

    [Fact]
    public void QuoteRoutesDatesAndFailClosedRecoveryContractMatchTheStandardApi()
    {
        var quoteRoute = Assert.Single(typeof(BusinessCentralSalesQuote)
            .GetCustomAttributes(typeof(BusinessCentralUrlAttribute), inherit: true)
            .Cast<BusinessCentralUrlAttribute>());
        var lineRoute = Assert.Single(typeof(BusinessCentralSalesQuoteLine)
            .GetCustomAttributes(typeof(BusinessCentralUrlAttribute), inherit: true)
            .Cast<BusinessCentralUrlAttribute>());
        var parentRoute = Assert.Single(typeof(BusinessCentralSalesQuoteLine)
            .GetCustomAttributes(typeof(BusinessCentralParentUrlAttribute), inherit: true)
            .Cast<BusinessCentralParentUrlAttribute>());
        var recoveryFields = typeof(BusinessCentralSalesQuote)
            .GetProperties()
            .SelectMany(property => property
                .GetCustomAttributes(typeof(BusinessCentralCreateRecoveryKeyAttribute), inherit: true)
                .Cast<BusinessCentralCreateRecoveryKeyAttribute>())
            .Select(attribute => attribute.FieldName)
            .OrderBy(field => field)
            .ToArray();

        Assert.Equal("salesQuotes", quoteRoute.Url);
        Assert.Equal("salesQuoteLines", lineRoute.Url);
        Assert.Equal("salesQuotes", parentRoute.ParentUrl);
        Assert.Equal(nameof(BusinessCentralSalesQuoteLine.DocumentId), parentRoute.ParentIdPropertyName);
        // Standard Business Central doesn't enforce uniqueness for customerId plus
        // externalDocumentNumber. Advertising that pair as a recovery key could attach an
        // ambiguous POST retry to the wrong quote, so automatic recovery must remain disabled.
        Assert.Empty(recoveryFields);

        AssertDateProperty(nameof(BusinessCentralSalesQuote.DocumentDate));
        AssertDateProperty(nameof(BusinessCentralSalesQuote.PostingDate));
        AssertDateProperty(nameof(BusinessCentralSalesQuote.DueDate));
        AssertDateProperty(nameof(BusinessCentralSalesQuote.ValidUntilDate));
        AssertDateProperty(nameof(BusinessCentralSalesQuote.AcceptedDate));
    }

    [Fact]
    public async Task DataHubQuoteMapsOnlyEditableDraftFields()
    {
        var customerId = Guid.NewGuid();
        var source = NewQuote();

        var result = await new MapQuoteToBusinessCentralSalesQuote().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubAccount>(source.CustomerAccount!.EntityId, "customer", customerId));

        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(source.ExternalDocumentNumber, result.ExternalDocumentNumber);
        Assert.Equal("2026-08-15", result.DocumentDate);
        Assert.Equal("2026-09-14", result.ValidUntilDate);
        Assert.Equal(source.PhoneNumber, result.PhoneNumber);
        Assert.Equal(source.Email, result.Email);
        Assert.Equal(source.DiscountAmount, result.DiscountAmount);
        Assert.Null(result.Number);
        Assert.Null(result.Status);
        Assert.Null(result.SentDate);
        Assert.Null(result.AcceptedDate);
        Assert.Null(result.TotalAmountExcludingTax);
        Assert.Null(result.TotalTaxAmount);
        Assert.Null(result.TotalAmountIncludingTax);
        Assert.DoesNotContain("number", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("totalAmountIncludingTax", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NullDataHubQuoteDiscountMapsToBusinessCentralNormalizedZero()
    {
        var customerId = Guid.NewGuid();
        var source = NewQuote();
        source.DiscountAmount = null;

        var result = await new MapQuoteToBusinessCentralSalesQuote().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubAccount>(source.CustomerAccount!.EntityId, "customer", customerId));

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Contains("discountAmount", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NullDataHubQuoteDatesRemainNullForNormalizedComparison()
    {
        var customerId = Guid.NewGuid();
        var source = NewQuote();
        source.EffectiveFrom = null;
        source.EffectiveTo = null;

        var result = await new MapQuoteToBusinessCentralSalesQuote().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubAccount>(source.CustomerAccount!.EntityId, "customer", customerId));

        Assert.Null(result.DocumentDate);
        Assert.Null(result.ValidUntilDate);
        Assert.Contains("documentDate", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Contains("validUntilDate", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UntrackedQuoteWithoutExternalNumberGetsDeterministicRecoveryValue(
        string? externalDocumentNumber)
    {
        var customerId = Guid.NewGuid();
        var source = NewQuote();
        source.id = "common-prefix-0123456789abcdef-first-quote";
        source.ExternalDocumentNumber = externalDocumentNumber;
        var cache = Cache<DataHubAccount>(
            source.CustomerAccount!.EntityId,
            "customer",
            customerId);

        var first = await new MapQuoteToBusinessCentralSalesQuote()
            .MapAsync(source, CancellationToken.None, cache);
        var second = await new MapQuoteToBusinessCentralSalesQuote()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.NotNull(first.ExternalDocumentNumber);
        Assert.StartsWith("DHIT-Q-", first.ExternalDocumentNumber, StringComparison.Ordinal);
        Assert.True(first.ExternalDocumentNumber.Length <= 35);
        Assert.Equal(first.ExternalDocumentNumber, second.ExternalDocumentNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task TrackedBusinessCentralQuotePreservesBlankExternalNumber(
        string? externalDocumentNumber)
    {
        var customerId = Guid.NewGuid();
        var source = NewQuote();
        source.ExternalDocumentNumber = externalDocumentNumber;
        source.alternateKeys =
        [
            new AlternateKey
            {
                Key = "businesscentral.salesquote",
                Value = Guid.NewGuid().ToString()
            }
        ];

        var result = await new MapQuoteToBusinessCentralSalesQuote().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubAccount>(source.CustomerAccount!.EntityId, "customer", customerId));

        Assert.Equal(externalDocumentNumber, result.ExternalDocumentNumber);
    }

    [Fact]
    public async Task BusinessCentralQuoteMapsCommonSemanticsReferencesAndCalculatedValues()
    {
        var customerId = Guid.NewGuid();
        var modified = new DateTimeOffset(2026, 8, 15, 1, 2, 3, TimeSpan.Zero);
        var source = new BusinessCentralSalesQuote
        {
            Id = Guid.NewGuid().ToString(),
            Number = "SQ-100",
            ExternalDocumentNumber = "DHIT-QUOTE-100",
            DocumentDate = "2026-08-15",
            ValidUntilDate = "2026-09-14",
            CustomerId = customerId,
            PhoneNumber = "+61 2 9000 0000",
            Email = "quote@example.test",
            Status = "Draft",
            DiscountAmount = 5m,
            TotalAmountExcludingTax = 95m,
            TotalTaxAmount = 9.5m,
            TotalAmountIncludingTax = 104.5m,
            LastModifiedDateTime = modified
        };

        var result = await new MapBusinessCentralSalesQuoteToQuote()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(modified, result.createdOn);
        Assert.Equal(modified, result.lastUpdated);
        Assert.Equal(source.ExternalDocumentNumber, result.Name);
        Assert.Equal(source.Number, result.QuoteNumber);
        Assert.Equal(new DateTime(2026, 8, 15), result.EffectiveFrom);
        Assert.Equal(new DateTime(2026, 9, 14), result.EffectiveTo);
        Assert.Equal(source.TotalAmountExcludingTax, result.TotalLineItemAmount);
        Assert.Equal(source.TotalTaxAmount, result.TotalTaxAmount);
        Assert.Equal(source.TotalAmountIncludingTax, result.TotalAmount);
        var customer = Assert.IsType<ExternalEntityReference>(result.CustomerAccount);
        Assert.Equal(typeof(DataHubAccount).Name, customer.EntityType);
        Assert.Equal(typeof(BusinessCentralCustomer).Name, customer.SourceEntityType);
        Assert.Equal("BusinessCentral", customer.DataSource);
        Assert.Equal(customerId.ToString(), customer.EntityId);
    }

    [Fact]
    public async Task UndefinedBusinessCentralDatesMapToNull()
    {
        var source = new BusinessCentralSalesQuote
        {
            Id = Guid.NewGuid().ToString(),
            DocumentDate = BusinessCentralDateAttribute.UndefinedDateValue,
            ValidUntilDate = BusinessCentralDateAttribute.UndefinedDateValue
        };

        var result = await new MapBusinessCentralSalesQuoteToQuote()
            .MapAsync(source, CancellationToken.None);

        Assert.Null(result.EffectiveFrom);
        Assert.Null(result.EffectiveTo);
    }

    [Fact]
    public async Task DataHubQuoteLineResolvesParentAndProductAndMapsOneDiscountRepresentation()
    {
        var source = NewQuoteLine();
        var quoteId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cache = Cache<DataHubQuote>(source.Quote!.EntityId, "salesquote", quoteId);
        cache[typeof(DataHubProduct).Name] = CacheObjects(source.Product!.EntityId, "item", itemId);

        var percentResult = await new MapQuoteLineToBusinessCentralSalesQuoteLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(quoteId, percentResult.DocumentId);
        Assert.Equal(itemId, percentResult.ItemId);
        Assert.Equal("Item", percentResult.LineType);
        Assert.Equal(source.PricePerUnit, percentResult.UnitPrice);
        Assert.Equal(source.DiscountPercent, percentResult.DiscountPercent);
        Assert.DoesNotContain("discountAmount", percentResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Null(percentResult.Sequence);
        Assert.Null(percentResult.AmountIncludingTax);

        source.DiscountPercent = null;
        source.ManualDiscountAmount = 12.34m;
        var amountResult = await new MapQuoteLineToBusinessCentralSalesQuoteLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(source.ManualDiscountAmount, amountResult.DiscountAmount);
        Assert.DoesNotContain("discountPercent", amountResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);

        source.ManualDiscountAmount = null;
        var clearedResult = await new MapQuoteLineToBusinessCentralSalesQuoteLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(0m, clearedResult.DiscountPercent);
        Assert.DoesNotContain("discountAmount", clearedResult.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessCentralQuoteLineMapsCommonSemanticsAndCalculatedValues()
    {
        var quoteId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var source = new BusinessCentralSalesQuoteLine
        {
            Id = Guid.NewGuid().ToString(),
            DocumentId = quoteId,
            ItemId = itemId,
            Sequence = 10000,
            Description = "Consulting",
            Description2 = "August",
            Quantity = 2m,
            UnitPrice = 100m,
            DiscountAmount = 10m,
            DiscountPercent = 5m,
            AmountExcludingTax = 190m,
            TaxPercent = 10m,
            TotalTaxAmount = 19m,
            AmountIncludingTax = 209m
        };

        var result = await new MapBusinessCentralSalesQuoteLineToQuoteLine()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Id, result.id);
        Assert.Equal(DateTimeOffset.UnixEpoch, result.createdOn);
        Assert.Equal(source.UnitPrice, result.PricePerUnit);
        Assert.Equal(source.DiscountAmount, result.ManualDiscountAmount);
        Assert.Equal(source.AmountExcludingTax, result.BaseAmount);
        Assert.Equal(source.AmountIncludingTax, result.ExtendedAmount);
        var quote = Assert.IsType<ExternalEntityReference>(result.Quote);
        Assert.Equal(typeof(DataHubQuote).Name, quote.EntityType);
        Assert.Equal(typeof(BusinessCentralSalesQuote).Name, quote.SourceEntityType);
        Assert.Equal(quoteId.ToString(), quote.EntityId);
        var product = Assert.IsType<ExternalEntityReference>(result.Product);
        Assert.Equal(typeof(DataHubProduct).Name, product.EntityType);
        Assert.Equal(typeof(BusinessCentralItem).Name, product.SourceEntityType);
        Assert.Equal(itemId.ToString(), product.EntityId);
    }

    [Fact]
    public async Task MissingRequiredQuoteValuesFailClearly()
    {
        var missingCustomer = NewQuote();
        missingCustomer.CustomerAccount = null;
        var line = NewQuoteLine();
        line.Product = null;

        var customerFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapQuoteToBusinessCentralSalesQuote()
                .MapAsync(missingCustomer, CancellationToken.None));
        var lineFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapQuoteLineToBusinessCentralSalesQuoteLine()
                .MapAsync(line, CancellationToken.None));

        Assert.Contains("reference a customer", customerFailure.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("both a quote and a product", lineFailure.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertDateProperty(string propertyName)
    {
        var property = typeof(BusinessCentralSalesQuote).GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.Single(property!.GetCustomAttributes(typeof(BusinessCentralDateAttribute), inherit: true));
    }

    private static DataHubQuote NewQuote() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        Name = "Internal quote name",
        ExternalDocumentNumber = "DHIT-QUOTE-100",
        EffectiveFrom = new DateTime(2026, 8, 15, 12, 34, 56),
        EffectiveTo = new DateTime(2026, 9, 14, 23, 59, 59),
        CustomerAccount = new EntityReference
        {
            EntityType = typeof(DataHubAccount).Name,
            EntityId = "account-1"
        },
        PhoneNumber = "+61 2 9000 0000",
        Email = "quote@example.test",
        DiscountAmount = 5m
    };

    private static DataHubQuoteLine NewQuoteLine() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        Quote = new EntityReference
        {
            EntityType = typeof(DataHubQuote).Name,
            EntityId = "quote-1"
        },
        Product = new EntityReference
        {
            EntityType = typeof(DataHubProduct).Name,
            EntityId = "product-1"
        },
        Description = "Consulting",
        Description2 = "August",
        Quantity = 2m,
        PricePerUnit = 100m,
        DiscountPercent = 5m
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
