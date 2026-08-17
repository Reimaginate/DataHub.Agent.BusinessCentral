using System.Reflection;
using System.Text.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Xunit;
using BusinessCentralCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Currency;
using BusinessCentralItemVariant = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.ItemVariant;
using BusinessCentralLocation = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Location;
using BusinessCentralPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentMethod;
using BusinessCentralPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentTerm;
using BusinessCentralUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.UnitOfMeasure;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Models;

public sealed class ReferenceEntityModelTests
{
    [Theory]
    [InlineData(typeof(BusinessCentralCurrency), "currencies")]
    [InlineData(typeof(BusinessCentralPaymentTerm), "paymentTerms")]
    [InlineData(typeof(BusinessCentralPaymentMethod), "paymentMethods")]
    [InlineData(typeof(BusinessCentralUnitOfMeasure), "unitsOfMeasure")]
    [InlineData(typeof(BusinessCentralLocation), "locations")]
    [InlineData(typeof(BusinessCentralItemVariant), "itemVariants")]
    public void ReferenceEntityUsesStandardIncrementalRoute(Type entityType, string expectedRoute)
    {
        var route = entityType.GetCustomAttribute<BusinessCentralUrlAttribute>();
        var lastModified = entityType.GetCustomAttribute<BusinessCentralLastModifiedAttribute>();

        Assert.NotNull(route);
        Assert.Equal(expectedRoute, route.Url);
        Assert.NotNull(lastModified);
        Assert.Equal("lastModifiedDateTime", lastModified.PropertyName);
        Assert.True(typeof(IBusinessCentralIncrementalEntity).IsAssignableFrom(entityType));
    }

    [Fact]
    public void StandardReferenceEntityPayloadsDeserializeSupportedProperties()
    {
        var modified = new DateTimeOffset(2026, 8, 14, 1, 2, 3, TimeSpan.Zero);
        var itemId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var currency = Deserialize<BusinessCentralCurrency>(
            """
            {
              "id": "00000000-0000-0000-0000-000000000001",
              "code": "AUD",
              "displayName": "Australian Dollar",
              "symbol": "$",
              "amountDecimalPlaces": "2:2",
              "amountRoundingPrecision": 0.01,
              "lastModifiedDateTime": "2026-08-14T01:02:03Z"
            }
            """);
        Assert.Equal("AUD", currency.Code);
        Assert.Equal("Australian Dollar", currency.DisplayName);
        Assert.Equal("$", currency.Symbol);
        Assert.Equal("2:2", currency.AmountDecimalPlaces);
        Assert.Equal(0.01m, currency.AmountRoundingPrecision);
        Assert.Equal(modified, currency.LastModifiedAt);

        var paymentTerm = Deserialize<BusinessCentralPaymentTerm>(
            """
            {
              "id": "00000000-0000-0000-0000-000000000002",
              "code": "30D",
              "displayName": "Net 30 days",
              "dueDateCalculation": "30D",
              "discountDateCalculation": "10D",
              "discountPercent": 2.5,
              "calculateDiscountOnCreditMemos": true,
              "lastModifiedDateTime": "2026-08-14T01:02:03Z"
            }
            """);
        Assert.Equal("30D", paymentTerm.Code);
        Assert.Equal("30D", paymentTerm.DueDateCalculation);
        Assert.Equal("10D", paymentTerm.DiscountDateCalculation);
        Assert.Equal(2.5m, paymentTerm.DiscountPercent);
        Assert.True(paymentTerm.CalculateDiscountOnCreditMemos);
        Assert.Equal(modified, paymentTerm.LastModifiedAt);

        var paymentMethod = Deserialize<BusinessCentralPaymentMethod>(
            """
            {
              "id": "00000000-0000-0000-0000-000000000003",
              "code": "EFT",
              "displayName": "Electronic funds transfer",
              "lastModifiedDateTime": "2026-08-14T01:02:03Z"
            }
            """);
        Assert.Equal("EFT", paymentMethod.Code);
        Assert.Equal("Electronic funds transfer", paymentMethod.DisplayName);
        Assert.Equal(modified, paymentMethod.LastModifiedAt);

        var unitOfMeasure = Deserialize<BusinessCentralUnitOfMeasure>(
            """
            {
              "id": "00000000-0000-0000-0000-000000000004",
              "code": "BOX",
              "displayName": "Box",
              "internationalStandardCode": "BX",
              "symbol": "box",
              "lastModifiedDateTime": "2026-08-14T01:02:03Z"
            }
            """);
        Assert.Equal("BOX", unitOfMeasure.Code);
        Assert.Equal("BX", unitOfMeasure.InternationalStandardCode);
        Assert.Equal("box", unitOfMeasure.Symbol);
        Assert.Equal(modified, unitOfMeasure.LastModifiedAt);

        var location = Deserialize<BusinessCentralLocation>(
            """
            {
              "id": "00000000-0000-0000-0000-000000000005",
              "code": "MAIN",
              "displayName": "Main Warehouse",
              "contact": "Warehouse Team",
              "addressLine1": "1 Store Street",
              "addressLine2": "Dock 2",
              "city": "Sydney",
              "state": "NSW",
              "country": "AU",
              "postalCode": "2000",
              "phoneNumber": "+61 2 9000 0070",
              "email": "warehouse@example.test",
              "website": "https://example.test/warehouse",
              "lastModifiedDateTime": "2026-08-14T01:02:03Z"
            }
            """);
        Assert.Equal("MAIN", location.Code);
        Assert.Equal("Main Warehouse", location.DisplayName);
        Assert.Equal("Warehouse Team", location.Contact);
        Assert.Equal("Sydney", location.City);
        Assert.Equal("NSW", location.State);
        Assert.Equal("warehouse@example.test", location.Email);
        Assert.Equal(modified, location.LastModifiedAt);

        var itemVariant = Deserialize<BusinessCentralItemVariant>(
            $$"""
            {
              "id": "00000000-0000-0000-0000-000000000006",
              "itemId": "{{itemId}}",
              "itemNumber": "ITEM-100",
              "code": "BLUE",
              "description": "Blue variant",
              "lastModifiedDateTime": "2026-08-14T01:02:03Z"
            }
            """);
        Assert.Equal(itemId, itemVariant.ItemId);
        Assert.Equal("ITEM-100", itemVariant.ItemNumber);
        Assert.Equal("BLUE", itemVariant.Code);
        Assert.Equal("Blue variant", itemVariant.Description);
        Assert.Equal(modified, itemVariant.LastModifiedAt);
    }

    private static T Deserialize<T>(string json)
        where T : BusinessCentralDocument
    {
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException($"Could not deserialize {typeof(T).Name}.");
    }
}
