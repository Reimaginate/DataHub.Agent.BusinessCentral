using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Xunit;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralIntegrationSettingsTests
{
    [Fact]
    public void ProductionEnvironmentRequiresExplicitOptIn()
    {
        var settings = CreateValidSettings();

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        Assert.Contains("AllowProductionEnvironment=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionEnvironmentAllowsExplicitIsolatedCompanyOptIn()
    {
        var settings = CreateValidSettings();
        settings.AllowProductionEnvironment = true;

        settings.Validate();
    }

    [Fact]
    public void SalesInvoiceWritesRequireSeparateExplicitOptIn()
    {
        var settings = CreateValidSettings();

        var exception = Assert.Throws<BusinessCentralIntegrationTestSkippedException>(
            settings.ValidateSalesInvoiceWrites);

        Assert.Contains("SalesInvoiceWritesEnabled=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SalesInvoiceWritesAllowExplicitLocalOptIn()
    {
        var settings = CreateValidSettings();
        settings.SalesInvoiceWritesEnabled = true;

        settings.ValidateSalesInvoiceWrites();
    }

    [Fact]
    public void SalesCreditMemoWritesRequireSeparateExplicitOptIn()
    {
        var settings = CreateValidSettings();

        var exception = Assert.Throws<BusinessCentralIntegrationTestSkippedException>(
            settings.ValidateSalesCreditMemoWrites);

        Assert.Contains("SalesCreditMemoWritesEnabled=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SalesCreditMemoWritesAllowExplicitLocalOptIn()
    {
        var settings = CreateValidSettings();
        settings.SalesCreditMemoWritesEnabled = true;

        settings.ValidateSalesCreditMemoWrites();
    }

    [Fact]
    public void PurchaseInvoiceWritesRequireSeparateExplicitOptIn()
    {
        var settings = CreateValidSettings();

        var exception = Assert.Throws<BusinessCentralIntegrationTestSkippedException>(
            settings.ValidatePurchaseInvoiceWrites);

        Assert.Contains("PurchaseInvoiceWritesEnabled=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PurchaseInvoiceWritesAllowExplicitLocalOptIn()
    {
        var settings = CreateValidSettings();
        settings.PurchaseInvoiceWritesEnabled = true;

        settings.ValidatePurchaseInvoiceWrites();
    }

    [Fact]
    public void PurchaseCreditMemoWritesRequireSeparateExplicitOptIn()
    {
        var settings = CreateValidSettings();

        var exception = Assert.Throws<BusinessCentralIntegrationTestSkippedException>(
            settings.ValidatePurchaseCreditMemoWrites);

        Assert.Contains("PurchaseCreditMemoWritesEnabled=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PurchaseCreditMemoWritesAllowExplicitLocalOptIn()
    {
        var settings = CreateValidSettings();
        settings.PurchaseCreditMemoWritesEnabled = true;

        settings.ValidatePurchaseCreditMemoWrites();
    }

    [Fact]
    public void CustomerPaymentWritesRequireSeparateExplicitOptIn()
    {
        var settings = CreateValidSettings();

        var exception = Assert.Throws<BusinessCentralIntegrationTestSkippedException>(
            settings.ValidateCustomerPaymentWrites);

        Assert.Contains("CustomerPaymentWritesEnabled=true", exception.Message, StringComparison.Ordinal);
        Assert.Contains("unposted payment journals", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CustomerPaymentWritesAllowExplicitLocalOptIn()
    {
        var settings = CreateValidSettings();
        settings.CustomerPaymentWritesEnabled = true;

        settings.ValidateCustomerPaymentWrites();
    }

    [Fact]
    public void VendorPaymentWritesRequireSeparateExplicitOptIn()
    {
        var settings = CreateValidSettings();
        var exception = Assert.Throws<BusinessCentralIntegrationTestSkippedException>(settings.ValidateVendorPaymentWrites);
        Assert.Contains("VendorPaymentWritesEnabled=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VendorPaymentWritesAllowExplicitLocalOptIn()
    {
        var settings = CreateValidSettings();
        settings.VendorPaymentWritesEnabled = true;
        settings.ValidateVendorPaymentWrites();
    }

    [Fact]
    public void GeneralJournalWritesRequireSeparateExplicitOptIn()
    {
        var settings = CreateValidSettings();
        var exception = Assert.Throws<BusinessCentralIntegrationTestSkippedException>(settings.ValidateGeneralJournalWrites);
        Assert.Contains("GeneralJournalWritesEnabled=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneralJournalWritesAllowExplicitLocalOptIn()
    {
        var settings = CreateValidSettings();
        settings.GeneralJournalWritesEnabled = true;
        settings.ValidateGeneralJournalWrites();
    }

    [Fact]
    public void CorrelationReservationsAreOptionalAndUseTheExtensionRouteByDefault()
    {
        var settings = CreateValidSettings();
        settings.AllowProductionEnvironment = true;

        settings.Validate();

        Assert.False(settings.CorrelationReservationsEnabled);
        Assert.Equal("api/reimaginate/dataHub/v1.0", settings.CorrelationApiRoute);
    }

    [Fact]
    public void EnabledCorrelationReservationsRequireAnApiRoute()
    {
        var settings = CreateValidSettings();
        settings.AllowProductionEnvironment = true;
        settings.CorrelationReservationsEnabled = true;
        settings.CorrelationApiRoute = " ";

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        Assert.Contains("CorrelationApiRoute", exception.Message, StringComparison.Ordinal);
    }

    private static BusinessCentralIntegrationSettings CreateValidSettings() => new()
    {
        Enabled = true,
        TenantId = Guid.NewGuid().ToString(),
        EnvironmentName = "Production",
        CompanyId = Guid.NewGuid().ToString(),
        ExpectedCompanyName = "DataHub Test",
        AllowProductionEnvironment = false,
        ClientId = Guid.NewGuid().ToString(),
        ClientSecret = "test-only-placeholder"
    };
}
