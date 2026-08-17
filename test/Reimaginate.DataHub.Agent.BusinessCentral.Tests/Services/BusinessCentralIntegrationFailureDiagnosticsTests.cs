using FluentAssertions;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Xunit;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralIntegrationFailureDiagnosticsTests
{
    [Fact(DisplayName = "Invoice cleanup identifies a missing posted-document deletion cutoff")]
    [Trait("Category", "Unit")]
    public void IdentifiesPostedSalesDocumentDeletionCutoffFailure()
    {
        const string response =
            "The date Allow Document Deletion Before must be set on the page Sales & Receivables Setup before deleting sales documents.";

        BusinessCentralIntegrationFailureDiagnostics.IsPostedSalesDocumentDeletionBlocked(response)
            .Should().BeTrue();
        BusinessCentralIntegrationFailureDiagnostics.PostedSalesDocumentDeletionGuidance
            .Should().Contain("isolated test company only")
            .And.Contain("normally tomorrow")
            .And.Contain("do not enable it in a company that contains business data");
    }

    [Fact(DisplayName = "Purchase-invoice cleanup identifies a missing posted-document deletion cutoff")]
    [Trait("Category", "Unit")]
    public void IdentifiesPostedPurchaseDocumentDeletionCutoffFailure()
    {
        const string response =
            "The date Allow Document Deletion Before must be set on the page Purchases & Payables Setup before deleting purchase documents.";

        BusinessCentralIntegrationFailureDiagnostics.IsPostedPurchaseDocumentDeletionBlocked(response)
            .Should().BeTrue();
        BusinessCentralIntegrationFailureDiagnostics.PostedPurchaseDocumentDeletionGuidance
            .Should().Contain("Purchases & Payables Setup")
            .And.Contain("normally tomorrow")
            .And.Contain("Do not enable this in a company that contains business data");
    }

    [Fact(DisplayName = "Purchase-invoice cleanup identifies missing posted-table permissions")]
    [Trait("Category", "Unit")]
    public void IdentifiesPurchaseInvoicePlaceholderPermissionFailure()
    {
        const string response =
            "The current permissions prevented the action. (TableData 122 Purch. Inv. Header Delete: Base Application)";

        BusinessCentralIntegrationFailureDiagnostics
            .IsPurchaseInvoicePlaceholderPermissionBlocked(response)
            .Should().BeTrue();
        BusinessCentralIntegrationFailureDiagnostics.PurchaseInvoicePlaceholderPermissionGuidance
            .Should().Contain("TableData 122")
            .And.Contain("TableData 123")
            .And.Contain("indirect insert and delete")
            .And.Contain("Do not assign D365 PURCH DOC, POST or SUPER");
    }

    [Theory(DisplayName = "Sales-credit-memo cleanup identifies missing posted-table permissions")]
    [Trait("Category", "Unit")]
    [InlineData("The current permissions prevented the action. (TableData 114 Sales Cr.Memo Header Delete: Base Application)")]
    [InlineData("The current permissions prevented the action. (TableData 115 Sales Cr.Memo Line IndirectDelete: Base Application)")]
    public void IdentifiesSalesCreditMemoPlaceholderPermissionFailure(string response)
    {
        BusinessCentralIntegrationFailureDiagnostics
            .IsSalesCreditMemoPlaceholderPermissionBlocked(response)
            .Should().BeTrue();
        BusinessCentralIntegrationFailureDiagnostics.SalesCreditMemoPlaceholderPermissionGuidance
            .Should().Contain("TableData 114")
            .And.Contain("TableData 115")
            .And.Contain("read plus indirect insert, modify, and delete")
            .And.Contain("read plus indirect insert and delete")
            .And.Contain("Do not assign D365 SALES DOC, POST or SUPER");
    }

    [Fact(DisplayName = "G/L entry reads identify the exact least-privilege permission")]
    [Trait("Category", "Unit")]
    public void IdentifiesGeneralLedgerEntryReadPermissionFailure()
    {
        const string response =
            "The current permissions prevented the action. (TableData 17 G/L Entry Read: _Exclude_APIV2_)";

        BusinessCentralIntegrationFailureDiagnostics
            .IsGeneralLedgerEntryReadPermissionBlocked(response)
            .Should().BeTrue();
        BusinessCentralIntegrationFailureDiagnostics.GeneralLedgerEntryReadPermissionGuidance
            .Should().Contain("TableData 17 G/L Entry")
            .And.Contain("outside the APIV2 application-scope entitlement")
            .And.Contain("correlation extension version 1.0.1.0")
            .And.Contain("api/reimaginate/dataHub/v1.0/generalLedgerEntries")
            .And.Contain("DH DATAHUB CORR")
            .And.Contain("Do not assign D365 READ, posting permissions, or SUPER");
    }

    [Fact(DisplayName = "Generic read failures do not recommend write permissions")]
    [Trait("Category", "Unit")]
    public void ReadPermissionGuidanceDoesNotRecommendWriteAccess()
    {
        BusinessCentralIntegrationFailureDiagnostics.MissingEntityPermissionGuidance("read existing entries")
            .Should().Contain("direct read access")
            .And.Contain("do not add write permissions")
            .And.NotContain("read/insert/modify/delete");
    }

    [Theory(DisplayName = "Invoice cleanup does not misclassify unrelated failures")]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData("The current permissions prevented the action.")]
    [InlineData("A customer with this identifier was not found.")]
    public void RejectsUnrelatedFailures(string response)
    {
        BusinessCentralIntegrationFailureDiagnostics.IsPostedSalesDocumentDeletionBlocked(response)
            .Should().BeFalse();
        BusinessCentralIntegrationFailureDiagnostics
            .IsSalesCreditMemoPlaceholderPermissionBlocked(response)
            .Should().BeFalse();
        BusinessCentralIntegrationFailureDiagnostics
            .IsGeneralLedgerEntryReadPermissionBlocked(response)
            .Should().BeFalse();
    }
}
