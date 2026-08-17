using FluentAssertions;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Xunit;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralSalesInvoiceCleanupPolicyTests
{
    [Fact(DisplayName = "Invoice cleanup permits only empty paid test artifacts")]
    [Trait("Category", "Unit")]
    public void AllowsEmptyPaidTestArtifact()
    {
        BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(
                EmptyPostedTestInvoice(),
                [])
            .Should().BeTrue();
    }

    [Fact(DisplayName = "Invoice cleanup rejects non-test and non-empty invoices")]
    [Trait("Category", "Unit")]
    public void RejectsUnsafeInvoiceValues()
    {
        var nonTestInvoice = EmptyPostedTestInvoice();
        nonTestInvoice.ExternalDocumentNumber = "CUSTOMER-INVOICE";
        BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(
                nonTestInvoice,
                [])
            .Should().BeFalse();

        var nonZeroInvoice = EmptyPostedTestInvoice();
        nonZeroInvoice.TotalAmountIncludingTax = 1m;
        BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(
                nonZeroInvoice,
                [])
            .Should().BeFalse();

        var invoiceWithNonZeroLine = EmptyPostedTestInvoice();
        BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(
                invoiceWithNonZeroLine,
                [new BusinessCentralSalesInvoiceLine
                {
                    DocumentId = Guid.Parse(invoiceWithNonZeroLine.Id!),
                    Quantity = 1m,
                    UnitPrice = 0m,
                    AmountExcludingTax = 0m,
                    TotalTaxAmount = 0m,
                    AmountIncludingTax = 0m
                }])
            .Should().BeFalse();
    }

    [Fact(DisplayName = "Invoice cleanup rejects draft and incomplete API responses")]
    [Trait("Category", "Unit")]
    public void RejectsDraftAndIncompleteResponses()
    {
        var draft = EmptyPostedTestInvoice();
        draft.Status = "Draft";
        BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(draft, [])
            .Should().BeFalse();

        var incomplete = EmptyPostedTestInvoice();
        incomplete.TotalTaxAmount = null;
        BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(incomplete, [])
            .Should().BeFalse();
    }

    [Fact(DisplayName = "Invoice cleanup permits only zero-valued retained aggregate lines")]
    [Trait("Category", "Unit")]
    public void AllowsZeroValuedRetainedAggregateLine()
    {
        var invoice = EmptyPostedTestInvoice();

        BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(
                invoice,
                [new BusinessCentralSalesInvoiceLine
                {
                    DocumentId = Guid.Parse(invoice.Id!),
                    Quantity = 0m,
                    UnitPrice = 0m,
                    AmountExcludingTax = 0m,
                    TotalTaxAmount = 0m,
                    AmountIncludingTax = 0m
                }])
            .Should().BeTrue();
    }

    private static BusinessCentralSalesInvoice EmptyPostedTestInvoice() => new()
    {
        Id = Guid.NewGuid().ToString(),
        ExternalDocumentNumber = "DHIT-I-CLEANUP",
        Status = "Paid",
        RemainingAmount = 0m,
        TotalAmountExcludingTax = 0m,
        TotalTaxAmount = 0m,
        TotalAmountIncludingTax = 0m
    };
}
