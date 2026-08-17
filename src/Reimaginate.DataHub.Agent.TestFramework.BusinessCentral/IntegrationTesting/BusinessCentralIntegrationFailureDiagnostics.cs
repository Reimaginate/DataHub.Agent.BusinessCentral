namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public static class BusinessCentralIntegrationFailureDiagnostics
{
    private const string AllowDocumentDeletionBefore = "Allow Document Deletion Before";

    public static bool IsPostedSalesDocumentDeletionBlocked(string responseBody) =>
        !string.IsNullOrWhiteSpace(responseBody) &&
        responseBody.Contains(AllowDocumentDeletionBefore, StringComparison.OrdinalIgnoreCase) &&
        responseBody.Contains("deleting sales documents", StringComparison.OrdinalIgnoreCase);

    public static bool IsPostedPurchaseDocumentDeletionBlocked(string responseBody) =>
        !string.IsNullOrWhiteSpace(responseBody) &&
        responseBody.Contains(AllowDocumentDeletionBefore, StringComparison.OrdinalIgnoreCase) &&
        responseBody.Contains("purchase", StringComparison.OrdinalIgnoreCase);

    public static bool IsPurchaseInvoicePlaceholderPermissionBlocked(string responseBody) =>
        !string.IsNullOrWhiteSpace(responseBody) &&
        responseBody.Contains("current permissions prevented", StringComparison.OrdinalIgnoreCase) &&
        (responseBody.Contains("TableData 122", StringComparison.OrdinalIgnoreCase) ||
         responseBody.Contains("TableData 123", StringComparison.OrdinalIgnoreCase) ||
         responseBody.Contains("Purch. Inv. Header", StringComparison.OrdinalIgnoreCase) ||
         responseBody.Contains("Purch. Inv. Line", StringComparison.OrdinalIgnoreCase));

    public static bool IsSalesCreditMemoPlaceholderPermissionBlocked(string responseBody) =>
        !string.IsNullOrWhiteSpace(responseBody) &&
        responseBody.Contains("current permissions prevented", StringComparison.OrdinalIgnoreCase) &&
        (responseBody.Contains("TableData 114", StringComparison.OrdinalIgnoreCase) ||
         responseBody.Contains("TableData 115", StringComparison.OrdinalIgnoreCase) ||
         responseBody.Contains("Sales Cr.Memo Header", StringComparison.OrdinalIgnoreCase) ||
         responseBody.Contains("Sales Cr.Memo Line", StringComparison.OrdinalIgnoreCase));

    public static bool IsGeneralLedgerEntryReadPermissionBlocked(string responseBody) =>
        !string.IsNullOrWhiteSpace(responseBody) &&
        responseBody.Contains("current permissions prevented", StringComparison.OrdinalIgnoreCase) &&
        responseBody.Contains("TableData 17", StringComparison.OrdinalIgnoreCase) &&
        responseBody.Contains("G/L Entry Read", StringComparison.OrdinalIgnoreCase);

    public static string PostedSalesDocumentDeletionGuidance =>
        "Business Central retained an empty posted test-invoice artifact, but the company blocks its deletion. " +
        "In the isolated test company only, open Sales & Receivables Setup and set 'Allow Document Deletion Before' " +
        "to a date after the test posting date (normally tomorrow), then rerun the sales-invoice preflight. " +
        "This setting applies to every posted sales document in that company, so do not enable it in a company " +
        "that contains business data.";

    public static string PostedPurchaseDocumentDeletionGuidance =>
        "Business Central created the validated zero-value purchase-invoice no-series placeholder, but the " +
        "company blocks its deletion. In the isolated test company only, open Purchases & Payables Setup and " +
        "set 'Allow Document Deletion Before' to a date after the test posting date (normally tomorrow), then " +
        "rerun the purchase-invoice preflight. Do not enable this in a company that contains business data.";

    public static string PurchaseInvoicePlaceholderPermissionGuidance =>
        "The Entra app can create the validated purchase-invoice no-series placeholder but cannot remove it. " +
        "Assign a company-scoped test permission set with indirect insert and delete access to TableData 122 " +
        "Purch. Inv. Header and TableData 123 Purch. Inv. Line. Do not assign D365 PURCH DOC, POST or SUPER.";

    public static string SalesCreditMemoPlaceholderPermissionGuidance =>
        "The Entra app can create the validated sales-credit-memo no-series placeholder but cannot remove it. " +
        "Assign a company-scoped test permission set with read plus indirect insert, modify, and delete access " +
        "to TableData 114 Sales Cr.Memo Header, and read plus indirect insert and delete access to TableData 115 " +
        "Sales Cr.Memo Line. Do not assign D365 SALES DOC, POST or SUPER.";

    public static string GeneralLedgerEntryReadPermissionGuidance =>
        "The standard G/L-entry API reads Base Application TableData 17 G/L Entry, which is outside the APIV2 " +
        "application-scope entitlement and cannot be enabled by a tenant permission set alone. Deploy the " +
        "Data Hub correlation extension version 1.0.1.0 or later and use its read-only " +
        "api/reimaginate/dataHub/v1.0/generalLedgerEntries endpoint. Assign DH DATAHUB CORR to the Entra app " +
        "for the isolated company. Do not assign D365 READ, posting permissions, or SUPER.";

    public static string MissingEntityPermissionGuidance(string operation)
    {
        var readOnlyOperation = operation.StartsWith("read ", StringComparison.OrdinalIgnoreCase) ||
            operation.StartsWith("list ", StringComparison.OrdinalIgnoreCase) ||
            operation.StartsWith("find ", StringComparison.OrdinalIgnoreCase) ||
            operation.StartsWith("verify ", StringComparison.OrdinalIgnoreCase);

        return readOnlyOperation
            ? "The Entra app is authenticated but lacks Business Central permissions for this operation. " +
              "Assign the smallest company-scoped permission set that grants direct read access required by " +
              "this API operation; do not add write permissions to a read-only endpoint."
            : "The Entra app is authenticated but lacks Business Central permissions for this operation. " +
              "Assign the smallest company-scoped permission set that grants only the access required by this " +
              "API operation.";
    }
}
