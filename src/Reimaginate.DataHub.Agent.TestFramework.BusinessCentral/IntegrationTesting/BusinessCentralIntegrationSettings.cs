namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public sealed class BusinessCentralIntegrationSettings
{
    public const string SectionName = "BusinessCentralIntegrationTests";

    public bool Enabled { get; set; }

    public string? TenantId { get; set; }

    public string? EnvironmentName { get; set; }

    public string? CompanyId { get; set; }

    public string? ExpectedCompanyName { get; set; }

    public bool AllowProductionEnvironment { get; set; }

    public bool SalesInvoiceWritesEnabled { get; set; }

    public bool SalesCreditMemoWritesEnabled { get; set; }

    public bool PurchaseInvoiceWritesEnabled { get; set; }

    public bool PurchaseCreditMemoWritesEnabled { get; set; }

    public bool CustomerPaymentWritesEnabled { get; set; }

    public bool VendorPaymentWritesEnabled { get; set; }

    public bool GeneralJournalWritesEnabled { get; set; }

    public bool CorrelationReservationsEnabled { get; set; }

    public string CorrelationApiRoute { get; set; } = "api/reimaginate/dataHub/v1.0";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string BaseUrl =>
        $"https://api.businesscentral.dynamics.com/v2.0/{Uri.EscapeDataString(TenantId!)}/{Uri.EscapeDataString(EnvironmentName!)}/";

    public void Validate()
    {
        if (!Enabled)
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central integration tests are disabled. Set BusinessCentralIntegrationTests:Enabled=true after configuring an isolated test company.");
        }

        var missing = new Dictionary<string, string?>
        {
            [nameof(TenantId)] = TenantId,
            [nameof(EnvironmentName)] = EnvironmentName,
            [nameof(CompanyId)] = CompanyId,
            [nameof(ExpectedCompanyName)] = ExpectedCompanyName,
            [nameof(ClientId)] = ClientId,
            [nameof(ClientSecret)] = ClientSecret
        }.Where(setting => string.IsNullOrWhiteSpace(setting.Value)).Select(setting => setting.Key).ToList();

        if (missing.Count != 0)
        {
            throw new InvalidOperationException(
                $"Business Central integration testing is enabled but these settings are missing: {string.Join(", ", missing)}. Store secrets in user-secrets or environment variables.");
        }

        if (!Guid.TryParse(TenantId, out _))
        {
            throw new InvalidOperationException("BusinessCentralIntegrationTests:TenantId must be a tenant GUID.");
        }

        if (!Guid.TryParse(CompanyId, out _))
        {
            throw new InvalidOperationException("BusinessCentralIntegrationTests:CompanyId must be a company GUID.");
        }

        if (!Guid.TryParse(ClientId, out _))
        {
            throw new InvalidOperationException("BusinessCentralIntegrationTests:ClientId must be an application client GUID.");
        }

        if (EnvironmentName!.Equals("Production", StringComparison.OrdinalIgnoreCase) && !AllowProductionEnvironment)
        {
            throw new InvalidOperationException(
                "Business Central integration tests refuse to target an environment named Production unless BusinessCentralIntegrationTests:AllowProductionEnvironment=true is explicitly configured.");
        }

        if (CorrelationReservationsEnabled && string.IsNullOrWhiteSpace(CorrelationApiRoute))
        {
            throw new InvalidOperationException(
                "BusinessCentralIntegrationTests:CorrelationApiRoute is required when correlation reservations are enabled.");
        }
    }

    public void ValidateSalesInvoiceWrites()
    {
        if (!SalesInvoiceWritesEnabled)
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central sales-invoice integration tests are disabled. These tests create financial " +
                "documents. Confirm that the isolated company has no workflow, job queue, extension, or other " +
                "automation that posts test invoices, then set " +
                "BusinessCentralIntegrationTests:SalesInvoiceWritesEnabled=true for a deliberate local run.");
        }
    }

    public void ValidateSalesCreditMemoWrites()
    {
        if (!SalesCreditMemoWritesEnabled)
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central sales-credit-memo integration tests are disabled. These tests create financial " +
                "documents. Confirm that the isolated company has no workflow, job queue, extension, or other " +
                "automation that posts, sends, cancels, or otherwise transitions test credit memos, then set " +
                "BusinessCentralIntegrationTests:SalesCreditMemoWritesEnabled=true for a deliberate local run.");
        }
    }

    public void ValidatePurchaseInvoiceWrites()
    {
        if (!PurchaseInvoiceWritesEnabled)
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central purchase-invoice integration tests are disabled. These tests create financial " +
                "documents. Confirm that the isolated company has no workflow, job queue, extension, or other " +
                "automation that posts test invoices, then set " +
                "BusinessCentralIntegrationTests:PurchaseInvoiceWritesEnabled=true for a deliberate local run.");
        }
    }

    public void ValidatePurchaseCreditMemoWrites()
    {
        if (!PurchaseCreditMemoWritesEnabled)
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central purchase-credit-memo integration tests are disabled. These tests create financial " +
                "documents. Confirm that the isolated company has no workflow, job queue, extension, or other " +
                "automation that posts or cancels test credit memos, then set " +
                "BusinessCentralIntegrationTests:PurchaseCreditMemoWritesEnabled=true for a deliberate local run.");
        }
    }

    public void ValidateCustomerPaymentWrites()
    {
        if (!CustomerPaymentWritesEnabled)
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central customer-payment integration tests are disabled. These tests create " +
                "unposted payment journals and payment lines. Confirm that the isolated company has no " +
                "workflow, job queue, extension, or other automation that posts payment journals, then set " +
                "BusinessCentralIntegrationTests:CustomerPaymentWritesEnabled=true for a deliberate local run.");
        }
    }

    public void ValidateVendorPaymentWrites()
    {
        if (!VendorPaymentWritesEnabled)
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central vendor-payment integration tests are disabled. These tests create " +
                "unposted vendor-payment journals and payment lines. Confirm that the isolated company has no " +
                "automation that posts or consumes payment journals, then set " +
                "BusinessCentralIntegrationTests:VendorPaymentWritesEnabled=true for a deliberate local run.");
        }
    }

    public void ValidateGeneralJournalWrites()
    {
        if (!GeneralJournalWritesEnabled)
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central general-journal integration tests are disabled. These tests create unposted " +
                "general journal batches and lines. Confirm that the isolated company has no automation that " +
                "posts journal batches, then set BusinessCentralIntegrationTests:GeneralJournalWritesEnabled=true " +
                "for a deliberate local run.");
        }
    }
}

public sealed class BusinessCentralIntegrationTestSkippedException(string message, Exception? innerException = null)
    : Exception(message, innerException);
