using FluentAssertions;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Xunit;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralPurchaseInvoiceCleanupPolicyTests
{
    [Fact(DisplayName = "Purchase-invoice cleanup permits only the paid no-series placeholder")]
    [Trait("Category", "Unit")]
    public void AllowsConfirmedNoSeriesPlaceholder()
    {
        var invoice = EmptyPaidTestInvoice();

        BusinessCentralPurchaseInvoiceCleanupPolicy.IsSafeNoSeriesPlaceholder(
                invoice,
                [NoSeriesPlaceholderLine(invoice)])
            .Should().BeTrue();
    }

    [Fact(DisplayName = "Purchase-invoice cleanup rejects multiple no-series placeholder lines")]
    [Trait("Category", "Unit")]
    public void RejectsMultipleNoSeriesPlaceholderLines()
    {
        var invoice = EmptyPaidTestInvoice();

        BusinessCentralPurchaseInvoiceCleanupPolicy.IsSafeNoSeriesPlaceholder(
                invoice,
                [NoSeriesPlaceholderLine(invoice), NoSeriesPlaceholderLine(invoice)])
            .Should().BeFalse();
    }

    [Fact(DisplayName = "Purchase-invoice cleanup rejects unsafe header values")]
    [Trait("Category", "Unit")]
    public void RejectsUnsafeHeaderValues()
    {
        AssertRejected(invoice => invoice.Status = "Draft");
        AssertRejected(invoice => invoice.VendorInvoiceNumber = "VENDOR-INV-1");
        AssertRejected(invoice => invoice.ETag = null);
        AssertRejected(invoice => invoice.DiscountAmount = null);
        AssertRejected(invoice => invoice.TotalAmountExcludingTax = 1m);
        AssertRejected(invoice => invoice.TotalTaxAmount = null);
        AssertRejected(invoice => invoice.TotalAmountIncludingTax = 1m);

        var invoice = EmptyPaidTestInvoice();
        BusinessCentralPurchaseInvoiceCleanupPolicy.IsSafeNoSeriesPlaceholder(invoice, [])
            .Should().BeFalse();
    }

    [Fact(DisplayName = "Purchase-invoice cleanup rejects non-placeholder and incomplete lines")]
    [Trait("Category", "Unit")]
    public void RejectsUnsafeLineValues()
    {
        AssertLineRejected(line => line.DocumentId = Guid.NewGuid());
        AssertLineRejected(line => line.LineType = "Item");
        AssertLineRejected(line => line.Description = "Different comment");
        AssertLineRejected(line => line.ItemId = Guid.NewGuid());
        AssertLineRejected(line => line.Quantity = 1m);
        AssertLineRejected(line => line.UnitCost = 1m);
        AssertLineRejected(line => line.DiscountAmount = 1m);
        AssertLineRejected(line => line.AmountExcludingTax = null);
        AssertLineRejected(line => line.TotalTaxAmount = 1m);
        AssertLineRejected(line => line.AmountIncludingTax = 1m);
    }

    private static void AssertRejected(Action<BusinessCentralPurchaseInvoice> change)
    {
        var invoice = EmptyPaidTestInvoice();
        change(invoice);

        BusinessCentralPurchaseInvoiceCleanupPolicy.IsSafeNoSeriesPlaceholder(
                invoice,
                [NoSeriesPlaceholderLine(invoice)])
            .Should().BeFalse();
    }

    private static void AssertLineRejected(Action<BusinessCentralPurchaseInvoiceLine> change)
    {
        var invoice = EmptyPaidTestInvoice();
        var line = NoSeriesPlaceholderLine(invoice);
        change(line);

        BusinessCentralPurchaseInvoiceCleanupPolicy.IsSafeNoSeriesPlaceholder(invoice, [line])
            .Should().BeFalse();
    }

    private static BusinessCentralPurchaseInvoice EmptyPaidTestInvoice() => new()
    {
        Id = Guid.NewGuid().ToString(),
        ETag = "W/\"JzQ0O2FiYyc=\"",
        VendorInvoiceNumber = "DHIT-PI-CLEANUP",
        Status = "Paid",
        DiscountAmount = 0m,
        TotalAmountExcludingTax = 0m,
        TotalTaxAmount = 0m,
        TotalAmountIncludingTax = 0m
    };

    private static BusinessCentralPurchaseInvoiceLine NoSeriesPlaceholderLine(
        BusinessCentralPurchaseInvoice invoice) => new()
    {
        Id = Guid.NewGuid().ToString(),
        DocumentId = Guid.Parse(invoice.Id!),
        LineType = "Comment",
        Description = BusinessCentralPurchaseInvoiceCleanupPolicy.NoSeriesPlaceholderDescription,
        Description2 = string.Empty,
        LineObjectNumber = string.Empty,
        ItemId = Guid.Empty,
        Quantity = 0m,
        UnitCost = 0m,
        DiscountAmount = 0m,
        DiscountPercent = 0m,
        AmountExcludingTax = 0m,
        TaxPercent = 0m,
        TotalTaxAmount = 0m,
        AmountIncludingTax = 0m
    };
}
