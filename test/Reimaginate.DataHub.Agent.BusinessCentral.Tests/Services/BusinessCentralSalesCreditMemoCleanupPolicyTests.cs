using FluentAssertions;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Xunit;
using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemoLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralSalesCreditMemoCleanupPolicyTests
{
    [Fact(DisplayName = "Sales-credit-memo cleanup permits only the paid no-series placeholder")]
    [Trait("Category", "Unit")]
    public void AllowsConfirmedNoSeriesPlaceholder()
    {
        var creditMemo = EmptyPaidTestCreditMemo();

        BusinessCentralSalesCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(
                creditMemo,
                [NoSeriesPlaceholderLine(creditMemo)])
            .Should().BeTrue();
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup rejects multiple no-series placeholder lines")]
    [Trait("Category", "Unit")]
    public void RejectsMultipleNoSeriesPlaceholderLines()
    {
        var creditMemo = EmptyPaidTestCreditMemo();

        BusinessCentralSalesCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(
                creditMemo,
                [NoSeriesPlaceholderLine(creditMemo), NoSeriesPlaceholderLine(creditMemo)])
            .Should().BeFalse();
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup allows a blank identity only with transition provenance")]
    [Trait("Category", "Unit")]
    public void BlankIdentityRequiresTransitionProvenance()
    {
        var creditMemo = EmptyPaidTestCreditMemo();
        creditMemo.ExternalDocumentNumber = string.Empty;
        var lines = new[] { NoSeriesPlaceholderLine(creditMemo) };

        BusinessCentralSalesCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(creditMemo, lines)
            .Should().BeFalse();
        BusinessCentralSalesCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(
                creditMemo,
                lines,
                allowBlankExternalDocumentNumber: true)
            .Should().BeTrue();
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup rejects unsafe header values")]
    [Trait("Category", "Unit")]
    public void RejectsUnsafeHeaderValues()
    {
        AssertRejected(creditMemo => creditMemo.Status = "Draft");
        AssertRejected(creditMemo => creditMemo.ExternalDocumentNumber = "CUSTOMER-CM-1");
        AssertRejected(creditMemo => creditMemo.ETag = null);
        AssertRejected(creditMemo => creditMemo.ETag = "*");
        AssertRejected(creditMemo => creditMemo.DiscountAmount = null);
        AssertRejected(creditMemo => creditMemo.TotalAmountExcludingTax = 1m);
        AssertRejected(creditMemo => creditMemo.TotalTaxAmount = null);
        AssertRejected(creditMemo => creditMemo.TotalAmountIncludingTax = 1m);

        var creditMemo = EmptyPaidTestCreditMemo();
        BusinessCentralSalesCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(creditMemo, [])
            .Should().BeFalse();
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup rejects non-placeholder and incomplete lines")]
    [Trait("Category", "Unit")]
    public void RejectsUnsafeLineValues()
    {
        AssertLineRejected(line => line.DocumentId = Guid.NewGuid());
        AssertLineRejected(line => line.Sequence = 20000);
        AssertLineRejected(line => line.LineType = "Item");
        AssertLineRejected(line => line.Description = "Different comment");
        AssertLineRejected(line => line.Description2 = "Unexpected");
        AssertLineRejected(line => line.LineObjectNumber = "1000");
        AssertLineRejected(line => line.ItemId = Guid.NewGuid());
        AssertLineRejected(line => line.Quantity = 1m);
        AssertLineRejected(line => line.UnitPrice = 1m);
        AssertLineRejected(line => line.DiscountAmount = 1m);
        AssertLineRejected(line => line.DiscountPercent = 1m);
        AssertLineRejected(line => line.AmountExcludingTax = null);
        AssertLineRejected(line => line.TaxPercent = 1m);
        AssertLineRejected(line => line.TotalTaxAmount = 1m);
        AssertLineRejected(line => line.AmountIncludingTax = 1m);
        AssertLineRejected(line => line.ShipmentDate = "2026-08-15");
    }

    private static void AssertRejected(Action<BusinessCentralSalesCreditMemo> change)
    {
        var creditMemo = EmptyPaidTestCreditMemo();
        change(creditMemo);

        BusinessCentralSalesCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(
                creditMemo,
                [NoSeriesPlaceholderLine(creditMemo)])
            .Should().BeFalse();
    }

    private static void AssertLineRejected(Action<BusinessCentralSalesCreditMemoLine> change)
    {
        var creditMemo = EmptyPaidTestCreditMemo();
        var line = NoSeriesPlaceholderLine(creditMemo);
        change(line);

        BusinessCentralSalesCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(creditMemo, [line])
            .Should().BeFalse();
    }

    private static BusinessCentralSalesCreditMemo EmptyPaidTestCreditMemo() => new()
    {
        Id = Guid.NewGuid().ToString(),
        ETag = "W/\"JzQ0O2FiYyc=\"",
        ExternalDocumentNumber = "DHIT-SCM-CLEANUP",
        Status = "Paid",
        DiscountAmount = 0m,
        TotalAmountExcludingTax = 0m,
        TotalTaxAmount = 0m,
        TotalAmountIncludingTax = 0m
    };

    private static BusinessCentralSalesCreditMemoLine NoSeriesPlaceholderLine(
        BusinessCentralSalesCreditMemo creditMemo) => new()
    {
        Id = Guid.NewGuid().ToString(),
        DocumentId = Guid.Parse(creditMemo.Id!),
        Sequence = 10000,
        LineType = "Comment",
        Description = BusinessCentralSalesCreditMemoCleanupPolicy.NoSeriesPlaceholderDescription,
        Description2 = string.Empty,
        LineObjectNumber = string.Empty,
        ItemId = Guid.Empty,
        Quantity = 0m,
        UnitPrice = 0m,
        DiscountAmount = 0m,
        DiscountPercent = 0m,
        AmountExcludingTax = 0m,
        TaxPercent = 0m,
        TotalTaxAmount = 0m,
        AmountIncludingTax = 0m,
        ShipmentDate = "0001-01-01"
    };
}
