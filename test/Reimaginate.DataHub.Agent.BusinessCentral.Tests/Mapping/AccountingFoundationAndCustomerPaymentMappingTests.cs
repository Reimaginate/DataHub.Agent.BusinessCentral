using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralBankAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.BankAccount;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralCustomerPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPayment;
using BusinessCentralCustomerPaymentDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPaymentDimensionSetLine;
using BusinessCentralCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPaymentJournal;
using BusinessCentralFinancialDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimension;
using BusinessCentralFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue;
using BusinessCentralGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubCustomerPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPayment;
using DataHubCustomerPaymentDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPaymentDimension;
using DataHubCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPaymentJournal;
using DataHubFinancialDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimension;
using DataHubFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimensionValue;
using DataHubGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class AccountingFoundationAndCustomerPaymentMappingTests
{
    [Fact]
    public void ContractsUseStandardRoutesMarkersDatesAndParentScopes()
    {
        Assert.Equal("accounts", Url<BusinessCentralGeneralLedgerAccount>());
        Assert.Equal("bankAccounts", Url<BusinessCentralBankAccount>());
        Assert.Equal("dimensions", Url<BusinessCentralFinancialDimension>());
        Assert.Equal("dimensionValues", Url<BusinessCentralFinancialDimensionValue>());
        Assert.Equal("customerPaymentJournals", Url<BusinessCentralCustomerPaymentJournal>());
        Assert.Equal("customerPayments", Url<BusinessCentralCustomerPayment>());
        Assert.Equal("dimensionSetLines", Url<BusinessCentralCustomerPaymentDimension>());

        AssertParent<BusinessCentralCustomerPayment>(
            "customerPaymentJournals", nameof(BusinessCentralCustomerPayment.JournalId));
        AssertParent<BusinessCentralCustomerPaymentDimension>(
            "customerPayments", nameof(BusinessCentralCustomerPaymentDimension.ParentId));
        Assert.Single(typeof(BusinessCentralCustomerPayment).GetProperty(nameof(BusinessCentralCustomerPayment.PostingDate))!
            .GetCustomAttributes(typeof(BusinessCentralDateAttribute), true));
        Assert.Empty(new[] { typeof(BusinessCentralCustomerPayment), typeof(BusinessCentralCustomerPaymentDimension) }
            .SelectMany(type => type.GetProperties())
            .SelectMany(property => property.GetCustomAttributes(typeof(BusinessCentralCreateRecoveryKeyAttribute), true)));
        var journalRecovery = Assert.Single(typeof(BusinessCentralCustomerPaymentJournal).GetProperties()
            .SelectMany(property => property.GetCustomAttributes(typeof(BusinessCentralCreateRecoveryKeyAttribute), true))
            .Cast<BusinessCentralCreateRecoveryKeyAttribute>());
        Assert.Equal("code", journalRecovery.FieldName);
    }

    [Fact]
    public async Task FoundationSnapshotsMapInboundAndPreserveRelationships()
    {
        var modified = DateTimeOffset.UtcNow;
        var dimensionId = Guid.NewGuid();
        var account = await new MapBusinessCentralGeneralLedgerAccountToGeneralLedgerAccount().MapAsync(
            new BusinessCentralGeneralLedgerAccount
            {
                Id = Guid.NewGuid().ToString(), Number = "4000", DisplayName = "Revenue",
                Category = "Income", DirectPosting = true, NetChange = 125m, LastModifiedDateTime = modified
            }, CancellationToken.None);
        var value = await new MapBusinessCentralFinancialDimensionValueToFinancialDimensionValue().MapAsync(
            new BusinessCentralFinancialDimensionValue
            {
                Id = Guid.NewGuid().ToString(), DimensionId = dimensionId, Code = "NORTH",
                DisplayName = "North", LastModifiedDateTime = modified
            }, CancellationToken.None);

        Assert.Equal("4000", account.Number);
        Assert.Equal("Income", account.Category);
        Assert.Equal(125m, account.NetChange);
        Assert.Equal(modified, account.lastUpdated);
        AssertReference<DataHubFinancialDimension, BusinessCentralFinancialDimension>(value.Dimension, dimensionId);
    }

    [Fact]
    public async Task AccountingFoundationsRejectOutboundWrites()
    {
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            new MapGeneralLedgerAccountToBusinessCentralGeneralLedgerAccount().MapAsync(
                new DataHubGeneralLedgerAccount { id = "gl-1" }, CancellationToken.None));

        Assert.Contains("inbound-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UntrackedBlankJournalGetsStableBoundedCode()
    {
        var source = new DataHubCustomerPaymentJournal { id = "payment-journal-1", DisplayName = null };
        var mapper = new MapCustomerPaymentJournalToBusinessCentralCustomerPaymentJournal();

        var first = await mapper.MapAsync(source, CancellationToken.None);
        var second = await mapper.MapAsync(source, CancellationToken.None);

        Assert.StartsWith("DH", first.Code, StringComparison.Ordinal);
        Assert.Equal(10, first.Code!.Length);
        Assert.Equal(first.Code, second.Code);
        Assert.Equal($"Data Hub {first.Code}", first.DisplayName);
    }

    [Fact]
    public async Task JournalMapsOptionalBalancingAccountBothWays()
    {
        var accountId = Guid.NewGuid();
        var source = new DataHubCustomerPaymentJournal
        {
            id = "journal-1",
            Code = "DHJOURNAL",
            DisplayName = "Data Hub Receipts",
            BalancingAccount = Reference<DataHubGeneralLedgerAccount>("gl-1")
        };
        var outbound = await new MapCustomerPaymentJournalToBusinessCentralCustomerPaymentJournal()
            .MapAsync(source, CancellationToken.None, Cache<DataHubGeneralLedgerAccount>("gl-1", "generalledgeraccount", accountId));
        var inbound = await new MapBusinessCentralCustomerPaymentJournalToCustomerPaymentJournal()
            .MapAsync(new BusinessCentralCustomerPaymentJournal
            {
                Id = Guid.NewGuid().ToString(), Code = source.Code, DisplayName = source.DisplayName,
                BalancingAccountId = accountId, BalancingAccountNumber = "10100"
            }, CancellationToken.None);

        Assert.Equal(accountId, outbound.BalancingAccountId);
        AssertReference<DataHubGeneralLedgerAccount, BusinessCentralGeneralLedgerAccount>(inbound.BalancingAccount, accountId);
        Assert.Equal("10100", inbound.BalancingAccountNumber);
    }

    [Fact]
    public async Task PaymentMapsParentCustomerInvoiceAndEditableValues()
    {
        var journalId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var source = new DataHubCustomerPayment
        {
            id = "payment-1",
            PaymentJournal = Reference<DataHubCustomerPaymentJournal>("journal-1"),
            Customer = Reference<DataHubAccount>("account-1"),
            AppliesToSalesInvoice = Reference<DataHubSalesInvoice>("invoice-1"),
            PostingDate = "2026-08-15",
            Amount = 125.50m,
            Description = "Receipt",
            Comment = "Data Hub test"
        };
        var cache = Cache<DataHubCustomerPaymentJournal>("journal-1", "customerpaymentjournal", journalId);
        cache[typeof(DataHubAccount).Name] = CacheObjects("account-1", "customer", customerId);
        cache[typeof(DataHubSalesInvoice).Name] = CacheObjects("invoice-1", "salesinvoice", invoiceId);

        var result = await new MapCustomerPaymentToBusinessCentralCustomerPayment()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(journalId, result.JournalId);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(invoiceId, result.AppliesToInvoiceId);
        Assert.Equal(source.PostingDate, result.PostingDate);
        Assert.Equal(source.Amount, result.Amount);
        Assert.StartsWith("DH-PAY-", result.DocumentNumber, StringComparison.Ordinal);
        Assert.Equal(20, result.DocumentNumber!.Length);
        Assert.Null(result.LineNumber);
    }

    [Fact]
    public async Task PaymentInboundMapsReferencesAndBusinessCentralLineNumber()
    {
        var journalId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var source = new BusinessCentralCustomerPayment
        {
            Id = Guid.NewGuid().ToString(), JournalId = journalId, CustomerId = customerId,
            AppliesToInvoiceId = invoiceId, LineNumber = 10000, DocumentNumber = "PAY-1", Amount = 42m
        };

        var result = await new MapBusinessCentralCustomerPaymentToCustomerPayment()
            .MapAsync(source, CancellationToken.None);

        AssertReference<DataHubCustomerPaymentJournal, BusinessCentralCustomerPaymentJournal>(result.PaymentJournal, journalId);
        AssertReference<DataHubAccount, BusinessCentralCustomer>(result.Customer, customerId);
        AssertReference<DataHubSalesInvoice, BusinessCentralSalesInvoice>(result.AppliesToSalesInvoice, invoiceId);
        Assert.Equal(10000, result.LineNumber);
        Assert.Equal(42m, result.Amount);
    }

    [Fact]
    public async Task PaymentDimensionMapsParentAndValueBothWays()
    {
        var paymentId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var source = new DataHubCustomerPaymentDimension
        {
            id = "dimension-line-1",
            Payment = Reference<DataHubCustomerPayment>("payment-1"),
            DimensionValue = Reference<DataHubFinancialDimensionValue>("value-1"),
            Code = "DEPARTMENT"
        };
        var cache = Cache<DataHubCustomerPayment>("payment-1", "customerpayment", paymentId);
        cache[typeof(DataHubFinancialDimensionValue).Name] = CacheObjects("value-1", "financialdimensionvalue", valueId);

        var outbound = await new MapCustomerPaymentDimensionToBusinessCentralCustomerPaymentDimension()
            .MapAsync(source, CancellationToken.None, cache);
        var inbound = await new MapBusinessCentralCustomerPaymentDimensionToCustomerPaymentDimension()
            .MapAsync(new BusinessCentralCustomerPaymentDimension
            {
                Id = Guid.NewGuid().ToString(), ParentId = paymentId, ValueId = valueId,
                Code = "DEPARTMENT", ValueCode = "SALES"
            }, CancellationToken.None);

        Assert.Equal(paymentId, outbound.ParentId);
        Assert.Equal(valueId, outbound.ValueId);
        AssertReference<DataHubCustomerPayment, BusinessCentralCustomerPayment>(inbound.Payment, paymentId);
        AssertReference<DataHubFinancialDimensionValue, BusinessCentralFinancialDimensionValue>(inbound.DimensionValue, valueId);
    }

    private static string Url<T>() => Assert.Single(typeof(T)
        .GetCustomAttributes(typeof(BusinessCentralUrlAttribute), true)
        .Cast<BusinessCentralUrlAttribute>()).Url;

    private static void AssertParent<T>(string url, string property)
    {
        var attribute = Assert.Single(typeof(T)
            .GetCustomAttributes(typeof(BusinessCentralParentUrlAttribute), true)
            .Cast<BusinessCentralParentUrlAttribute>());
        Assert.Equal(url, attribute.ParentUrl);
        Assert.Equal(property, attribute.ParentIdPropertyName);
    }

    private static EntityReference Reference<T>(string id) =>
        new() { EntityType = typeof(T).Name, EntityId = id };

    private static void AssertReference<TDataHub, TBusinessCentral>(EntityReference? reference, Guid id)
    {
        var external = Assert.IsType<ExternalEntityReference>(reference);
        Assert.Equal(typeof(TDataHub).Name, external.EntityType);
        Assert.Equal(typeof(TBusinessCentral).Name, external.SourceEntityType);
        Assert.Equal(id.ToString(), external.EntityId);
    }

    private static Dictionary<string, object> Cache<T>(string id, string type, Guid externalId) where T : DataHubEntity =>
        new() { [typeof(T).Name] = CacheObjects(id, type, externalId) };

    private static List<JObject> CacheObjects(string id, string type, Guid externalId) =>
    [
        new JObject
        {
            [nameof(DataHubEntity.id)] = id,
            [nameof(DataHubEntity.alternateKeys)] = new JArray
            {
                new JObject
                {
                    [nameof(AlternateKey.Key)] = $"businesscentral.{type}",
                    [nameof(AlternateKey.Value)] = externalId.ToString()
                }
            }
        }
    ];
}
