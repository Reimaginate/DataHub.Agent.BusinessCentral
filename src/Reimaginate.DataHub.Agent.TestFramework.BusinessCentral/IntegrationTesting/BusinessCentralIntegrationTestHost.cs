using System.Net;
using System.Net.Http.Json;
using System.Runtime.ExceptionServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json.Linq;
using OneOf;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Contracts;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.CreateBusinessCentralRecords;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.UpdateBusinessCentralRecords;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Queries.GetSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeSpecificBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeUpdatedBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeBusinessCentralEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeDependencyBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ResolveResolutionPromises;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RetrieveUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeFailuresToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeSuccessesToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncFailuresToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncSuccessesToDataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDataHubEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDependencyDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralSyncMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting.Containers;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.Agent.TestFramework;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral;
using Reimaginate.DataHub.Auth;
using Reimaginate.DataHub.Config;
using Reimaginate.DataHub.Helpers;
using Reimaginate.DataHub.Requests.External.Client.DeserializeClientRequest;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Models.Duplicates;
using Reimaginate.DataHub.SharedModels.Core.Models.Jobs;
using Reimaginate.DataHub.SharedModels.Markers;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.DataServices.Cosmos;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Contact;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;
using DataHubSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoiceLine;
using DataHubSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemo;
using DataHubSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesCreditMemoLine;
using DataHubSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrder;
using DataHubSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesOrderLine;
using DataHubQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Quote;
using DataHubQuoteLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.QuoteLine;
using DataHubSupplier = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Supplier;
using DataHubPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrder;
using DataHubPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseOrderLine;
using DataHubPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoice;
using DataHubPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseInvoiceLine;
using DataHubPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemo;
using DataHubPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseCreditMemoLine;
using DataHubSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipment;
using DataHubSalesShipmentLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesShipmentLine;
using DataHubPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceipt;
using DataHubPurchaseReceiptLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PurchaseReceiptLine;
using DataHubGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerAccount;
using DataHubBankAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.BankAccount;
using DataHubFinancialDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimension;
using DataHubFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimensionValue;
using DataHubCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPaymentJournal;
using DataHubCustomerPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPayment;
using DataHubCustomerPaymentDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.CustomerPaymentDimension;
using DataHubVendorPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPaymentJournal;
using DataHubVendorPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPayment;
using DataHubVendorPaymentDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.VendorPaymentDimension;
using DataHubGeneralJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournal;
using DataHubGeneralJournalLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournalLine;
using DataHubGeneralJournalDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournalLineDimension;
using DataHubGeneralLedgerEntry = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralLedgerEntry;
using DataHubItemLedgerEntry = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.ItemLedgerEntry;
using DataHubCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Currency;
using DataHubPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PaymentTerm;
using DataHubPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.PaymentMethod;
using DataHubUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.UnitOfMeasure;
using DataHubInventoryLocation = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.InventoryLocation;
using DataHubProductVariant = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.ProductVariant;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralContact = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Contact;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;
using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemoLine;
using BusinessCentralSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrder;
using BusinessCentralSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrderLine;
using BusinessCentralSalesQuote = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuote;
using BusinessCentralSalesQuoteLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesQuoteLine;
using BusinessCentralVendor = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Vendor;
using BusinessCentralPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrder;
using BusinessCentralPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrderLine;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;
using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemoLine;
using BusinessCentralSalesShipment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipment;
using BusinessCentralSalesShipmentLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesShipmentLine;
using BusinessCentralPurchaseReceipt = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceipt;
using BusinessCentralPurchaseReceiptLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseReceiptLine;
using BusinessCentralGeneralLedgerAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerAccount;
using BusinessCentralBankAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.BankAccount;
using BusinessCentralFinancialDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimension;
using BusinessCentralFinancialDimensionValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue;
using BusinessCentralCustomerPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPaymentJournal;
using BusinessCentralCustomerPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPayment;
using BusinessCentralCustomerPaymentDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.CustomerPaymentDimensionSetLine;
using BusinessCentralVendorPaymentJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPaymentJournal;
using BusinessCentralVendorPayment = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPayment;
using BusinessCentralVendorPaymentDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.VendorPaymentDimensionSetLine;
using BusinessCentralGeneralJournal = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournal;
using BusinessCentralGeneralJournalLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournalLine;
using BusinessCentralGeneralJournalDimension = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournalDimensionSetLine;
using BusinessCentralGeneralLedgerEntry = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralLedgerEntry;
using BusinessCentralItemLedgerEntry = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.ItemLedgerEntry;
using BusinessCentralCurrency = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Currency;
using BusinessCentralPaymentTerm = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentTerm;
using BusinessCentralPaymentMethod = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PaymentMethod;
using BusinessCentralUnitOfMeasure = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.UnitOfMeasure;
using BusinessCentralLocation = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Location;
using BusinessCentralItemVariant = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.ItemVariant;
using IntegrationMapper = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapper;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public sealed class BusinessCentralIntegrationTestHost : IDisposable
{
    private const string SeedDataSource = "BusinessCentralIntegrationSeed";

    private BusinessCentralIntegrationTestHost(
        IConfigurationRoot configuration,
        IServiceProvider serviceProvider,
        BusinessCentralIntegrationSettings settings,
        string testInstanceId)
    {
        Configuration = configuration;
        ServiceProvider = serviceProvider;
        Settings = settings;
        TestInstanceId = testInstanceId;
    }

    public IConfigurationRoot Configuration { get; }

    public IServiceProvider ServiceProvider { get; }

    public BusinessCentralIntegrationSettings Settings { get; }

    public string TestInstanceId { get; }

    public string TestPrefix => $"DHIT-{DateTimeOffset.UtcNow:yyyyMMdd}-{TestInstanceId[^8..]}";

    public IMediator Mediator => ServiceProvider.GetRequiredService<IMediator>();

    public IDataHubClient DataHubClient => ServiceProvider.GetRequiredService<IDataHubClient>();

    public IBusinessCentralODataService BusinessCentralService =>
        ServiceProvider.GetRequiredService<IBusinessCentralODataService>();

    public static BusinessCentralIntegrationTestHost Create(Type userSecretsMarkerType)
    {
        var baseConfiguration = IntegrationConfiguration.Build(userSecretsMarkerType);
        var settings = new BusinessCentralIntegrationSettings();
        baseConfiguration.GetSection(BusinessCentralIntegrationSettings.SectionName).Bind(settings);
        settings.Validate();

        var cosmos = DataHubCosmosDbEmulator.Current;
        if (cosmos?.SkipReason is not null)
        {
            throw new BusinessCentralIntegrationTestSkippedException(cosmos.SkipReason);
        }

        if (cosmos is null || string.IsNullOrWhiteSpace(cosmos.ConnectionString))
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central integration tests require TestFixtures:UseLocalCosmosDb=true so the Cosmos fixture provides a connection string.");
        }

        var redis = DataHubRedisContainer.Current;
        if (redis?.SkipReason is not null)
        {
            throw new BusinessCentralIntegrationTestSkippedException(redis.SkipReason);
        }

        if (redis is null || string.IsNullOrWhiteSpace(redis.ConnectionString))
        {
            throw new BusinessCentralIntegrationTestSkippedException(
                "Business Central integration tests require the Redis fixture to provide a connection string.");
        }

        var testInstance = new AgentTestInstance("BusinessCentralIntegration");
        var configuration = new ConfigurationBuilder()
            .AddConfiguration(baseConfiguration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataHub:ProcessingLockOptions:RedisClientOptions:ConnString"] = redis.ConnectionString,
                ["BusinessCentralAgentOptions:ProcessingLockOptions:RedisClientOptions:ConnString"] = redis.ConnectionString,
                ["BusinessCentralAgentOptions:AgentId"] = $"BusinessCentralIntegration-{testInstance.Id}",
                ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:BaseUrl"] = settings.BaseUrl,
                ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:CompanyId"] = settings.CompanyId,
                ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:ApiRoute"] = "api/v2.0",
                ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:CorrelationReservationsEnabled"] =
                    settings.CorrelationReservationsEnabled.ToString(),
                ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:CorrelationApiRoute"] =
                    settings.CorrelationApiRoute
            })
            .Build();

        var serviceProvider = new AgentTestServiceBuilder()
            .Add((services, config) => ConfigureServices(services, config, cosmos, settings, testInstance))
            .Build(configuration);

        return new BusinessCentralIntegrationTestHost(
            configuration,
            serviceProvider,
            settings,
            testInstance.Id);
    }

    public async Task PreflightAsync(CancellationToken cancellationToken = default)
    {
        var client = ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("BusinessCentral");
        var companyId = Settings.CompanyId;

        using var companiesResponse = await client.GetAsync("api/v2.0/companies?$top=1", cancellationToken);
        await EnsureSuccessAsync(companiesResponse, "list companies", cancellationToken);

        using var companyResponse = await client.GetAsync($"api/v2.0/companies({companyId})", cancellationToken);
        await EnsureSuccessAsync(companyResponse, "read the configured company", cancellationToken);
        var company = JObject.Parse(await companyResponse.Content.ReadAsStringAsync(cancellationToken));
        var companyName = company.Value<string>("displayName") ?? company.Value<string>("name");
        if (!string.Equals(companyName, Settings.ExpectedCompanyName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Configured company {companyId} is '{companyName}', not the expected isolated test company '{Settings.ExpectedCompanyName}'.");
        }

        using var customersResponse = await client.GetAsync(
            $"api/v2.0/companies({companyId})/customers?$top=1",
            cancellationToken);
        await EnsureSuccessAsync(customersResponse, "read customers", cancellationToken);

        var preflightToken = Guid.NewGuid().ToString("N");
        var preflightNumber = $"DHIT-PF-{preflightToken[..12]}".ToUpperInvariant();
        var preflightName = $"{TestPrefix}-preflight-{preflightToken[..8]}";
        string? preflightCustomerId = null;
        Exception? preflightFailure = null;

        try
        {
            using var createResponse = await client.PostAsJsonAsync(
                $"api/v2.0/companies({companyId})/customers",
                new
                {
                    number = preflightNumber,
                    displayName = preflightName,
                    type = "Company"
                },
                cancellationToken);
            await EnsureSuccessAsync(createResponse, "create a preflight customer", cancellationToken);

            var createdCustomer = JObject.Parse(
                await createResponse.Content.ReadAsStringAsync(cancellationToken));
            preflightCustomerId = createdCustomer.Value<string>("id");
            if (!Guid.TryParse(preflightCustomerId, out _))
            {
                throw new InvalidOperationException(
                    "Business Central created the preflight customer without returning a valid customer id.");
            }

            using var updateRequest = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/v2.0/companies({companyId})/customers({preflightCustomerId})")
            {
                Content = JsonContent.Create(new
                {
                    displayName = $"{preflightName}-updated"
                })
            };
            updateRequest.Headers.TryAddWithoutValidation("If-Match", "*");

            using var updateResponse = await client.SendAsync(updateRequest, cancellationToken);
            await EnsureSuccessAsync(
                updateResponse,
                "update a preflight customer's name",
                cancellationToken);
        }
        catch (Exception exception)
        {
            preflightFailure = exception;
        }

        if (preflightCustomerId is not null)
        {
            try
            {
                using var deleteRequest = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"api/v2.0/companies({companyId})/customers({preflightCustomerId})");
                deleteRequest.Headers.TryAddWithoutValidation("If-Match", "*");

                using var deleteResponse = await client.SendAsync(deleteRequest, CancellationToken.None);
                await EnsureSuccessAsync(
                    deleteResponse,
                    "delete the preflight customer",
                    CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                preflightFailure = preflightFailure is null
                    ? cleanupException
                    : new AggregateException(
                        "Business Central preflight failed and its customer cleanup also failed.",
                        preflightFailure,
                        cleanupException);
            }
        }

        if (preflightFailure is not null)
        {
            ExceptionDispatchInfo.Capture(preflightFailure).Throw();
        }
    }

    public async Task PreflightItemsAsync(CancellationToken cancellationToken = default)
    {
        await PreflightAsync(cancellationToken);

        var client = ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("BusinessCentral");
        var companyId = Settings.CompanyId;
        using var itemsResponse = await client.GetAsync(
            $"api/v2.0/companies({companyId})/items?$top=1",
            cancellationToken);
        await EnsureSuccessAsync(itemsResponse, "read items", cancellationToken);

        var token = Guid.NewGuid().ToString("N");
        var number = $"DHIT-PF-{token[..12]}".ToUpperInvariant();
        var displayName = $"{TestPrefix}-item-preflight-{token[..8]}";
        string? itemId = null;
        Exception? preflightFailure = null;

        try
        {
            using var createResponse = await client.PostAsJsonAsync(
                $"api/v2.0/companies({companyId})/items",
                new
                {
                    number,
                    displayName,
                    type = "Service",
                    unitPrice = 1.25m
                },
                cancellationToken);
            await EnsureSuccessAsync(createResponse, "create a preflight item", cancellationToken);

            var createdItem = JObject.Parse(await createResponse.Content.ReadAsStringAsync(cancellationToken));
            itemId = createdItem.Value<string>("id");
            if (!Guid.TryParse(itemId, out _))
            {
                throw new InvalidOperationException(
                    "Business Central created the preflight item without returning a valid item id.");
            }

            using var updateRequest = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/v2.0/companies({companyId})/items({itemId})")
            {
                Content = JsonContent.Create(new { unitPrice = 2.50m })
            };
            updateRequest.Headers.TryAddWithoutValidation("If-Match", "*");

            using var updateResponse = await client.SendAsync(updateRequest, cancellationToken);
            await EnsureSuccessAsync(updateResponse, "update a preflight item's price", cancellationToken);
        }
        catch (Exception exception)
        {
            preflightFailure = exception;
        }

        if (itemId is not null)
        {
            try
            {
                using var deleteRequest = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"api/v2.0/companies({companyId})/items({itemId})");
                deleteRequest.Headers.TryAddWithoutValidation("If-Match", "*");

                using var deleteResponse = await client.SendAsync(deleteRequest, CancellationToken.None);
                await EnsureSuccessAsync(deleteResponse, "delete the preflight item", CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                preflightFailure = preflightFailure is null
                    ? cleanupException
                    : new AggregateException(
                        "Business Central item preflight failed and its cleanup also failed.",
                        preflightFailure,
                        cleanupException);
            }
        }

        if (preflightFailure is not null)
        {
            ExceptionDispatchInfo.Capture(preflightFailure).Throw();
        }
    }

    public async Task<BusinessCentralSalesOrderTestReferences> PreflightSalesOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        await PreflightAsync(cancellationToken);

        var token = Guid.NewGuid().ToString("N");
        var customers = await GetPreflightEntitiesAsync<BusinessCentralCustomer>(
            100,
            "read candidate sales-order customers",
            cancellationToken);
        var items = await GetPreflightEntitiesAsync<BusinessCentralItem>(
            100,
            "read candidate sales-order items",
            cancellationToken);

        if (customers.Count == 0 || items.Count == 0)
        {
            throw new InvalidOperationException(
                "Business Central sales-order integration tests require at least one existing customer " +
                "and one existing item in the isolated sandbox company. These reference records are " +
                "used read-only and are never deleted by the tests.");
        }

        BusinessCentralSalesOrder? order = null;
        BusinessCentralSalesOrderLine? line = null;
        Exception? lastCustomerFailure = null;

        foreach (var customer in customers)
        {
            try
            {
                order = await CreatePreflightEntityAsync(new BusinessCentralSalesOrder
                {
                    DataHubCorrelationId = Guid.NewGuid(),
                    ExternalDocumentNumber = $"DHIT-PF-{token[..16]}".ToUpperInvariant(),
                    OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    CustomerId = Guid.Parse(customer.Id!)
                }, $"create a preflight sales order for customer {customer.Number ?? customer.Id}", cancellationToken);
            }
            catch (Exception exception)
            {
                lastCustomerFailure = exception;
                if (IsAuthorizationFailure(exception)) throw;
                continue;
            }

            try
            {
                var updatedOrder = new BusinessCentralSalesOrder
                {
                    Id = order.Id,
                    ETag = order.ETag,
                    ExternalDocumentNumber = $"{order.ExternalDocumentNumber}-U"
                };
                var orderUpdate = await BusinessCentralService.UpdateEntityAsync(updatedOrder, cancellationToken);
                if (!orderUpdate.Success)
                {
                    throw new InvalidOperationException(
                        "Business Central could not update the preflight sales order.",
                        orderUpdate.Exception);
                }

                Exception? lastItemFailure = null;
                foreach (var item in items)
                {
                    try
                    {
                        line = await CreatePreflightEntityAsync(new BusinessCentralSalesOrderLine
                        {
                            DataHubCorrelationId = Guid.NewGuid(),
                            DocumentId = Guid.Parse(order.Id!),
                            ItemId = Guid.Parse(item.Id!),
                            LineType = "Item",
                            Description = "Data Hub preflight line",
                            Quantity = 1m,
                            UnitPrice = 12.50m
                        }, $"create a preflight sales order line for item {item.Number ?? item.Id}", cancellationToken);

                        var updatedLine = new BusinessCentralSalesOrderLine
                        {
                            Id = line.Id,
                            ETag = line.ETag,
                            Quantity = 2m
                        };
                        var lineUpdate = await BusinessCentralService.UpdateEntityAsync(updatedLine, cancellationToken);
                        if (!lineUpdate.Success)
                        {
                            throw new InvalidOperationException(
                                "Business Central could not update the preflight sales order line.",
                                lineUpdate.Exception);
                        }

                        return new BusinessCentralSalesOrderTestReferences(customer, item);
                    }
                    catch (Exception exception)
                    {
                        lastItemFailure = exception;
                        if (IsAuthorizationFailure(exception)) throw;
                    }
                    finally
                    {
                        await DeletePreflightEntityAsync<BusinessCentralSalesOrderLine>(line?.Id);
                        line = null;
                    }
                }

                throw new InvalidOperationException(
                    "No existing Business Central item could be used on a sales order. Configure at least " +
                    "one unblocked sandbox item with a General Product Posting Group, then rerun the tests.",
                    lastItemFailure);
            }
            finally
            {
                await DeletePreflightEntityAsync<BusinessCentralSalesOrder>(order?.Id);
                order = null;
            }
        }

        throw new InvalidOperationException(
            "No existing Business Central customer could be used on a sales order. Configure at least one " +
            "unblocked sandbox customer with General Business and Customer Posting Groups, then rerun the tests.",
            lastCustomerFailure);
    }

    public async Task<BusinessCentralSalesInvoiceTestReferences> PreflightSalesInvoicesAsync(
        CancellationToken cancellationToken = default)
    {
        Settings.ValidateSalesInvoiceWrites();

        // Reuse the qualified customer and item discovery already exercised by the sales-order
        // preflight, then validate invoice-specific create, update, line, and cleanup access.
        var references = await PreflightSalesOrdersAsync(cancellationToken);
        var token = Guid.NewGuid().ToString("N");
        BusinessCentralSalesInvoice? invoice = null;
        BusinessCentralSalesInvoiceLine? line = null;

        try
        {
            invoice = await CreatePreflightEntityAsync(new BusinessCentralSalesInvoice
            {
                ExternalDocumentNumber = $"DHIT-PF-I-{token[..14]}".ToUpperInvariant(),
                InvoiceDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DueDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
                CustomerId = Guid.Parse(references.Customer.Id!)
            }, "create a preflight draft sales invoice", cancellationToken);
            EnsureDraftSalesInvoice(invoice, "created");

            var invoiceUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralSalesInvoice
                {
                    Id = invoice.Id,
                    ETag = invoice.ETag,
                    ExternalDocumentNumber = $"{invoice.ExternalDocumentNumber}-U"
                },
                cancellationToken);
            if (!invoiceUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft sales invoice.",
                    invoiceUpdate.Exception);
            }
            EnsureDraftSalesInvoice(
                invoice = invoiceUpdate.ResultingEntity ?? throw new InvalidOperationException(
                    "Business Central returned no preflight sales invoice after updating it."),
                "updated");

            line = await CreatePreflightEntityAsync(new BusinessCentralSalesInvoiceLine
            {
                DocumentId = Guid.Parse(invoice.Id!),
                ItemId = Guid.Parse(references.Item.Id!),
                LineType = "Item",
                Description = "Data Hub invoice preflight line",
                Quantity = 1m,
                UnitPrice = 12.50m
            }, "create a preflight draft sales invoice line", cancellationToken);

            var lineUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralSalesInvoiceLine
                {
                    Id = line.Id,
                    ETag = line.ETag,
                    DocumentId = Guid.Parse(invoice.Id!),
                    Quantity = 2m
                },
                cancellationToken);
            if (!lineUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft sales invoice line.",
                    lineUpdate.Exception);
            }

            var invoiceAfterLineUpdate = await BusinessCentralService.GetEntityAsync<BusinessCentralSalesInvoice>(
                Guid.Parse(invoice.Id!),
                cancellationToken);
            if (invoiceAfterLineUpdate.IsT2) throw invoiceAfterLineUpdate.AsT2;
            if (invoiceAfterLineUpdate.IsT1)
            {
                using var httpResponse = invoiceAfterLineUpdate.AsT1;
                await EnsureSuccessAsync(
                    httpResponse,
                    "verify the preflight sales invoice remains a draft after line changes",
                    cancellationToken);
            }
            EnsureDraftSalesInvoice(
                invoice = invoiceAfterLineUpdate.AsT0 ?? throw new InvalidOperationException(
                    "The preflight sales invoice disappeared after its line was updated."),
                "updated with an item line");

            return new BusinessCentralSalesInvoiceTestReferences(
                references.Customer,
                references.Item);
        }
        finally
        {
            await DeletePreflightSalesInvoiceAsync(line?.Id, invoice?.Id);
        }
    }

    public async Task<BusinessCentralSalesQuoteTestReferences> PreflightSalesQuotesAsync(
        CancellationToken cancellationToken = default)
    {
        // Reuse the customer and item qualification already exercised by the sales-order
        // preflight, then prove that draft quote headers and lines can be created, updated,
        // read, and deleted. No quote lifecycle action is ever invoked by this harness.
        var references = await PreflightSalesOrdersAsync(cancellationToken);
        var token = Guid.NewGuid().ToString("N");
        BusinessCentralSalesQuote? quote = null;
        BusinessCentralSalesQuoteLine? line = null;

        try
        {
            quote = await CreatePreflightEntityAsync(new BusinessCentralSalesQuote
            {
                ExternalDocumentNumber = $"DHIT-PF-Q-{token[..14]}".ToUpperInvariant(),
                DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DueDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
                ValidUntilDate = DateTime.UtcNow.AddDays(14).ToString("yyyy-MM-dd"),
                CustomerId = Guid.Parse(references.Customer.Id!)
            }, "create a preflight draft sales quote", cancellationToken);
            if (!Guid.TryParse(quote.Id, out var quoteId))
            {
                throw new InvalidOperationException(
                    "Business Central returned no valid id after creating the preflight sales quote.");
            }
            EnsureDraftSalesQuote(quote, "created");

            var quoteUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralSalesQuote
                {
                    Id = quote.Id,
                    ETag = quote.ETag,
                    ExternalDocumentNumber = $"{quote.ExternalDocumentNumber}-U"
                },
                cancellationToken);
            if (!quoteUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft sales quote.",
                    quoteUpdate.Exception);
            }

            quote = quoteUpdate.ResultingEntity ?? throw new InvalidOperationException(
                "Business Central returned no preflight sales quote after updating it.");
            if (!Guid.TryParse(quote.Id, out var updatedQuoteId) || updatedQuoteId != quoteId)
            {
                throw new InvalidOperationException(
                    "Business Central changed or omitted the preflight sales quote id during update.");
            }
            EnsureDraftSalesQuote(quote, "updated");

            line = await CreatePreflightEntityAsync(new BusinessCentralSalesQuoteLine
            {
                DocumentId = Guid.Parse(quote.Id!),
                ItemId = Guid.Parse(references.Item.Id!),
                LineType = "Item",
                Description = "Data Hub quote preflight line",
                Quantity = 1m,
                UnitPrice = 12.50m
            }, "create a preflight draft sales quote line", cancellationToken);
            if (!Guid.TryParse(line.Id, out var lineId) || line.DocumentId != quoteId)
            {
                throw new InvalidOperationException(
                    "Business Central returned an invalid id or parent document id for the preflight sales quote line.");
            }

            var lineUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralSalesQuoteLine
                {
                    Id = line.Id,
                    ETag = line.ETag,
                    DocumentId = Guid.Parse(quote.Id!),
                    Quantity = 2m
                },
                cancellationToken);
            if (!lineUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft sales quote line.",
                    lineUpdate.Exception);
            }

            var updatedLine = lineUpdate.ResultingEntity ?? throw new InvalidOperationException(
                "Business Central returned no preflight sales quote line after updating it.");
            if (!Guid.TryParse(updatedLine.Id, out var updatedLineId) ||
                updatedLineId != lineId ||
                updatedLine.DocumentId != quoteId)
            {
                throw new InvalidOperationException(
                    "Business Central changed or omitted the preflight sales quote line id or parent document id " +
                    "during update.");
            }
            line = updatedLine;

            var quoteAfterLineUpdate = await BusinessCentralService.GetEntityAsync<BusinessCentralSalesQuote>(
                quoteId,
                cancellationToken);
            if (quoteAfterLineUpdate.IsT2) throw quoteAfterLineUpdate.AsT2;
            if (quoteAfterLineUpdate.IsT1)
            {
                using var httpResponse = quoteAfterLineUpdate.AsT1;
                await EnsureSuccessAsync(
                    httpResponse,
                    "verify the preflight sales quote remains a draft after line changes",
                    cancellationToken);
            }
            var refreshedQuote = quoteAfterLineUpdate.AsT0 ?? throw new InvalidOperationException(
                "The preflight sales quote disappeared after its line was updated.");
            if (!Guid.TryParse(refreshedQuote.Id, out var refreshedQuoteId) || refreshedQuoteId != quoteId)
            {
                throw new InvalidOperationException(
                    "Business Central changed or omitted the preflight sales quote id after its line was updated.");
            }
            quote = refreshedQuote;
            EnsureDraftSalesQuote(quote, "updated with an item line");

            return new BusinessCentralSalesQuoteTestReferences(
                references.Customer,
                references.Item);
        }
        finally
        {
            await DeletePreflightSalesQuoteAsync(line?.Id, quote?.Id);
        }
    }

    public async Task<BusinessCentralSalesCreditMemoTestReferences> PreflightSalesCreditMemosAsync(
        CancellationToken cancellationToken = default)
    {
        Settings.ValidateSalesCreditMemoWrites();

        // Reuse the customer and item qualification already exercised by the sales-order
        // preflight, then prove draft-only credit-memo header and parent-scoped item-line CRUD.
        // This harness never invokes post, send, cancel, corrective, or other lifecycle APIs.
        var references = await PreflightSalesOrdersAsync(cancellationToken);
        var token = Guid.NewGuid().ToString("N");
        var externalDocumentNumber = $"DHIT-PF-CM-{token[..14]}".ToUpperInvariant();
        BusinessCentralSalesCreditMemo? creditMemo = null;
        BusinessCentralSalesCreditMemoLine? line = null;

        try
        {
            creditMemo = await CreatePreflightEntityAsync(new BusinessCentralSalesCreditMemo
            {
                ExternalDocumentNumber = externalDocumentNumber,
                CreditMemoDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                CustomerId = Guid.Parse(references.Customer.Id!)
            }, "create a preflight draft sales credit memo", cancellationToken);
            if (!Guid.TryParse(creditMemo.Id, out var creditMemoId))
            {
                throw new InvalidOperationException(
                    "Business Central returned no valid id after creating the preflight sales credit memo.");
            }
            EnsureDraftSalesCreditMemo(creditMemo, "created");

            var creditMemoUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralSalesCreditMemo
                {
                    Id = creditMemo.Id,
                    ETag = creditMemo.ETag,
                    ExternalDocumentNumber = $"{creditMemo.ExternalDocumentNumber}-U"
                },
                cancellationToken);
            if (!creditMemoUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft sales credit memo.",
                    creditMemoUpdate.Exception);
            }

            creditMemo = creditMemoUpdate.ResultingEntity ?? throw new InvalidOperationException(
                "Business Central returned no preflight sales credit memo after updating it.");
            if (!Guid.TryParse(creditMemo.Id, out var updatedCreditMemoId) || updatedCreditMemoId != creditMemoId)
            {
                throw new InvalidOperationException(
                    "Business Central changed or omitted the preflight sales credit memo id during update.");
            }
            EnsureDraftSalesCreditMemo(creditMemo, "updated");

            line = await CreatePreflightEntityAsync(new BusinessCentralSalesCreditMemoLine
            {
                DocumentId = creditMemoId,
                ItemId = Guid.Parse(references.Item.Id!),
                LineType = "Item",
                Description = "Data Hub credit memo preflight line",
                Quantity = 1m,
                UnitPrice = 12.50m
            }, "create a preflight draft sales credit memo line", cancellationToken);
            if (!Guid.TryParse(line.Id, out var lineId) || line.DocumentId != creditMemoId)
            {
                throw new InvalidOperationException(
                    "Business Central returned an invalid id or parent document id for the preflight sales " +
                    "credit memo line.");
            }

            var lineUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralSalesCreditMemoLine
                {
                    Id = line.Id,
                    ETag = line.ETag,
                    DocumentId = creditMemoId,
                    Quantity = 2m
                },
                cancellationToken);
            if (!lineUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft sales credit memo line.",
                    lineUpdate.Exception);
            }

            var updatedLine = lineUpdate.ResultingEntity ?? throw new InvalidOperationException(
                "Business Central returned no preflight sales credit memo line after updating it.");
            if (!Guid.TryParse(updatedLine.Id, out var updatedLineId) ||
                updatedLineId != lineId ||
                updatedLine.DocumentId != creditMemoId)
            {
                throw new InvalidOperationException(
                    "Business Central changed or omitted the preflight sales credit memo line id or parent " +
                    "document id during update.");
            }
            line = updatedLine;

            var creditMemoAfterLineUpdate =
                await BusinessCentralService.GetEntityAsync<BusinessCentralSalesCreditMemo>(
                    creditMemoId,
                    cancellationToken);
            if (creditMemoAfterLineUpdate.IsT2) throw creditMemoAfterLineUpdate.AsT2;
            if (creditMemoAfterLineUpdate.IsT1)
            {
                using var httpResponse = creditMemoAfterLineUpdate.AsT1;
                await EnsureSuccessAsync(
                    httpResponse,
                    "verify the preflight sales credit memo remains a draft after line changes",
                    cancellationToken);
            }

            var refreshedCreditMemo = creditMemoAfterLineUpdate.AsT0 ?? throw new InvalidOperationException(
                "The preflight sales credit memo disappeared after its line was updated.");
            if (!Guid.TryParse(refreshedCreditMemo.Id, out var refreshedCreditMemoId) ||
                refreshedCreditMemoId != creditMemoId)
            {
                throw new InvalidOperationException(
                    "Business Central changed or omitted the preflight sales credit memo id after its line " +
                    "was updated.");
            }
            creditMemo = refreshedCreditMemo;
            EnsureDraftSalesCreditMemo(creditMemo, "updated with an item line");

            var refreshedLines = await FindSalesCreditMemoLinesAsync(creditMemoId, cancellationToken);
            var refreshedLine = refreshedLines.SingleOrDefault(candidate =>
                Guid.TryParse(candidate.Id, out var candidateId) && candidateId == lineId);
            if (refreshedLine is null || refreshedLine.DocumentId != creditMemoId || refreshedLine.Quantity != 2m)
            {
                throw new InvalidOperationException(
                    "Business Central could not read back the updated preflight sales credit memo line from " +
                    "its current parent.");
            }
            line = refreshedLine;

            return new BusinessCentralSalesCreditMemoTestReferences(
                references.Customer,
                references.Item);
        }
        finally
        {
            await DeletePreflightSalesCreditMemoAsync(
                externalDocumentNumber,
                line?.Id,
                creditMemo?.Id);
        }
    }

    public async Task<BusinessCentralPurchaseOrderTestReferences> PreflightPurchaseOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        await PreflightAsync(cancellationToken);

        var vendors = await GetPreflightEntitiesAsync<BusinessCentralVendor>(
            100,
            "read candidate purchase-order vendors",
            cancellationToken);
        var items = await GetPreflightEntitiesAsync<BusinessCentralItem>(
            100,
            "read candidate purchase-order items",
            cancellationToken);

        if (vendors.Count == 0 || items.Count == 0)
        {
            throw new InvalidOperationException(
                "Business Central purchase-order integration tests require at least one existing vendor " +
                "and one existing item in the isolated sandbox company. These reference records are " +
                "used read-only and are never deleted by the tests.");
        }

        BusinessCentralPurchaseOrder? order = null;
        BusinessCentralPurchaseOrderLine? line = null;
        Exception? lastVendorFailure = null;

        foreach (var vendor in vendors)
        {
            try
            {
                order = await CreatePreflightEntityAsync(new BusinessCentralPurchaseOrder
                {
                    DataHubCorrelationId = Guid.NewGuid(),
                    OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    RequestedReceiptDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                    VendorId = Guid.Parse(vendor.Id!)
                }, $"create a preflight purchase order for vendor {vendor.Number ?? vendor.Id}", cancellationToken);
            }
            catch (Exception exception)
            {
                lastVendorFailure = exception;
                if (IsAuthorizationFailure(exception)) throw;
                continue;
            }

            try
            {
                EnsureDraftPurchaseOrder(order, "created");
                var orderUpdate = await BusinessCentralService.UpdateEntityAsync(
                    new BusinessCentralPurchaseOrder
                    {
                        Id = order.Id,
                        ETag = order.ETag,
                        RequestedReceiptDate = DateTime.UtcNow.AddDays(8).ToString("yyyy-MM-dd")
                    },
                    cancellationToken);
                if (!orderUpdate.Success)
                {
                    throw new InvalidOperationException(
                        "Business Central could not update the preflight purchase order.",
                        orderUpdate.Exception);
                }

                order = orderUpdate.ResultingEntity ?? throw new InvalidOperationException(
                    "Business Central returned no preflight purchase order after updating it.");
                EnsureDraftPurchaseOrder(order, "updated");

                Exception? lastItemFailure = null;
                foreach (var item in items)
                {
                    try
                    {
                        line = await CreatePreflightEntityAsync(new BusinessCentralPurchaseOrderLine
                        {
                            DataHubCorrelationId = Guid.NewGuid(),
                            DocumentId = Guid.Parse(order.Id!),
                            ItemId = Guid.Parse(item.Id!),
                            LineType = "Item",
                            Description = "Data Hub purchase-order preflight line",
                            Quantity = 1m,
                            DirectUnitCost = 12.50m
                        }, $"create a preflight purchase order line for item {item.Number ?? item.Id}", cancellationToken);

                        var lineUpdate = await BusinessCentralService.UpdateEntityAsync(
                            new BusinessCentralPurchaseOrderLine
                            {
                                Id = line.Id,
                                ETag = line.ETag,
                                DocumentId = Guid.Parse(order.Id!),
                                Quantity = 2m
                            },
                            cancellationToken);
                        if (!lineUpdate.Success)
                        {
                            throw new InvalidOperationException(
                                "Business Central could not update the preflight purchase order line.",
                                lineUpdate.Exception);
                        }

                        return new BusinessCentralPurchaseOrderTestReferences(vendor, item);
                    }
                    catch (Exception exception)
                    {
                        lastItemFailure = exception;
                        if (IsAuthorizationFailure(exception)) throw;
                    }
                    finally
                    {
                        await DeletePreflightEntityAsync<BusinessCentralPurchaseOrderLine>(
                            line?.Id,
                            verifyDeleted: true);
                        line = null;
                    }
                }

                throw new InvalidOperationException(
                    "No existing Business Central item could be used on a purchase order. Configure at least " +
                    "one unblocked sandbox item with a General Product Posting Group, then rerun the tests.",
                    lastItemFailure);
            }
            finally
            {
                await DeletePreflightEntityAsync<BusinessCentralPurchaseOrder>(
                    order?.Id,
                    verifyDeleted: true);
                order = null;
            }
        }

        throw new InvalidOperationException(
            "No existing Business Central vendor could be used on a purchase order. Configure at least one " +
            "unblocked sandbox vendor with General Business and Vendor Posting Groups, then rerun the tests.",
            lastVendorFailure);
    }

    public async Task<BusinessCentralPurchaseInvoiceTestReferences> PreflightPurchaseInvoicesAsync(
        CancellationToken cancellationToken = default)
    {
        Settings.ValidatePurchaseInvoiceWrites();

        var references = await PreflightPurchaseOrdersAsync(cancellationToken);
        var token = Guid.NewGuid().ToString("N");
        BusinessCentralPurchaseInvoice? invoice = null;
        BusinessCentralPurchaseInvoiceLine? line = null;

        try
        {
            invoice = await CreatePreflightEntityAsync(new BusinessCentralPurchaseInvoice
            {
                VendorInvoiceNumber = $"DHIT-PF-PI-{token[..12]}".ToUpperInvariant(),
                InvoiceDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DueDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
                VendorId = Guid.Parse(references.Vendor.Id!)
            }, "create a preflight draft purchase invoice", cancellationToken);
            EnsureDraftPurchaseInvoice(invoice, "created");

            var invoiceUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralPurchaseInvoice
                {
                    Id = invoice.Id,
                    ETag = invoice.ETag,
                    VendorInvoiceNumber = $"DHIT-PF-PI-U-{token[..10]}".ToUpperInvariant()
                },
                cancellationToken);
            if (!invoiceUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft purchase invoice.",
                    invoiceUpdate.Exception);
            }

            invoice = invoiceUpdate.ResultingEntity ?? throw new InvalidOperationException(
                "Business Central returned no preflight purchase invoice after updating it.");
            EnsureDraftPurchaseInvoice(invoice, "updated");

            line = await CreatePreflightEntityAsync(new BusinessCentralPurchaseInvoiceLine
            {
                DocumentId = Guid.Parse(invoice.Id!),
                ItemId = Guid.Parse(references.Item.Id!),
                LineType = "Item",
                Description = "Data Hub purchase-invoice preflight line",
                Quantity = 1m,
                UnitCost = 12.50m
            }, "create a preflight draft purchase invoice line", cancellationToken);

            var lineUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralPurchaseInvoiceLine
                {
                    Id = line.Id,
                    ETag = line.ETag,
                    DocumentId = Guid.Parse(invoice.Id!),
                    Quantity = 2m
                },
                cancellationToken);
            if (!lineUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft purchase invoice line.",
                    lineUpdate.Exception);
            }

            var invoiceAfterLineUpdate = await BusinessCentralService.GetEntityAsync<BusinessCentralPurchaseInvoice>(
                Guid.Parse(invoice.Id!),
                cancellationToken);
            if (invoiceAfterLineUpdate.IsT2) throw invoiceAfterLineUpdate.AsT2;
            if (invoiceAfterLineUpdate.IsT1)
            {
                using var httpResponse = invoiceAfterLineUpdate.AsT1;
                await EnsureSuccessAsync(
                    httpResponse,
                    "verify the preflight purchase invoice remains a draft after line changes",
                    cancellationToken);
            }

            invoice = invoiceAfterLineUpdate.AsT0 ?? throw new InvalidOperationException(
                "The preflight purchase invoice disappeared after its line was updated.");
            EnsureDraftPurchaseInvoice(invoice, "updated with an item line");

            return new BusinessCentralPurchaseInvoiceTestReferences(references.Vendor, references.Item);
        }
        finally
        {
            await DeletePurchaseInvoiceTestArtifactAsync(invoice?.Id, line?.Id);
        }
    }

    public async Task DeletePurchaseInvoiceTestArtifactAsync(
        string? invoiceId,
        params string?[] capturedLineIds)
    {
        var lineIdsToVerify = capturedLineIds
            .Where(lineId => !string.IsNullOrWhiteSpace(lineId))
            .Select(lineId => lineId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(invoiceId))
        {
            if (!Guid.TryParse(invoiceId, out var invoiceGuid))
            {
                throw new InvalidOperationException(
                    $"Cannot safely clean up purchase invoice '{invoiceId}' because its id is not a GUID.");
            }

            var initialCleanup = await DeleteCurrentPurchaseInvoiceAsync(
                invoiceGuid,
                allowDraftDelete: true);
            foreach (var capturedLine in initialCleanup.CapturedLines)
            {
                lineIdsToVerify.Add(capturedLine.Id!);
            }

            if (initialCleanup.Disposition ==
                BusinessCentralPurchaseInvoiceCleanupDisposition.DraftDeleted)
            {
                var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
                BusinessCentralPurchaseInvoice? afterDraftDelete;
                do
                {
                    afterDraftDelete = await GetPreflightEntityIfPresentAsync<BusinessCentralPurchaseInvoice>(
                        invoiceGuid);
                    if (afterDraftDelete is null) break;

                    if (!string.Equals(
                            afterDraftDelete.Status,
                            "Draft",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var placeholderCleanup = await DeleteCurrentPurchaseInvoiceAsync(
                            invoiceGuid,
                            allowDraftDelete: false);
                        if (placeholderCleanup.Disposition !=
                            BusinessCentralPurchaseInvoiceCleanupDisposition.NoSeriesPlaceholderDeleted)
                        {
                            throw new InvalidOperationException(
                                $"Purchase invoice {invoiceGuid} changed unexpectedly during guarded cleanup.");
                        }

                        foreach (var placeholderLine in placeholderCleanup.CapturedLines)
                        {
                            lineIdsToVerify.Add(placeholderLine.Id!);
                        }
                        break;
                    }

                    if (DateTimeOffset.UtcNow < timeoutAt)
                    {
                        await Task.Delay(250, CancellationToken.None);
                    }
                }
                while (DateTimeOffset.UtcNow < timeoutAt);

                if (afterDraftDelete is not null &&
                    string.Equals(afterDraftDelete.Status, "Draft", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Purchase invoice {invoiceGuid} remained Draft after Business Central acknowledged " +
                        "deletion. Cleanup did not retry the DELETE.");
                }
            }

            if (initialCleanup.Disposition !=
                BusinessCentralPurchaseInvoiceCleanupDisposition.NotFound)
            {
                await VerifyPreflightEntityDeletedAsync<BusinessCentralPurchaseInvoice>(invoiceId);
            }
        }

        foreach (var lineId in lineIdsToVerify)
        {
            await VerifyPreflightEntityDeletedAsync<BusinessCentralPurchaseInvoiceLine>(lineId);
        }
    }

    private Task<BusinessCentralPurchaseInvoiceCleanupResult> DeleteCurrentPurchaseInvoiceAsync(
        Guid invoiceId,
        bool allowDraftDelete) =>
        BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            invoiceId,
            (id, _) => GetPreflightEntityIfPresentAsync<BusinessCentralPurchaseInvoice>(id),
            ReadCompletePurchaseInvoiceLinesAsync,
            DeletePurchaseInvoiceWithExactETagAsync,
            allowDraftDelete,
            CancellationToken.None);

    private async Task<BusinessCentralPurchaseInvoiceLineSnapshot> ReadCompletePurchaseInvoiceLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralPurchaseInvoiceLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read the complete purchase-invoice line set for {documentId}",
                cancellationToken);
        }

        return new BusinessCentralPurchaseInvoiceLineSnapshot(
            response.AsT0.Count,
            response.AsT0.Value);
    }

    private async Task DeletePurchaseInvoiceWithExactETagAsync(
        BusinessCentralPurchaseInvoice invoice,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(invoice.ETag))
        {
            throw new InvalidOperationException(
                $"Purchase invoice {invoice.Id} has no ETag. Cleanup will not use a wildcard If-Match value.");
        }

        var response = await BusinessCentralService.DeleteEntityAsync(invoice, cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"delete purchase invoice {invoice.Id} with its exact ETag",
                cancellationToken);
        }
    }

    public async Task<BusinessCentralPurchaseCreditMemoTestReferences> PreflightPurchaseCreditMemosAsync(
        CancellationToken cancellationToken = default)
    {
        Settings.ValidatePurchaseCreditMemoWrites();

        var references = await PreflightPurchaseOrdersAsync(cancellationToken);
        var token = Guid.NewGuid().ToString("N");
        BusinessCentralPurchaseCreditMemo? creditMemo = null;
        BusinessCentralPurchaseCreditMemoLine? line = null;

        try
        {
            creditMemo = await CreatePreflightEntityAsync(new BusinessCentralPurchaseCreditMemo
            {
                VendorCreditMemoNumber = $"DHIT-PF-PCM-{token[..10]}".ToUpperInvariant(),
                CreditMemoDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                VendorId = Guid.Parse(references.Vendor.Id!)
            }, "create a preflight draft purchase credit memo", cancellationToken);
            EnsureDraftPurchaseCreditMemo(creditMemo, "created");

            var creditMemoUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralPurchaseCreditMemo
                {
                    Id = creditMemo.Id,
                    ETag = creditMemo.ETag,
                    VendorCreditMemoNumber = $"DHIT-PF-PCM-U-{token[..8]}".ToUpperInvariant()
                },
                cancellationToken);
            if (!creditMemoUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight draft purchase credit memo.",
                    creditMemoUpdate.Exception);
            }

            creditMemo = creditMemoUpdate.ResultingEntity ?? throw new InvalidOperationException(
                "Business Central returned no preflight purchase credit memo after updating it.");
            EnsureDraftPurchaseCreditMemo(creditMemo, "updated");

            line = await CreatePreflightEntityAsync(new BusinessCentralPurchaseCreditMemoLine
            {
                DocumentId = Guid.Parse(creditMemo.Id!),
                ItemId = Guid.Parse(references.Item.Id!),
                LineType = "Item",
                Description = "Data Hub purchase-credit-memo preflight line",
                Quantity = 1m,
                UnitCost = 12.50m
            }, "create a preflight purchase credit memo line", cancellationToken);

            var lineUpdate = await BusinessCentralService.UpdateEntityAsync(
                new BusinessCentralPurchaseCreditMemoLine
                {
                    Id = line.Id,
                    ETag = line.ETag,
                    DocumentId = Guid.Parse(creditMemo.Id!),
                    Quantity = 2m
                },
                cancellationToken);
            if (!lineUpdate.Success)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight purchase credit memo line.",
                    lineUpdate.Exception);
            }

            var afterLineUpdate = await BusinessCentralService.GetEntityAsync<BusinessCentralPurchaseCreditMemo>(
                Guid.Parse(creditMemo.Id!),
                cancellationToken);
            if (afterLineUpdate.IsT2) throw afterLineUpdate.AsT2;
            if (afterLineUpdate.IsT1)
            {
                using var httpResponse = afterLineUpdate.AsT1;
                await EnsureSuccessAsync(
                    httpResponse,
                    "verify the preflight purchase credit memo remains a draft",
                    cancellationToken);
            }

            creditMemo = afterLineUpdate.AsT0 ?? throw new InvalidOperationException(
                "The preflight purchase credit memo disappeared after its line was updated.");
            EnsureDraftPurchaseCreditMemo(creditMemo, "updated with an item line");

            return new BusinessCentralPurchaseCreditMemoTestReferences(references.Vendor, references.Item);
        }
        finally
        {
            await DeletePurchaseCreditMemoTestArtifactAsync(creditMemo?.Id, line?.Id);
        }
    }

    public async Task DeletePurchaseCreditMemoTestArtifactAsync(
        string? creditMemoId,
        params string?[] capturedLineIds)
    {
        var lineIdsToVerify = capturedLineIds
            .Where(lineId => !string.IsNullOrWhiteSpace(lineId))
            .Select(lineId => lineId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(creditMemoId))
        {
            if (!Guid.TryParse(creditMemoId, out var creditMemoGuid))
            {
                throw new InvalidOperationException(
                    $"Cannot safely clean up purchase credit memo '{creditMemoId}' because its id is not a GUID.");
            }

            var initialCleanup = await DeleteCurrentPurchaseCreditMemoAsync(
                creditMemoGuid,
                allowDraftDelete: true);
            foreach (var capturedLine in initialCleanup.CapturedLines)
            {
                lineIdsToVerify.Add(capturedLine.Id!);
            }

            if (initialCleanup.Disposition == BusinessCentralPurchaseCreditMemoCleanupDisposition.DraftDeleted)
            {
                var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
                BusinessCentralPurchaseCreditMemo? afterDraftDelete;
                do
                {
                    afterDraftDelete = await GetPreflightEntityIfPresentAsync<BusinessCentralPurchaseCreditMemo>(
                        creditMemoGuid);
                    if (afterDraftDelete is null) break;

                    if (!string.Equals(afterDraftDelete.Status, "Draft", StringComparison.OrdinalIgnoreCase))
                    {
                        var placeholderCleanup = await DeleteCurrentPurchaseCreditMemoAsync(
                            creditMemoGuid,
                            allowDraftDelete: false);
                        if (placeholderCleanup.Disposition !=
                            BusinessCentralPurchaseCreditMemoCleanupDisposition.NoSeriesPlaceholderDeleted)
                        {
                            throw new InvalidOperationException(
                                $"Purchase credit memo {creditMemoGuid} changed unexpectedly during guarded cleanup.");
                        }

                        foreach (var placeholderLine in placeholderCleanup.CapturedLines)
                        {
                            lineIdsToVerify.Add(placeholderLine.Id!);
                        }
                        break;
                    }

                    if (DateTimeOffset.UtcNow < timeoutAt)
                    {
                        await Task.Delay(250, CancellationToken.None);
                    }
                }
                while (DateTimeOffset.UtcNow < timeoutAt);

                if (afterDraftDelete is not null &&
                    string.Equals(afterDraftDelete.Status, "Draft", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Purchase credit memo {creditMemoGuid} remained Draft after deletion was acknowledged.");
                }
            }

            if (initialCleanup.Disposition != BusinessCentralPurchaseCreditMemoCleanupDisposition.NotFound)
            {
                await VerifyPreflightEntityDeletedAsync<BusinessCentralPurchaseCreditMemo>(creditMemoId);
            }
        }

        foreach (var lineId in lineIdsToVerify)
        {
            await VerifyPreflightEntityDeletedAsync<BusinessCentralPurchaseCreditMemoLine>(lineId);
        }
    }

    private Task<BusinessCentralPurchaseCreditMemoCleanupResult> DeleteCurrentPurchaseCreditMemoAsync(
        Guid creditMemoId,
        bool allowDraftDelete) =>
        BusinessCentralPurchaseCreditMemoCleanupFlow.DeleteCurrentAsync(
            creditMemoId,
            (id, _) => GetPreflightEntityIfPresentAsync<BusinessCentralPurchaseCreditMemo>(id),
            ReadCompletePurchaseCreditMemoLinesAsync,
            DeletePurchaseCreditMemoWithExactETagAsync,
            allowDraftDelete,
            CancellationToken.None);

    private async Task<BusinessCentralPurchaseCreditMemoLineSnapshot> ReadCompletePurchaseCreditMemoLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralPurchaseCreditMemoLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read the complete purchase-credit-memo line set for {documentId}",
                cancellationToken);
        }

        return new BusinessCentralPurchaseCreditMemoLineSnapshot(
            response.AsT0.Count,
            response.AsT0.Value);
    }

    private async Task DeletePurchaseCreditMemoWithExactETagAsync(
        BusinessCentralPurchaseCreditMemo creditMemo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(creditMemo.ETag))
        {
            throw new InvalidOperationException(
                $"Purchase credit memo {creditMemo.Id} has no ETag. Cleanup will not use a wildcard If-Match value.");
        }

        var response = await BusinessCentralService.DeleteEntityAsync(creditMemo, cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"delete purchase credit memo {creditMemo.Id} with its exact ETag",
                cancellationToken);
        }
    }

    public async Task<DataHubAccount> SeedAccountAsync(DataHubAccount account, CancellationToken cancellationToken = default)
    {
        var response = await DataHubClient.PostRequestAsync<MergeEntitiesRequest, MergeEntitiesResponse>(
            new MergeEntitiesRequest
            {
                DataSource = SeedDataSource,
                Requests =
                [
                    new MergeEntityRequest
                    {
                        DataSource = SeedDataSource,
                        DataHubEntityType = account.entityType,
                        SourceEntityType = typeof(DataHubAccount).Name,
                        SourceEntityId = account.id,
                        Data = JObject.FromObject(account)
                    }
                ]
            },
            cancellationToken);

        var result = response.Results.Single();
        if (!MergeOutcomes.IsSuccess(result.MergeOutcome))
        {
            throw new InvalidOperationException($"Data Hub seed merge failed: {result.FailureReason}");
        }

        return await GetAccountAsync(result.DataHubEntityId, cancellationToken);
    }

    public async Task<DataHubAccount> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var response = await DataHubClient.PostRequestAsync<GetDataHubEntityRequest, GetDataHubEntityResponse>(
            new GetDataHubEntityRequest { EntityType = typeof(DataHubAccount).Name, EntityId = accountId },
            cancellationToken);
        return response.Entity.ToObject<DataHubAccount>()!;
    }

    public async Task<DataHubAccount> PatchAccountAsync(
        string accountId,
        params (string Path, object Value)[] values)
    {
        var response = await DataHubClient.PostRequestAsync<PatchEntitiesRequest, PatchEntitiesResponse>(
            new PatchEntitiesRequest
            {
                DispatchNotifications = false,
                Requests =
                [
                    new PatchEntityRequest
                    {
                        DataSource = DataSources.DataHub,
                        EntityType = typeof(DataHubAccount).Name,
                        EntityId = accountId,
                        Operations = values.Select(value => new Patch
                        {
                            Operation = "set",
                            Path = value.Path,
                            Value = JToken.FromObject(value.Value)
                        }).ToList()
                    }
                ]
            },
            CancellationToken.None);

        if (!response.Success)
        {
            var failures = response.Results?
                .Where(result => !result.Success)
                .Select(result => result.FailureReason)
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .ToList() ?? [];

            var failureMessage = string.Join(Environment.NewLine, failures);
            if (string.IsNullOrWhiteSpace(failureMessage))
            {
                failureMessage = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            }

            throw new InvalidOperationException(
                $"Data Hub patch failed: {failureMessage}");
        }

        return await GetAccountAsync(accountId);
    }

    public async Task<ProcessDataHubEntitySyncResponse> SyncAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.TrySend<ProcessDataHubEntitySyncResponse>(
            new SyncSpecificDataHubEntitiesRequest<DataHubAccount, BusinessCentralCustomer>
            {
                EntityIds = [accountId],
                CorrelationId = TestInstanceId
            },
            cancellationToken);

        if (result.Item2 is not null)
        {
            throw result.Item2;
        }

        return result.Item1
            ?? throw new InvalidOperationException("Business Central account sync returned no response.");
    }

    public async Task<BusinessCentralAccountingFoundationTestReferences> GetAccountingFoundationTestReferencesAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await GetPreflightEntitiesAsync<BusinessCentralGeneralLedgerAccount>(
            100, "read G/L accounts for accounting-foundation tests", cancellationToken);
        var bankAccounts = await GetPreflightEntitiesAsync<BusinessCentralBankAccount>(
            100, "read bank accounts for accounting-foundation tests", cancellationToken);
        var dimensions = await GetPreflightEntitiesAsync<BusinessCentralFinancialDimension>(
            100, "read financial dimensions for accounting-foundation tests", cancellationToken);
        var values = await GetPreflightEntitiesAsync<BusinessCentralFinancialDimensionValue>(
            500, "read financial dimension values for accounting-foundation tests", cancellationToken);

        var postingAccount = accounts.FirstOrDefault(account =>
            string.Equals(account.AccountType, "Posting", StringComparison.OrdinalIgnoreCase) &&
            account.Blocked != true && account.DirectPosting == true);
        var bankAccount = bankAccounts.FirstOrDefault(account =>
            Guid.TryParse(account.Id, out _) &&
            !string.IsNullOrWhiteSpace(account.Number) &&
            account.Blocked != true);
        var dimension = dimensions.FirstOrDefault(candidate =>
            Guid.TryParse(candidate.Id, out var id) && values.Any(value => value.DimensionId == id));
        var dimensionId = Guid.TryParse(dimension?.Id, out var parsedDimensionId)
            ? parsedDimensionId
            : Guid.Empty;
        var dimensionValue = values.FirstOrDefault(value => value.DimensionId == dimensionId);

        if (postingAccount is null || bankAccount is null || dimension is null || dimensionValue is null)
        {
            throw new InvalidOperationException(
                "Business Central accounting-foundation tests require an unblocked posting G/L account, " +
                "an unblocked bank account with a number, and a dimension with at least one value in the isolated company. " +
                "These records are used read-only and are never modified or deleted by the tests.");
        }

        return new BusinessCentralAccountingFoundationTestReferences(
            postingAccount, bankAccount, dimension, dimensionValue);
    }

    private async Task<(BusinessCentralFinancialDimension Dimension, BusinessCentralFinancialDimensionValue Value)>
        GetUnusedFinancialDimensionAsync(
            IEnumerable<string?> existingDimensionCodes,
            string parentDescription,
            CancellationToken cancellationToken)
    {
        var usedCodes = existingDimensionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var dimensions = await GetPreflightEntitiesAsync<BusinessCentralFinancialDimension>(
            100, $"read financial dimensions for {parentDescription}", cancellationToken);
        var values = await GetPreflightEntitiesAsync<BusinessCentralFinancialDimensionValue>(
            500, $"read financial dimension values for {parentDescription}", cancellationToken);

        foreach (var dimension in dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimension.Code) ||
                usedCodes.Contains(dimension.Code) ||
                !Guid.TryParse(dimension.Id, out var dimensionId))
            {
                continue;
            }

            var value = values.FirstOrDefault(candidate => candidate.DimensionId == dimensionId);
            if (value is not null)
            {
                return (dimension, value);
            }
        }

        throw new InvalidOperationException(
            $"Business Central {parentDescription} tests require a financial dimension with at least one value " +
            "that is not already present through the parent record's default dimensions. Add a separate test " +
            "dimension/value in the isolated company or remove the conflicting default dimension.");
    }

    public async Task<BusinessCentralCustomerPaymentTestReferences> PreflightCustomerPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        Settings.ValidateCustomerPaymentWrites();
        await PreflightAsync(cancellationToken);

        var foundations = await GetAccountingFoundationTestReferencesAsync(cancellationToken);
        var customers = await GetPreflightEntitiesAsync<BusinessCentralCustomer>(
            100, "read candidate customer-payment customers", cancellationToken);
        var customer = customers.FirstOrDefault();
        if (customer is null || !Guid.TryParse(customer.Id, out var customerId))
        {
            throw new InvalidOperationException(
                "Business Central customer-payment integration tests require at least one existing unblocked " +
                "customer in the isolated company. The customer is used read-only and is never deleted.");
        }

        var token = Guid.NewGuid().ToString("N").ToUpperInvariant();
        BusinessCentralCustomerPaymentJournal? journal = null;
        BusinessCentralCustomerPayment? payment = null;
        BusinessCentralCustomerPaymentDimension? dimensionLine = null;

        try
        {
            journal = await CreatePreflightEntityAsync(new BusinessCentralCustomerPaymentJournal
            {
                Code = $"DH{token[..8]}",
                DisplayName = $"Data Hub test {token[..8]}",
                BalancingAccountId = Guid.Parse(foundations.GeneralLedgerAccount.Id!)
            }, "create a preflight customer payment journal", cancellationToken);

            payment = await CreatePreflightEntityAsync(new BusinessCentralCustomerPayment
            {
                JournalId = Guid.Parse(journal.Id!),
                CustomerId = customerId,
                PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DocumentNumber = $"DH-PF-{token[..13]}",
                ExternalDocumentNumber = $"DHIT-{token[..12]}",
                Amount = 1m,
                Description = "Data Hub payment preflight"
            }, "create a preflight customer payment", cancellationToken);

            var update = await BusinessCentralService.UpdateEntityAsync(new BusinessCentralCustomerPayment
            {
                Id = payment.Id,
                ETag = payment.ETag,
                JournalId = Guid.Parse(journal.Id!),
                Description = "Data Hub payment preflight updated"
            }, cancellationToken);
            if (!update.Success || update.ResultingEntity is null)
            {
                throw new InvalidOperationException(
                    "Business Central could not update the preflight customer payment.", update.Exception);
            }
            payment = update.ResultingEntity;

            var existingDimensions = await FindCustomerPaymentDimensionsAsync(
                Guid.Parse(payment.Id!), cancellationToken);
            var (dimension, dimensionValue) = await GetUnusedFinancialDimensionAsync(
                existingDimensions.Select(existing => existing.Code),
                "customer-payment dimension",
                cancellationToken);

            dimensionLine = await CreatePreflightEntityAsync(new BusinessCentralCustomerPaymentDimension
            {
                ParentId = Guid.Parse(payment.Id!),
                Code = dimension.Code,
                ValueId = Guid.Parse(dimensionValue.Id!)
            }, "create a preflight customer payment dimension", cancellationToken);

            return new BusinessCentralCustomerPaymentTestReferences(
                customer,
                foundations.GeneralLedgerAccount,
                dimension,
                dimensionValue);
        }
        finally
        {
            if (dimensionLine is not null)
            {
                await DeleteParentScopedBusinessCentralEntityWithExactETagAsync(
                    Guid.Parse(payment!.Id!), dimensionLine,
                    "preflight customer payment dimension", CancellationToken.None);
            }

            if (payment is not null && Guid.TryParse(payment.Id, out var paymentId))
            {
                var currentPayment = await GetPreflightEntityIfPresentAsync<BusinessCentralCustomerPayment>(paymentId);
                if (currentPayment is not null)
                {
                    await DeleteBusinessCentralEntityWithExactETagAsync(
                        currentPayment, "preflight customer payment", CancellationToken.None);
                }
            }

            if (journal is not null && Guid.TryParse(journal.Id, out var journalId))
            {
                var currentJournal = await GetPreflightEntityIfPresentAsync<BusinessCentralCustomerPaymentJournal>(journalId);
                if (currentJournal is not null)
                {
                    await DeleteBusinessCentralEntityWithExactETagAsync(
                        currentJournal, "preflight customer payment journal", CancellationToken.None);
                }
            }
        }
    }

    public async Task<IReadOnlyList<BusinessCentralCustomerPaymentJournal>> FindCustomerPaymentJournalsByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var escaped = code.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralCustomerPaymentJournal>(
            $"code eq '{escaped}'", top: 100, cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find customer payment journals by exact code", cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessCentralCustomerPayment>> FindCustomerPaymentsByDocumentNumberAsync(
        Guid journalId,
        string documentNumber,
        CancellationToken cancellationToken = default)
    {
        var escaped = documentNumber.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralCustomerPayment>(
            $"journalId eq {journalId} and documentNumber eq '{escaped}'",
            top: 100,
            cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find customer payments by exact document number", cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessCentralCustomerPaymentDimension>> FindCustomerPaymentDimensionsAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralCustomerPaymentDimension>(
            paymentId, top: 100, cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find customer payment dimensions", cancellationToken);
    }

    public async Task DeleteCustomerPaymentTestArtifactsAsync(
        Guid? journalId,
        Guid? paymentId,
        IReadOnlyCollection<Guid>? dimensionIds = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var dimensionId in dimensionIds ?? [])
        {
            if (!paymentId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Refusing to delete customer payment dimension {dimensionId} without its captured parent payment id.");
            }

            var dimension = await GetParentScopedEntityIfPresentAsync<BusinessCentralCustomerPaymentDimension>(
                paymentId.Value, dimensionId, cancellationToken);
            if (dimension is not null)
            {
                if (dimension.ParentId != paymentId.Value)
                {
                    throw new InvalidOperationException(
                        $"Refusing to delete customer payment dimension {dimension.Id} because its parent " +
                        $"'{dimension.ParentId?.ToString() ?? "<missing>"}' is not the captured test payment.");
                }

                await DeleteParentScopedBusinessCentralEntityWithExactETagAsync(
                    paymentId.Value, dimension, "customer payment dimension", cancellationToken);
            }
        }

        if (paymentId.HasValue)
        {
            var payment = await GetPreflightEntityIfPresentAsync<BusinessCentralCustomerPayment>(paymentId.Value);
            if (payment is not null)
            {
                if (payment.DocumentNumber?.StartsWith("DH-", StringComparison.OrdinalIgnoreCase) != true)
                {
                    throw new InvalidOperationException(
                        $"Refusing to delete customer payment {payment.Id} because document number " +
                        $"'{payment.DocumentNumber ?? "<missing>"}' is not test-owned.");
                }

                await DeleteBusinessCentralEntityWithExactETagAsync(
                    payment, "customer payment", cancellationToken);
            }
        }

        if (journalId.HasValue)
        {
            var journal = await GetPreflightEntityIfPresentAsync<BusinessCentralCustomerPaymentJournal>(journalId.Value);
            if (journal is not null)
            {
                if (journal.Code?.StartsWith("DH", StringComparison.OrdinalIgnoreCase) != true)
                {
                    throw new InvalidOperationException(
                        $"Refusing to delete customer payment journal {journal.Id} because code " +
                        $"'{journal.Code ?? "<missing>"}' is not test-owned.");
                }

                await DeleteBusinessCentralEntityWithExactETagAsync(
                    journal, "customer payment journal", cancellationToken);
            }
        }
    }

    public async Task<BusinessCentralVendorPaymentTestReferences> PreflightVendorPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        Settings.ValidateVendorPaymentWrites();
        await PreflightAsync(cancellationToken);
        var foundations = await GetAccountingFoundationTestReferencesAsync(cancellationToken);
        var vendors = await GetPreflightEntitiesAsync<BusinessCentralVendor>(100, "read candidate vendor-payment vendors", cancellationToken);
        var vendor = vendors.FirstOrDefault(candidate => Guid.TryParse(candidate.Id, out _))
            ?? throw new InvalidOperationException("Vendor Payment tests require an existing vendor in the isolated company.");
        var token = Guid.NewGuid().ToString("N").ToUpperInvariant();
        BusinessCentralVendorPaymentJournal? journal = null;
        BusinessCentralVendorPayment? payment = null;
        BusinessCentralVendorPaymentDimension? dimensionLine = null;
        try
        {
            journal = await CreatePreflightEntityAsync(new BusinessCentralVendorPaymentJournal
            {
                Code = $"DV{token[..8]}", DisplayName = $"Data Hub vendor {token[..6]}",
                BalancingAccountId = Guid.Parse(foundations.GeneralLedgerAccount.Id!)
            }, "create a preflight vendor payment journal", cancellationToken);
            payment = await CreatePreflightEntityAsync(new BusinessCentralVendorPayment
            {
                JournalId = Guid.Parse(journal.Id!), VendorId = Guid.Parse(vendor.Id!),
                PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"), DocumentNumber = $"DH-VPAY-{token[..12]}",
                ExternalDocumentNumber = $"DHIT-{token[..12]}", Amount = 1m,
                Description = "Data Hub vendor-payment preflight"
            }, "create a preflight vendor payment", cancellationToken);
            var update = await BusinessCentralService.UpdateEntityAsync(new BusinessCentralVendorPayment
            {
                Id = payment.Id, ETag = payment.ETag, JournalId = Guid.Parse(journal.Id!),
                Description = "Data Hub vendor-payment preflight updated"
            }, cancellationToken);
            if (!update.Success || update.ResultingEntity is null)
                throw new InvalidOperationException("Business Central could not update the preflight vendor payment.", update.Exception);
            payment = update.ResultingEntity;
            var existingDimensions = await FindVendorPaymentDimensionsAsync(Guid.Parse(payment.Id!), cancellationToken);
            var (dimension, dimensionValue) = await GetUnusedFinancialDimensionAsync(
                existingDimensions.Select(existing => existing.Code),
                "vendor-payment dimension",
                cancellationToken);
            dimensionLine = await CreatePreflightEntityAsync(new BusinessCentralVendorPaymentDimension
            {
                ParentId = Guid.Parse(payment.Id!), Code = dimension.Code,
                ValueId = Guid.Parse(dimensionValue.Id!)
            }, "create a preflight vendor payment dimension", cancellationToken);
            return new BusinessCentralVendorPaymentTestReferences(vendor, foundations.GeneralLedgerAccount, dimension, dimensionValue);
        }
        finally
        {
            if (dimensionLine is not null && payment is not null)
                await DeleteParentScopedBusinessCentralEntityWithExactETagAsync(Guid.Parse(payment.Id!), dimensionLine, "preflight vendor payment dimension", CancellationToken.None);
            if (payment is not null && Guid.TryParse(payment.Id, out var paymentId))
            {
                var current = await GetPreflightEntityIfPresentAsync<BusinessCentralVendorPayment>(paymentId);
                if (current is not null) await DeleteBusinessCentralEntityWithExactETagAsync(current, "preflight vendor payment", CancellationToken.None);
            }
            if (journal is not null && Guid.TryParse(journal.Id, out var journalId))
            {
                var current = await GetPreflightEntityIfPresentAsync<BusinessCentralVendorPaymentJournal>(journalId);
                if (current is not null) await DeleteBusinessCentralEntityWithExactETagAsync(current, "preflight vendor payment journal", CancellationToken.None);
            }
        }
    }

    public async Task<IReadOnlyList<BusinessCentralVendorPaymentJournal>> FindVendorPaymentJournalsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var escaped = code.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralVendorPaymentJournal>($"code eq '{escaped}'", top: 100, cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find vendor payment journals by exact code", cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessCentralVendorPayment>> FindVendorPaymentsByDocumentNumberAsync(Guid journalId, string documentNumber, CancellationToken cancellationToken = default)
    {
        var escaped = documentNumber.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralVendorPayment>($"journalId eq {journalId} and documentNumber eq '{escaped}'", top: 100, cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find vendor payments by exact journal and document number", cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessCentralVendorPaymentDimension>> FindVendorPaymentDimensionsAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralVendorPaymentDimension>(paymentId, top: 100, cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find vendor payment dimensions", cancellationToken);
    }

    public async Task DeleteVendorPaymentTestArtifactsAsync(Guid? journalId, Guid? paymentId, IReadOnlyCollection<Guid>? dimensionIds = null, CancellationToken cancellationToken = default)
    {
        foreach (var dimensionId in dimensionIds ?? [])
        {
            if (!paymentId.HasValue) throw new InvalidOperationException($"Cannot delete vendor payment dimension {dimensionId} without its parent id.");
            var dimension = await GetParentScopedEntityIfPresentAsync<BusinessCentralVendorPaymentDimension>(paymentId.Value, dimensionId, cancellationToken);
            if (dimension is not null)
            {
                if (dimension.ParentId != paymentId) throw new InvalidOperationException($"Vendor payment dimension {dimensionId} belongs to another payment.");
                await DeleteParentScopedBusinessCentralEntityWithExactETagAsync(paymentId.Value, dimension, "vendor payment dimension", cancellationToken);
            }
        }
        if (paymentId.HasValue)
        {
            var payment = await GetPreflightEntityIfPresentAsync<BusinessCentralVendorPayment>(paymentId.Value);
            if (payment is not null)
            {
                if (payment.DocumentNumber?.StartsWith("DH-VPAY-", StringComparison.OrdinalIgnoreCase) != true)
                    throw new InvalidOperationException($"Refusing to delete non-test vendor payment {payment.Id}.");
                await DeleteBusinessCentralEntityWithExactETagAsync(payment, "vendor payment", cancellationToken);
            }
        }
        if (journalId.HasValue)
        {
            var journal = await GetPreflightEntityIfPresentAsync<BusinessCentralVendorPaymentJournal>(journalId.Value);
            if (journal is not null)
            {
                if (journal.Code?.StartsWith("DV", StringComparison.OrdinalIgnoreCase) != true)
                    throw new InvalidOperationException($"Refusing to delete non-test vendor payment journal {journal.Id}.");
                await DeleteBusinessCentralEntityWithExactETagAsync(journal, "vendor payment journal", cancellationToken);
            }
        }
    }

    public async Task<BusinessCentralGeneralJournalTestReferences> PreflightGeneralJournalsAsync(CancellationToken cancellationToken = default)
    {
        Settings.ValidateGeneralJournalWrites();
        await PreflightAsync(cancellationToken);
        var foundations = await GetAccountingFoundationTestReferencesAsync(cancellationToken);
        var token = Guid.NewGuid().ToString("N").ToUpperInvariant();
        BusinessCentralGeneralJournal? journal = null;
        BusinessCentralGeneralJournalLine? line = null;
        BusinessCentralGeneralJournalDimension? dimensionLine = null;
        try
        {
            journal = await CreatePreflightEntityAsync(new BusinessCentralGeneralJournal
            {
                Code = $"DG{token[..8]}", DisplayName = $"Data Hub general {token[..6]}",
                BalancingAccountId = Guid.Parse(foundations.GeneralLedgerAccount.Id!)
            }, "create a preflight general journal", cancellationToken);
            line = await CreatePreflightEntityAsync(new BusinessCentralGeneralJournalLine
            {
                JournalId = Guid.Parse(journal.Id!), AccountType = "G/L Account",
                AccountId = Guid.Parse(foundations.GeneralLedgerAccount.Id!), PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DocumentNumber = $"DH-GL-{token[..12]}", Amount = 1m,
                Description = "Data Hub general-journal preflight", BalanceAccountType = "Bank Account",
                BalancingAccountNumber = foundations.BankAccount.Number
            }, "create a preflight general journal line", cancellationToken);
            var update = await BusinessCentralService.UpdateEntityAsync(new BusinessCentralGeneralJournalLine
            {
                Id = line.Id, ETag = line.ETag, JournalId = Guid.Parse(journal.Id!),
                Description = "Data Hub general-journal preflight updated"
            }, cancellationToken);
            if (!update.Success || update.ResultingEntity is null)
                throw new InvalidOperationException("Business Central could not update the preflight general journal line.", update.Exception);
            line = update.ResultingEntity;
            var existingDimensions = await FindGeneralJournalDimensionsAsync(Guid.Parse(line.Id!), cancellationToken);
            var (dimension, dimensionValue) = await GetUnusedFinancialDimensionAsync(
                existingDimensions.Select(existing => existing.Code),
                "general-journal dimension",
                cancellationToken);
            dimensionLine = await CreatePreflightEntityAsync(new BusinessCentralGeneralJournalDimension
            {
                ParentId = Guid.Parse(line.Id!), Code = dimension.Code,
                ValueId = Guid.Parse(dimensionValue.Id!)
            }, "create a preflight general journal dimension", cancellationToken);
            return new BusinessCentralGeneralJournalTestReferences(foundations.GeneralLedgerAccount, foundations.BankAccount, dimension, dimensionValue);
        }
        finally
        {
            if (dimensionLine is not null && line is not null)
                await DeleteParentScopedBusinessCentralEntityWithExactETagAsync(Guid.Parse(line.Id!), dimensionLine, "preflight general journal dimension", CancellationToken.None);
            if (line is not null && Guid.TryParse(line.Id, out var lineId))
            {
                var current = await GetPreflightEntityIfPresentAsync<BusinessCentralGeneralJournalLine>(lineId);
                if (current is not null) await DeleteBusinessCentralEntityWithExactETagAsync(current, "preflight general journal line", CancellationToken.None);
            }
            if (journal is not null && Guid.TryParse(journal.Id, out var journalId))
            {
                var current = await GetPreflightEntityIfPresentAsync<BusinessCentralGeneralJournal>(journalId);
                if (current is not null) await DeleteBusinessCentralEntityWithExactETagAsync(current, "preflight general journal", CancellationToken.None);
            }
        }
    }

    public async Task<IReadOnlyList<BusinessCentralGeneralJournal>> FindGeneralJournalsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var escaped = code.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralGeneralJournal>($"code eq '{escaped}'", top: 100, cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find general journals by exact code", cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessCentralGeneralJournalLine>> FindGeneralJournalLinesAsync(Guid journalId, CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralGeneralJournalLine>(journalId, top: 100, cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find general journal lines", cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessCentralGeneralJournalDimension>> FindGeneralJournalDimensionsAsync(Guid lineId, CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralGeneralJournalDimension>(lineId, top: 100, cancellationToken: cancellationToken);
        return await ReadCollectionAsync(response, "find general journal dimensions", cancellationToken);
    }

    public async Task DeleteGeneralJournalTestArtifactsAsync(Guid? journalId, IReadOnlyCollection<Guid>? lineIds = null, IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>>? dimensionIdsByLine = null, CancellationToken cancellationToken = default)
    {
        foreach (var lineId in lineIds ?? [])
        {
            foreach (var dimensionId in dimensionIdsByLine?.GetValueOrDefault(lineId) ?? [])
            {
                var dimension = await GetParentScopedEntityIfPresentAsync<BusinessCentralGeneralJournalDimension>(lineId, dimensionId, cancellationToken);
                if (dimension is not null) await DeleteParentScopedBusinessCentralEntityWithExactETagAsync(lineId, dimension, "general journal dimension", cancellationToken);
            }
            var line = await GetPreflightEntityIfPresentAsync<BusinessCentralGeneralJournalLine>(lineId);
            if (line is not null)
            {
                if (!journalId.HasValue || line.JournalId != journalId || line.DocumentNumber?.StartsWith("DH-GL-", StringComparison.OrdinalIgnoreCase) != true)
                    throw new InvalidOperationException($"Refusing to delete non-test general journal line {line.Id}.");
                await DeleteBusinessCentralEntityWithExactETagAsync(line, "general journal line", cancellationToken);
            }
        }
        if (journalId.HasValue)
        {
            var journal = await GetPreflightEntityIfPresentAsync<BusinessCentralGeneralJournal>(journalId.Value);
            if (journal is not null)
            {
                if (journal.Code?.StartsWith("DG", StringComparison.OrdinalIgnoreCase) != true)
                    throw new InvalidOperationException($"Refusing to delete non-test general journal {journal.Id}.");
                await DeleteBusinessCentralEntityWithExactETagAsync(journal, "general journal", cancellationToken);
            }
        }
    }

    public async Task<BusinessCentralGeneralLedgerEntryTestReferences> GetGeneralLedgerEntryTestReferencesAsync(
        CancellationToken cancellationToken = default)
    {
        var general = await GetPreflightEntitiesAsync<BusinessCentralGeneralLedgerEntry>(100, "read existing G/L entries", cancellationToken);
        BusinessCentralGeneralLedgerEntry? generalEntry = null;
        BusinessCentralGeneralLedgerAccount? account = null;
        foreach (var candidate in general.Where(item => item.AccountId.HasValue && item.AccountId != Guid.Empty))
        {
            var candidateAccount = await GetPreflightEntityIfPresentAsync<BusinessCentralGeneralLedgerAccount>(candidate.AccountId!.Value);
            if (candidateAccount is null)
                continue;

            generalEntry = candidate;
            account = candidateAccount;
            break;
        }

        if (generalEntry is null || account is null)
            throw new BusinessCentralIntegrationTestSkippedException(
                "The G/L-entry reference scenario requires an existing G/L entry with a readable account in the isolated company.");

        return new BusinessCentralGeneralLedgerEntryTestReferences(generalEntry, account);
    }

    public async Task<BusinessCentralItemLedgerEntry> GetItemLedgerEntryTestReferenceAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await GetPreflightEntitiesAsync<BusinessCentralItemLedgerEntry>(
            100,
            "read existing item ledger entries",
            cancellationToken);
        var itemEntry = items.FirstOrDefault(candidate => Guid.TryParse(candidate.Id, out _));
        if (itemEntry is null)
            throw new BusinessCentralIntegrationTestSkippedException(
                "The item-ledger-entry reference scenario requires an existing item ledger entry in the isolated company.");

        return itemEntry;
    }

    public async Task<TBusinessCentralEntity> GetReferenceDataFixtureAsync<TBusinessCentralEntity>(
        string description,
        CancellationToken cancellationToken = default)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var supportedTypes = new HashSet<Type>
        {
            typeof(BusinessCentralCurrency),
            typeof(BusinessCentralPaymentTerm),
            typeof(BusinessCentralPaymentMethod),
            typeof(BusinessCentralUnitOfMeasure),
            typeof(BusinessCentralLocation)
        };
        if (!supportedTypes.Contains(typeof(TBusinessCentralEntity)))
            throw new InvalidOperationException(
                $"{typeof(TBusinessCentralEntity).Name} is not an approved read-only reference-data fixture type.");

        var entities = await GetPreflightEntitiesAsync<TBusinessCentralEntity>(
            100, $"read {description} reference data", cancellationToken);
        var entity = entities.FirstOrDefault(candidate => Guid.TryParse(candidate.Id, out _));
        return entity ?? throw new BusinessCentralIntegrationTestSkippedException(
            $"The isolated company has no {description} record for the read-only reference-data scenario.");
    }

    public async Task<BusinessCentralItemVariantTestReferences> GetItemVariantTestReferencesAsync(
        CancellationToken cancellationToken = default)
    {
        var variants = await GetPreflightEntitiesAsync<BusinessCentralItemVariant>(
            100, "read item variants for reference-data tests", cancellationToken);
        foreach (var variant in variants.Where(item => item.ItemId.HasValue && item.ItemId != Guid.Empty))
        {
            var item = await GetPreflightEntityIfPresentAsync<BusinessCentralItem>(variant.ItemId!.Value);
            if (item is not null)
                return new BusinessCentralItemVariantTestReferences(item, variant);
        }

        throw new BusinessCentralIntegrationTestSkippedException(
            "The isolated company has no item variant with a readable parent item for the read-only reference-data scenario.");
    }

    public async Task<BusinessCentralCustomer> GetCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntityAsync<BusinessCentralCustomer>(customerId, cancellationToken);
        if (response.IsT2)
        {
            throw response.AsT2;
        }

        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, $"read customer {customerId}", cancellationToken);
        }

        return response.AsT0 ?? throw new InvalidOperationException($"Business Central customer {customerId} was not found.");
    }

    public async Task<IReadOnlyList<BusinessCentralCustomer>> FindCustomersByDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var escapedName = displayName.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralCustomer>(
            filter: $"displayName eq '{escapedName}'",
            top: 10,
            cancellationToken: cancellationToken);

        if (response.IsT2)
        {
            throw response.AsT2;
        }

        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, $"find customer '{displayName}'", cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralContact>> FindContactsByDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var escapedName = displayName.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralContact>(
            filter: $"displayName eq '{escapedName}'",
            top: 10,
            cancellationToken: cancellationToken);

        if (response.IsT2)
        {
            throw response.AsT2;
        }

        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, $"find contact '{displayName}'", cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralItem>> FindItemsByDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var escapedName = displayName.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralItem>(
            filter: $"displayName eq '{escapedName}'",
            top: 10,
            cancellationToken: cancellationToken);

        if (response.IsT2)
        {
            throw response.AsT2;
        }

        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, $"find item '{displayName}'", cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralVendor>> FindVendorsByDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var escapedName = displayName.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralVendor>(
            filter: $"displayName eq '{escapedName}'",
            top: 10,
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, $"find vendor '{displayName}'", cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralPurchaseOrderLine>> FindPurchaseOrderLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralPurchaseOrderLine>(
            filter: $"documentId eq {documentId}",
            top: 100,
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read purchase order lines for {documentId}",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralPurchaseInvoice>> FindPurchaseInvoicesByVendorInvoiceNumberAsync(
        string vendorInvoiceNumber,
        CancellationToken cancellationToken = default)
    {
        var escapedNumber = vendorInvoiceNumber.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralPurchaseInvoice>(
            filter: $"vendorInvoiceNumber eq '{escapedNumber}'",
            top: 10,
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"find purchase invoice '{vendorInvoiceNumber}'",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralPurchaseInvoiceLine>> FindPurchaseInvoiceLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralPurchaseInvoiceLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read purchase invoice lines for {documentId}",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralPurchaseCreditMemo>>
        FindPurchaseCreditMemosByVendorCreditMemoNumberAsync(
            string vendorCreditMemoNumber,
            CancellationToken cancellationToken = default)
    {
        var escapedNumber = vendorCreditMemoNumber.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralPurchaseCreditMemo>(
            filter: $"vendorCreditMemoNumber eq '{escapedNumber}'",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"find purchase credit memo '{vendorCreditMemoNumber}'",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralPurchaseCreditMemoLine>>
        FindPurchaseCreditMemoLinesAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralPurchaseCreditMemoLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read purchase credit memo lines for {documentId}",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<BusinessCentralSalesShipmentTestReference> GetSalesShipmentTestReferenceAsync(
        CancellationToken cancellationToken = default)
    {
        var headers = await GetPostedTransactionHeadersAsync<BusinessCentralSalesShipment>(
            "sales shipments",
            cancellationToken);
        foreach (var header in headers)
        {
            if (!Guid.TryParse(header.Id, out var headerId)) continue;
            var lines = await FindSalesShipmentLinesAsync(headerId, cancellationToken);
            if (lines.Count != 0)
            {
                return new BusinessCentralSalesShipmentTestReference(header, lines);
            }
        }

        throw new BusinessCentralIntegrationTestSkippedException(
            "The isolated company has no posted sales shipment with lines. Supply a read-only posted fixture before running SalesShipment scenarios.");
    }

    public async Task<IReadOnlyList<BusinessCentralSalesShipmentLine>> FindSalesShipmentLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesShipmentLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, $"read sales shipment lines for {documentId}", cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<BusinessCentralPurchaseReceiptTestReference> GetPurchaseReceiptTestReferenceAsync(
        CancellationToken cancellationToken = default)
    {
        var headers = await GetPostedTransactionHeadersAsync<BusinessCentralPurchaseReceipt>(
            "purchase receipts",
            cancellationToken);
        foreach (var header in headers)
        {
            if (!Guid.TryParse(header.Id, out var headerId)) continue;
            var lines = await FindPurchaseReceiptLinesAsync(headerId, cancellationToken);
            if (lines.Count != 0)
            {
                return new BusinessCentralPurchaseReceiptTestReference(header, lines);
            }
        }

        throw new BusinessCentralIntegrationTestSkippedException(
            "The isolated company has no posted purchase receipt with lines. Supply a read-only posted fixture before running PurchaseReceipt scenarios.");
    }

    public async Task<IReadOnlyList<BusinessCentralPurchaseReceiptLine>> FindPurchaseReceiptLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralPurchaseReceiptLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, $"read purchase receipt lines for {documentId}", cancellationToken);
        }

        return response.AsT0.Value;
    }

    private async Task<IReadOnlyList<TBusinessCentralEntity>> GetPostedTransactionHeadersAsync<TBusinessCentralEntity>(
        string description,
        CancellationToken cancellationToken)
        where TBusinessCentralEntity : BusinessCentralDocument, new()
    {
        var response = await BusinessCentralService.GetEntitiesAsync<TBusinessCentralEntity>(
            top: 25,
            cancellationToken: cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, $"read {description}", cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralSalesOrder>> FindSalesOrdersByExternalDocumentNumberAsync(
        string externalDocumentNumber,
        CancellationToken cancellationToken = default)
    {
        var escapedNumber = externalDocumentNumber.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesOrder>(
            filter: $"externalDocumentNumber eq '{escapedNumber}'",
            top: 10,
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"find sales order '{externalDocumentNumber}'",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralSalesOrderLine>> FindSalesOrderLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesOrderLine>(
            filter: $"documentId eq {documentId}",
            top: 100,
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read sales order lines for {documentId}",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralSalesQuote>> FindSalesQuotesByExternalDocumentNumberAsync(
        string externalDocumentNumber,
        CancellationToken cancellationToken = default)
    {
        var escapedNumber = externalDocumentNumber.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesQuote>(
            filter: $"externalDocumentNumber eq '{escapedNumber}'",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"find sales quote '{externalDocumentNumber}'",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralSalesQuoteLine>> FindSalesQuoteLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesQuoteLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read sales quote lines for {documentId}",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task CleanupSalesQuoteTestArtifactsAsync(
        string? externalDocumentNumber,
        IEnumerable<string?> capturedQuoteIds,
        IEnumerable<string?> capturedLineIds,
        CancellationToken cancellationToken = default)
    {
        var quoteIds = ParseCleanupIds(capturedQuoteIds, "sales quote");
        var lineIds = ParseCleanupIds(capturedLineIds, "sales quote line");
        var exactMatchQuoteIds = new HashSet<Guid>();

        if (!string.IsNullOrWhiteSpace(externalDocumentNumber))
        {
            if (!externalDocumentNumber.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Refusing sales-quote cleanup lookup for non-test external document number " +
                    $"'{externalDocumentNumber}'.");
            }

            var exactMatches = await FindSalesQuotesByExternalDocumentNumberAsync(
                externalDocumentNumber,
                cancellationToken);
            foreach (var quote in exactMatches.Where(quote => string.Equals(
                         quote.ExternalDocumentNumber,
                         externalDocumentNumber,
                         StringComparison.Ordinal)))
            {
                var quoteId = ParseCleanupId(quote.Id, "sales quote");
                quoteIds.Add(quoteId);
                exactMatchQuoteIds.Add(quoteId);
            }
        }

        // A captured line is trusted only when its current parent is itself a captured or
        // exact-match DHIT quote. Resolve the parent before deleting anything so cleanup can
        // never use a child id to reach an unrelated document.
        foreach (var lineId in lineIds.ToList())
        {
            var line = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesQuoteLine>(
                lineId,
                cancellationToken);
            if (line is null) continue;
            if (!line.DocumentId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Cannot safely clean up sales quote line {lineId} because it has no documentId.");
            }

            var parentId = line.DocumentId.Value;
            if (!quoteIds.Contains(parentId))
            {
                var parent = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesQuote>(
                    parentId,
                    cancellationToken);
                EnsureSalesQuoteIsTestArtifact(parent, externalDocumentNumber, exactMatchQuoteIds.Contains(parentId));
                quoteIds.Add(parentId);
            }
        }

        // Validate every captured header against its current representation before using its
        // parent scope to discover children. Captured test quotes may deliberately clear their
        // external document number, but a non-empty value must retain the DHIT prefix.
        foreach (var quoteId in quoteIds)
        {
            var quote = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesQuote>(
                quoteId,
                cancellationToken);
            EnsureSalesQuoteIsTestArtifact(quote, externalDocumentNumber, exactMatchQuoteIds.Contains(quoteId));
            if (quote is null) continue;

            foreach (var line in await FindSalesQuoteLinesAsync(quoteId, cancellationToken))
            {
                if (line.DocumentId != quoteId) continue;
                lineIds.Add(ParseCleanupId(line.Id, "sales quote line"));
            }
        }

        var lineFailures = new List<Exception>();
        foreach (var lineId in lineIds)
        {
            try
            {
                var line = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesQuoteLine>(
                    lineId,
                    cancellationToken);
                if (line is null) continue;
                if (!line.DocumentId.HasValue || !quoteIds.Contains(line.DocumentId.Value))
                {
                    throw new InvalidOperationException(
                        $"Refusing to delete sales quote line {lineId} because its current parent " +
                        $"is not one of the captured test quotes.");
                }

                await DeleteBusinessCentralEntityWithExactETagAsync(
                    line,
                    "sales quote line",
                    cancellationToken);
            }
            catch (Exception exception)
            {
                lineFailures.Add(exception);
            }
        }

        if (lineFailures.Count != 0)
        {
            throw new AggregateException(
                "One or more Business Central sales-quote lines could not be cleaned up; " +
                "their parent headers were retained.",
                lineFailures);
        }

        var quoteFailures = new List<Exception>();
        foreach (var quoteId in quoteIds)
        {
            try
            {
                var quote = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesQuote>(
                    quoteId,
                    cancellationToken);
                EnsureSalesQuoteIsTestArtifact(
                    quote,
                    externalDocumentNumber,
                    exactMatchQuoteIds.Contains(quoteId));
                if (quote is null) continue;

                // Re-read immediately before deleting the header. This catches any child that
                // appeared after the first discovery pass and preserves line-first cleanup.
                foreach (var line in await FindSalesQuoteLinesAsync(quoteId, cancellationToken))
                {
                    if (line.DocumentId != quoteId) continue;
                    var currentLine = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesQuoteLine>(
                        ParseCleanupId(line.Id, "sales quote line"),
                        cancellationToken);
                    if (currentLine is not null)
                    {
                        if (currentLine.DocumentId != quoteId)
                        {
                            throw new InvalidOperationException(
                                $"Refusing to delete sales quote line {currentLine.Id} because its current parent " +
                                $"is not the captured test quote {quoteId}.");
                        }

                        await DeleteBusinessCentralEntityWithExactETagAsync(
                            currentLine,
                            "sales quote line",
                            cancellationToken);
                    }
                }

                quote = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesQuote>(
                    quoteId,
                    cancellationToken);
                EnsureSalesQuoteIsTestArtifact(
                    quote,
                    externalDocumentNumber,
                    exactMatchQuoteIds.Contains(quoteId));
                if (quote is not null)
                {
                    await DeleteBusinessCentralEntityWithExactETagAsync(
                        quote,
                        "sales quote",
                        cancellationToken);
                }
            }
            catch (Exception exception)
            {
                quoteFailures.Add(exception);
            }
        }

        if (quoteFailures.Count != 0)
        {
            throw new AggregateException(
                "One or more Business Central sales quotes could not be cleaned up.",
                quoteFailures);
        }
    }

    public async Task<IReadOnlyList<BusinessCentralSalesCreditMemo>>
        FindSalesCreditMemosByExternalDocumentNumberAsync(
            string externalDocumentNumber,
            CancellationToken cancellationToken = default)
    {
        var escapedNumber = externalDocumentNumber.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesCreditMemo>(
            filter: $"externalDocumentNumber eq '{escapedNumber}'",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"find sales credit memo '{externalDocumentNumber}'",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralSalesCreditMemoLine>> FindSalesCreditMemoLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesCreditMemoLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read sales credit memo lines for {documentId}",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public Task CleanupSalesCreditMemoTestArtifactsAsync(
        string? externalDocumentNumber,
        IEnumerable<string?> capturedCreditMemoIds,
        IEnumerable<string?> capturedLineIds,
        CancellationToken cancellationToken = default) =>
        CleanupSalesCreditMemoTestArtifactsCoreAsync(
            externalDocumentNumber,
            capturedCreditMemoIds,
            capturedLineIds,
            exactCapturedCreditMemoId: null,
            cancellationToken);

    private async Task CleanupSalesCreditMemoTestArtifactsCoreAsync(
        string? externalDocumentNumber,
        IEnumerable<string?> capturedCreditMemoIds,
        IEnumerable<string?> capturedLineIds,
        Guid? exactCapturedCreditMemoId,
        CancellationToken cancellationToken)
    {
        var capturedCreditMemoIdSet = ParseCleanupIds(
            capturedCreditMemoIds,
            "sales credit memo");
        var creditMemoIds = new HashSet<Guid>(capturedCreditMemoIdSet);
        var lineIds = ParseCleanupIds(capturedLineIds, "sales credit memo line");
        var exactMatchCreditMemoIds = new HashSet<Guid>();

        if (exactCapturedCreditMemoId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(externalDocumentNumber) ||
                !externalDocumentNumber.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Exact captured sales-credit-memo cleanup requires a nonblank DHIT external document number.");
            }

            capturedCreditMemoIdSet.Add(exactCapturedCreditMemoId.Value);
            creditMemoIds.Add(exactCapturedCreditMemoId.Value);
            exactMatchCreditMemoIds.Add(exactCapturedCreditMemoId.Value);
        }

        if (!exactCapturedCreditMemoId.HasValue &&
            !string.IsNullOrWhiteSpace(externalDocumentNumber))
        {
            if (!externalDocumentNumber.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Refusing sales-credit-memo cleanup lookup for non-test external document number " +
                    $"'{externalDocumentNumber}'.");
            }

            var exactMatches = await FindSalesCreditMemosByExternalDocumentNumberAsync(
                externalDocumentNumber,
                cancellationToken);
            foreach (var creditMemo in exactMatches.Where(creditMemo => string.Equals(
                         creditMemo.ExternalDocumentNumber,
                         externalDocumentNumber,
                         StringComparison.Ordinal)))
            {
                var creditMemoId = ParseCleanupId(creditMemo.Id, "sales credit memo");
                creditMemoIds.Add(creditMemoId);
                exactMatchCreditMemoIds.Add(creditMemoId);
            }
        }

        // A captured child is trusted only while its current parent is a captured or exact-match
        // DHIT draft. Resolve and validate every parent before deleting any record.
        foreach (var lineId in lineIds.ToList())
        {
            var line = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemoLine>(
                lineId,
                cancellationToken);
            if (line is null) continue;
            if (!line.DocumentId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Cannot safely clean up sales credit memo line {lineId} because it has no documentId.");
            }

            var parentId = line.DocumentId.Value;
            if (!creditMemoIds.Contains(parentId))
            {
                var parent = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemo>(
                    parentId,
                    cancellationToken);
                if (parent is null)
                {
                    throw new InvalidOperationException(
                        $"Cannot safely clean up sales credit memo line {lineId} because its current parent " +
                        $"{parentId} could not be read.");
                }
                EnsureSalesCreditMemoIsTestArtifact(
                    parent,
                    externalDocumentNumber,
                    exactMatchCreditMemoIds.Contains(parentId));
                creditMemoIds.Add(parentId);
            }
        }

        // The service follows @odata.nextLink, and no top limit is supplied, so every current
        // child of every validated test header is captured for line-first cleanup.
        foreach (var creditMemoId in creditMemoIds)
        {
            var creditMemo = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemo>(
                creditMemoId,
                cancellationToken);
            EnsureSalesCreditMemoIsTestArtifact(
                creditMemo,
                externalDocumentNumber,
                exactMatchCreditMemoIds.Contains(creditMemoId));
            if (creditMemo is null) continue;

            foreach (var line in await FindSalesCreditMemoLinesAsync(creditMemoId, cancellationToken))
            {
                if (line.DocumentId != creditMemoId)
                {
                    throw new InvalidOperationException(
                        $"Refusing sales-credit-memo cleanup because line {line.Id ?? "<missing>"} was returned " +
                        $"under header {creditMemoId} but currently identifies parent " +
                        $"{line.DocumentId?.ToString() ?? "<missing>"}.");
                }

                lineIds.Add(ParseCleanupId(line.Id, "sales credit memo line"));
            }
        }

        var lineFailures = new List<Exception>();
        foreach (var lineId in lineIds)
        {
            try
            {
                var line = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemoLine>(
                    lineId,
                    cancellationToken);
                if (line is null) continue;
                if (!line.DocumentId.HasValue || !creditMemoIds.Contains(line.DocumentId.Value))
                {
                    throw new InvalidOperationException(
                        $"Refusing to delete sales credit memo line {lineId} because its current parent " +
                        $"is not one of the captured test credit memos.");
                }

                var currentParent =
                    await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemo>(
                        line.DocumentId.Value,
                        cancellationToken);
                if (currentParent is null)
                {
                    throw new InvalidOperationException(
                        $"Refusing to delete sales credit memo line {lineId} because its current parent " +
                        $"{line.DocumentId.Value} could not be read.");
                }
                EnsureSalesCreditMemoIsTestArtifact(
                    currentParent,
                    externalDocumentNumber,
                    exactMatchCreditMemoIds.Contains(line.DocumentId.Value));

                await DeleteBusinessCentralEntityWithExactETagAsync(
                    line,
                    "sales credit memo line",
                    cancellationToken);
            }
            catch (Exception exception)
            {
                lineFailures.Add(exception);
            }
        }

        if (lineFailures.Count != 0)
        {
            throw new AggregateException(
                "One or more Business Central sales-credit-memo lines could not be cleaned up; " +
                "their parent headers were retained.",
                lineFailures);
        }

        var creditMemoFailures = new List<Exception>();
        foreach (var creditMemoId in creditMemoIds)
        {
            try
            {
                var creditMemo = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemo>(
                    creditMemoId,
                    cancellationToken);
                EnsureSalesCreditMemoIsTestArtifact(
                    creditMemo,
                    externalDocumentNumber,
                    exactMatchCreditMemoIds.Contains(creditMemoId));
                if (creditMemo is null) continue;

                var expectedExternalDocumentNumber = creditMemo.ExternalDocumentNumber;
                var allowCapturedBlankDraft =
                    capturedCreditMemoIdSet.Contains(creditMemoId) &&
                    string.IsNullOrEmpty(expectedExternalDocumentNumber);

                // Re-read all children immediately before the header delete. A child created after
                // the discovery pass is still deleted first, using its freshly read exact ETag.
                foreach (var discoveredLine in await FindSalesCreditMemoLinesAsync(
                             creditMemoId,
                             cancellationToken))
                {
                    if (discoveredLine.DocumentId != creditMemoId)
                    {
                        throw new InvalidOperationException(
                            $"Refusing to delete sales credit memo {creditMemoId} because discovered line " +
                            $"{discoveredLine.Id ?? "<missing>"} identifies a different current parent.");
                    }

                    var currentLine =
                        await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemoLine>(
                            ParseCleanupId(discoveredLine.Id, "sales credit memo line"),
                            cancellationToken);
                    if (currentLine is null) continue;
                    if (currentLine.DocumentId != creditMemoId)
                    {
                        throw new InvalidOperationException(
                            $"Refusing to delete sales credit memo line {currentLine.Id} because its current " +
                            $"parent is not the captured test credit memo {creditMemoId}.");
                    }

                    var currentParent =
                        await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemo>(
                            creditMemoId,
                            cancellationToken);
                    if (currentParent is null)
                    {
                        throw new InvalidOperationException(
                            $"Refusing to delete sales credit memo line {currentLine.Id} because its current " +
                            $"parent {creditMemoId} could not be read.");
                    }
                    EnsureSalesCreditMemoIsTestArtifact(
                        currentParent,
                        externalDocumentNumber,
                        exactMatchCreditMemoIds.Contains(creditMemoId));

                    await DeleteBusinessCentralEntityWithExactETagAsync(
                        currentLine,
                        "sales credit memo line",
                        cancellationToken);
                }

                creditMemo = await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemo>(
                    creditMemoId,
                    cancellationToken);
                EnsureSalesCreditMemoIsTestArtifact(
                    creditMemo,
                    externalDocumentNumber,
                    exactMatchCreditMemoIds.Contains(creditMemoId));
                if (creditMemo is null) continue;

                var initialCleanup = await DeleteCurrentSalesCreditMemoAsync(
                    creditMemoId,
                    allowDraftDelete: true,
                    expectedExternalDocumentNumber,
                    allowCapturedBlankDraft,
                    transitionProvenance: null,
                    cancellationToken);

                if (initialCleanup.Disposition ==
                    BusinessCentralSalesCreditMemoCleanupDisposition.DraftDeleted)
                {
                    var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
                    BusinessCentralSalesCreditMemo? afterDraftDelete;
                    do
                    {
                        afterDraftDelete =
                            await GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemo>(
                                creditMemoId,
                                cancellationToken);
                        if (afterDraftDelete is null) break;

                        if (!string.Equals(
                                afterDraftDelete.Status,
                                "Draft",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            var placeholderCleanup = await DeleteCurrentSalesCreditMemoAsync(
                                creditMemoId,
                                allowDraftDelete: false,
                                expectedExternalDocumentNumber,
                                allowCapturedBlankDraft: false,
                                initialCleanup.TransitionProvenance,
                                cancellationToken);
                            if (placeholderCleanup.Disposition !=
                                BusinessCentralSalesCreditMemoCleanupDisposition.NoSeriesPlaceholderDeleted)
                            {
                                throw new InvalidOperationException(
                                    $"Sales credit memo {creditMemoId} changed unexpectedly during guarded " +
                                    "cleanup.");
                            }

                            foreach (var placeholderLine in placeholderCleanup.CapturedLines)
                            {
                                lineIds.Add(ParseCleanupId(
                                    placeholderLine.Id,
                                    "sales credit memo placeholder line"));
                            }
                            break;
                        }

                        if (DateTimeOffset.UtcNow < timeoutAt)
                        {
                            await Task.Delay(250, cancellationToken);
                        }
                    }
                    while (DateTimeOffset.UtcNow < timeoutAt);

                    if (afterDraftDelete is not null &&
                        string.Equals(
                            afterDraftDelete.Status,
                            "Draft",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Sales credit memo {creditMemoId} remained Draft after Business Central " +
                            "acknowledged deletion. Cleanup did not retry the draft DELETE.");
                    }
                }

                if (initialCleanup.Disposition !=
                    BusinessCentralSalesCreditMemoCleanupDisposition.NotFound)
                {
                    await VerifyPreflightEntityDeletedAsync<BusinessCentralSalesCreditMemo>(
                        creditMemoId.ToString());
                }
            }
            catch (Exception exception)
            {
                creditMemoFailures.Add(exception);
            }
        }

        if (creditMemoFailures.Count != 0)
        {
            throw new AggregateException(
                "One or more Business Central sales credit memos could not be cleaned up.",
                creditMemoFailures);
        }

        foreach (var lineId in lineIds)
        {
            await VerifyPreflightEntityDeletedAsync<BusinessCentralSalesCreditMemoLine>(
                lineId.ToString());
        }
    }

    public async Task DeleteSalesCreditMemoAggregateAsync(
        Guid creditMemoId,
        string expectedExternalDocumentNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedExternalDocumentNumber) ||
            !expectedExternalDocumentNumber.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Deliberate sales-credit-memo aggregate deletion requires the current exact nonblank DHIT " +
                "external document number.");
        }

        await CleanupSalesCreditMemoTestArtifactsCoreAsync(
            externalDocumentNumber: expectedExternalDocumentNumber,
            capturedCreditMemoIds: [creditMemoId.ToString()],
            capturedLineIds: [],
            exactCapturedCreditMemoId: creditMemoId,
            cancellationToken);
    }

    public async Task DeleteSalesCreditMemoNoSeriesPlaceholderAsync(
        Guid creditMemoId,
        string expectedExternalDocumentNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedExternalDocumentNumber) ||
            !expectedExternalDocumentNumber.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Explicit sales-credit-memo placeholder cleanup requires the exact DHIT external document " +
                "number read during the preceding diagnostic.");
        }

        var result = await DeleteCurrentSalesCreditMemoAsync(
            creditMemoId,
            allowDraftDelete: false,
            expectedExternalDocumentNumber,
            allowCapturedBlankDraft: false,
            transitionProvenance: null,
            cancellationToken);
        if (result.Disposition == BusinessCentralSalesCreditMemoCleanupDisposition.NotFound)
        {
            return;
        }

        if (result.Disposition !=
            BusinessCentralSalesCreditMemoCleanupDisposition.NoSeriesPlaceholderDeleted)
        {
            throw new InvalidOperationException(
                $"Sales credit memo {creditMemoId} was not the exact Paid no-series placeholder.");
        }

        await VerifyPreflightEntityDeletedAsync<BusinessCentralSalesCreditMemo>(
            creditMemoId.ToString());
        foreach (var line in result.CapturedLines)
        {
            await VerifyPreflightEntityDeletedAsync<BusinessCentralSalesCreditMemoLine>(
                ParseCleanupId(line.Id, "sales credit memo placeholder line").ToString());
        }
    }

    private Task<BusinessCentralSalesCreditMemoCleanupResult> DeleteCurrentSalesCreditMemoAsync(
        Guid creditMemoId,
        bool allowDraftDelete,
        string? expectedExternalDocumentNumber,
        bool allowCapturedBlankDraft,
        BusinessCentralSalesCreditMemoCleanupProvenance? transitionProvenance,
        CancellationToken cancellationToken) =>
        BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            creditMemoId,
            GetBusinessCentralEntityIfPresentAsync<BusinessCentralSalesCreditMemo>,
            ReadCompleteSalesCreditMemoLinesAsync,
            DeleteSalesCreditMemoWithExactETagAsync,
            allowDraftDelete,
            expectedExternalDocumentNumber,
            allowCapturedBlankDraft,
            transitionProvenance,
            cancellationToken);

    private async Task<BusinessCentralSalesCreditMemoLineSnapshot>
        ReadCompleteSalesCreditMemoLinesAsync(
            Guid documentId,
            CancellationToken cancellationToken)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesCreditMemoLine>(
            filter: $"documentId eq {documentId}",
            order: "sequence",
            cancellationToken: cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read the complete sales-credit-memo line set for {documentId}",
                cancellationToken);
        }

        return new BusinessCentralSalesCreditMemoLineSnapshot(
            response.AsT0.Count,
            response.AsT0.Value);
    }

    private async Task DeleteSalesCreditMemoWithExactETagAsync(
        BusinessCentralSalesCreditMemo creditMemo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(creditMemo.ETag) ||
            string.Equals(creditMemo.ETag.Trim(), "*", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Sales credit memo {creditMemo.Id} has no exact ETag. Cleanup will not use a wildcard " +
                "If-Match value.");
        }

        var response = await BusinessCentralService.DeleteEntityAsync(creditMemo, cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"delete sales credit memo {creditMemo.Id} with its exact ETag",
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<BusinessCentralSalesInvoice>> FindSalesInvoicesByExternalDocumentNumberAsync(
        string externalDocumentNumber,
        CancellationToken cancellationToken = default)
    {
        var escapedNumber = externalDocumentNumber.Replace("'", "''", StringComparison.Ordinal);
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesInvoice>(
            filter: $"externalDocumentNumber eq '{escapedNumber}'",
            top: 10,
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"find sales invoice '{externalDocumentNumber}'",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralSalesInvoiceLine>> FindSalesInvoiceLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await BusinessCentralService.GetEntitiesAsync<BusinessCentralSalesInvoiceLine>(
            filter: $"documentId eq {documentId}",
            top: 100,
            order: "sequence",
            cancellationToken: cancellationToken);

        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"read sales invoice lines for {documentId}",
                cancellationToken);
        }

        return response.AsT0.Value;
    }

    public async Task<IReadOnlyList<BusinessCentralContactInformation>> GetCustomerContactInformationAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var client = ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("BusinessCentral");
        using var response = await client.GetAsync(
            $"api/v2.0/companies({Settings.CompanyId})/customers({customerId})/contactsInformation",
            cancellationToken);
        await EnsureSuccessAsync(response, $"read contact information for customer {customerId}", cancellationToken);

        var body = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return body["value"]?.ToObject<List<BusinessCentralContactInformation>>() ?? [];
    }

    public async Task<DataHubAccount?> GetTrackedCustomerAccountAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);

        do
        {
            var response = await DataHubClient.PostRequestAsync<GetTrackedEntitiesRequest, GetTrackedEntitiesResponse>(
                new GetTrackedEntitiesRequest
                {
                    DataSource = "BusinessCentral",
                    EntityType = typeof(BusinessCentralCustomer).Name,
                    EntityIds = [customerId.ToString()]
                },
                cancellationToken);

            var tracked = response.Results.SingleOrDefault()?.Data?.ToObject<DataHubAccount>();
            if (tracked is not null)
            {
                return tracked;
            }

            await Task.Delay(100, cancellationToken);
        }
        while (DateTimeOffset.UtcNow < timeoutAt);

        return null;
    }

    public async Task CleanupAsync(string dataHubAccountId, Guid? customerId, string displayName)
    {
        try
        {
            var customerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (customerId.HasValue)
            {
                customerIds.Add(customerId.Value.ToString());
            }

            var escapedName = displayName.Replace("'", "''", StringComparison.Ordinal);
            var matching = await BusinessCentralService.GetEntitiesAsync<BusinessCentralCustomer>(
                filter: $"displayName eq '{escapedName}'",
                top: 10,
                cancellationToken: CancellationToken.None);

            if (matching.IsT0)
            {
                foreach (var customer in matching.AsT0.Value.Where(customer => customer.Id is not null))
                {
                    customerIds.Add(customer.Id!);
                }
            }

            foreach (var id in customerIds)
            {
                _ = await BusinessCentralService.DeleteEntityAsync<BusinessCentralCustomer>(id, CancellationToken.None);
            }
        }
        catch
        {
            // Cleanup is best-effort and remains restricted to captured IDs or the unique exact test name.
        }

        try
        {
            await DataHubClient.PostRequestAsync<DeleteDataHubEntitiesRequest, DeleteDataHubEntitiesResponse>(
                new DeleteDataHubEntitiesRequest
                {
                    EntityType = typeof(DataHubAccount).Name,
                    EntityIds = [dataHubAccountId],
                    IncludeTrackingEntries = true
                },
                CancellationToken.None);
        }
        catch
        {
            // Cleanup is best-effort and remains restricted to the test-created account ID.
        }
    }

    public async Task CleanupContactAsync(
        string dataHubContactId,
        Guid? contactId,
        string displayName)
    {
        try
        {
            var contactIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (contactId.HasValue)
            {
                contactIds.Add(contactId.Value.ToString());
            }

            foreach (var contact in await FindContactsByDisplayNameAsync(displayName, CancellationToken.None))
            {
                if (!string.IsNullOrWhiteSpace(contact.Id))
                {
                    contactIds.Add(contact.Id);
                }
            }

            foreach (var id in contactIds)
            {
                _ = await BusinessCentralService.DeleteEntityAsync<BusinessCentralContact>(id, CancellationToken.None);
            }
        }
        catch
        {
            // Cleanup is best-effort and remains restricted to captured IDs or the unique exact test name.
        }

        try
        {
            await DataHubClient.PostRequestAsync<DeleteDataHubEntitiesRequest, DeleteDataHubEntitiesResponse>(
                new DeleteDataHubEntitiesRequest
                {
                    EntityType = typeof(DataHubContact).Name,
                    EntityIds = [dataHubContactId],
                    IncludeTrackingEntries = true
                },
                CancellationToken.None);
        }
        catch
        {
            // Cleanup is best-effort and remains restricted to the test-created contact ID.
        }
    }

    public async Task CleanupProductAsync(
        string dataHubProductId,
        Guid? itemId,
        string displayName)
    {
        try
        {
            var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (itemId.HasValue)
            {
                itemIds.Add(itemId.Value.ToString());
            }

            foreach (var item in await FindItemsByDisplayNameAsync(displayName, CancellationToken.None))
            {
                if (!string.IsNullOrWhiteSpace(item.Id))
                {
                    itemIds.Add(item.Id);
                }
            }

            foreach (var id in itemIds)
            {
                _ = await BusinessCentralService.DeleteEntityAsync<BusinessCentralItem>(id, CancellationToken.None);
            }
        }
        catch
        {
            // Cleanup is best-effort and remains restricted to captured IDs or the unique exact test name.
        }

        try
        {
            await DataHubClient.PostRequestAsync<DeleteDataHubEntitiesRequest, DeleteDataHubEntitiesResponse>(
                new DeleteDataHubEntitiesRequest
                {
                    EntityType = typeof(DataHubProduct).Name,
                    EntityIds = [dataHubProductId],
                    IncludeTrackingEntries = true
                },
                CancellationToken.None);
        }
        catch
        {
            // Cleanup is best-effort and remains restricted to the test-created product ID.
        }
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private async Task<TBusinessCentralEntity> CreatePreflightEntityAsync<TBusinessCentralEntity>(
        TBusinessCentralEntity entity,
        string operation,
        CancellationToken cancellationToken)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var response = await BusinessCentralService.CreateEntityAsync(entity, cancellationToken);
        if (response.IsT1)
        {
            throw new InvalidOperationException($"Business Central could not {operation}.", response.AsT1);
        }

        if (!response.AsT0.Success || response.AsT0.ResultingEntity is null)
        {
            throw new InvalidOperationException(
                $"Business Central could not {operation}.",
                response.AsT0.Exception);
        }

        return response.AsT0.ResultingEntity;
    }

    private async Task<IReadOnlyList<TBusinessCentralEntity>> GetPreflightEntitiesAsync<TBusinessCentralEntity>(
        int top,
        string operation,
        CancellationToken cancellationToken)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var response = await BusinessCentralService.GetEntitiesAsync<TBusinessCentralEntity>(
            top: top,
            cancellationToken: cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, operation, cancellationToken);
        }

        return response.AsT0.Value;
    }

    private static async Task<IReadOnlyList<TBusinessCentralEntity>> ReadCollectionAsync<TBusinessCentralEntity>(
        OneOf<ApiCollectionResponse<TBusinessCentralEntity>, HttpResponseMessage, Exception> response,
        string operation,
        CancellationToken cancellationToken)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(httpResponse, operation, cancellationToken);
        }

        return response.AsT0.Value;
    }

    private static bool IsAuthorizationFailure(Exception exception)
    {
        var text = exception.ToString();
        return text.Contains("current permissions prevented", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("403", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase);
    }

    private async Task DeletePreflightEntityAsync<TBusinessCentralEntity>(
        string? entityId,
        bool verifyDeleted = false)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        if (string.IsNullOrWhiteSpace(entityId)) return;

        if (!Guid.TryParse(entityId, out var id))
        {
            throw new InvalidOperationException(
                $"Cannot delete preflight {typeof(TBusinessCentralEntity).Name}/{entityId} because its id is not a GUID.");
        }

        var currentResponse = await BusinessCentralService.GetEntityAsync<TBusinessCentralEntity>(
            id,
            CancellationToken.None);
        if (currentResponse.IsT2) throw currentResponse.AsT2;
        if (currentResponse.IsT1)
        {
            using var httpResponse = currentResponse.AsT1;
            if (httpResponse.StatusCode == HttpStatusCode.NotFound) return;

            await EnsureSuccessAsync(
                httpResponse,
                $"read preflight {typeof(TBusinessCentralEntity).Name}/{entityId} before deletion",
                CancellationToken.None);
        }

        var response = await BusinessCentralService.DeleteEntityAsync(
            currentResponse.AsT0,
            CancellationToken.None);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            await EnsureSuccessAsync(
                httpResponse,
                $"delete preflight {typeof(TBusinessCentralEntity).Name}/{entityId}",
                CancellationToken.None);
        }

        if (verifyDeleted)
        {
            await VerifyPreflightEntityDeletedAsync<TBusinessCentralEntity>(entityId);
        }
    }

    private async Task DeletePreflightSalesInvoiceAsync(string? lineId, string? invoiceId)
    {
        await DeletePreflightEntityAsync<BusinessCentralSalesInvoice>(
            invoiceId,
            verifyDeleted: false);

        if (!string.IsNullOrWhiteSpace(invoiceId) && Guid.TryParse(invoiceId, out var invoiceGuid))
        {
            var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
            BusinessCentralSalesInvoice? retainedInvoice;
            do
            {
                retainedInvoice = await GetPreflightEntityIfPresentAsync<BusinessCentralSalesInvoice>(
                    invoiceGuid);
                if (retainedInvoice is null) break;

                var retainedLines = await FindSalesInvoiceLinesAsync(invoiceGuid);
                if (BusinessCentralSalesInvoiceCleanupPolicy.IsSafeEmptyPostedTestArtifact(
                        retainedInvoice,
                        retainedLines))
                {
                    await DeletePreflightEntityAsync<BusinessCentralSalesInvoice>(
                        invoiceId,
                        verifyDeleted: true);
                    retainedInvoice = null;
                    break;
                }

                if (DateTimeOffset.UtcNow < timeoutAt)
                {
                    await Task.Delay(250, CancellationToken.None);
                }
            }
            while (DateTimeOffset.UtcNow < timeoutAt);

            if (retainedInvoice is not null)
            {
                await VerifyPreflightEntityDeletedAsync<BusinessCentralSalesInvoice>(invoiceId);
            }
        }

        if (!string.IsNullOrWhiteSpace(lineId))
        {
            await VerifyPreflightEntityDeletedAsync<BusinessCentralSalesInvoiceLine>(lineId);
        }
    }

    private async Task DeletePreflightSalesQuoteAsync(string? lineId, string? quoteId)
    {
        Exception? cleanupFailure = null;

        try
        {
            await DeletePreflightEntityAsync<BusinessCentralSalesQuoteLine>(
                lineId,
                verifyDeleted: true);
        }
        catch (Exception exception)
        {
            cleanupFailure = exception;
        }

        try
        {
            await DeletePreflightEntityAsync<BusinessCentralSalesQuote>(
                quoteId,
                verifyDeleted: true);
        }
        catch (Exception exception)
        {
            cleanupFailure = cleanupFailure is null
                ? exception
                : new AggregateException(
                    "Business Central sales-quote line and header cleanup both failed.",
                    cleanupFailure,
                    exception);
        }

        if (cleanupFailure is not null)
        {
            ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
        }
    }

    private Task DeletePreflightSalesCreditMemoAsync(
        string externalDocumentNumber,
        string? lineId,
        string? creditMemoId) =>
        CleanupSalesCreditMemoTestArtifactsAsync(
            externalDocumentNumber,
            [creditMemoId],
            [lineId],
            CancellationToken.None);

    private static HashSet<Guid> ParseCleanupIds(
        IEnumerable<string?> entityIds,
        string entityName) =>
        entityIds
            .Where(entityId => !string.IsNullOrWhiteSpace(entityId))
            .Select(entityId => ParseCleanupId(entityId, entityName))
            .ToHashSet();

    private static Guid ParseCleanupId(string? entityId, string entityName)
    {
        if (Guid.TryParse(entityId, out var id)) return id;

        throw new InvalidOperationException(
            $"Cannot safely clean up Business Central {entityName} '{entityId ?? "<missing>"}' " +
            "because its id is not a GUID.");
    }

    private static void EnsureSalesQuoteIsTestArtifact(
        BusinessCentralSalesQuote? quote,
        string? exactExternalDocumentNumber,
        bool discoveredByExactMatch)
    {
        if (quote is null) return;

        if (!string.Equals(quote.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to delete sales quote {quote.Id} because its current status " +
                $"'{quote.Status ?? "<missing>"}' is not Draft.");
        }

        if (discoveredByExactMatch)
        {
            if (string.Equals(
                    quote.ExternalDocumentNumber,
                    exactExternalDocumentNumber,
                    StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Refusing to delete sales quote {quote.Id} because its external document number changed " +
                $"after the exact DHIT cleanup lookup.");
        }

        if (string.IsNullOrEmpty(quote.ExternalDocumentNumber) ||
            quote.ExternalDocumentNumber.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Refusing to delete captured sales quote {quote.Id} because its current external document number " +
            $"'{quote.ExternalDocumentNumber}' is not a DHIT test value.");
    }

    private static void EnsureSalesCreditMemoIsTestArtifact(
        BusinessCentralSalesCreditMemo? creditMemo,
        string? exactExternalDocumentNumber,
        bool discoveredByExactMatch)
    {
        if (creditMemo is null) return;

        if (!string.Equals(creditMemo.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to delete sales credit memo {creditMemo.Id} because its current status " +
                $"'{creditMemo.Status ?? "<missing>"}' is not Draft. The harness never invokes post, send, " +
                "cancel, or corrective lifecycle actions.");
        }

        if (discoveredByExactMatch)
        {
            if (string.Equals(
                    creditMemo.ExternalDocumentNumber,
                    exactExternalDocumentNumber,
                    StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Refusing to delete sales credit memo {creditMemo.Id} because its external document number " +
                "changed after the exact DHIT cleanup lookup.");
        }

        if (string.IsNullOrEmpty(creditMemo.ExternalDocumentNumber) ||
            creditMemo.ExternalDocumentNumber.StartsWith("DHIT-", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Refusing to delete captured sales credit memo {creditMemo.Id} because its current external " +
            $"document number '{creditMemo.ExternalDocumentNumber}' is not a DHIT test value.");
    }

    private async Task<TBusinessCentralEntity?> GetBusinessCentralEntityIfPresentAsync<TBusinessCentralEntity>(
        Guid id,
        CancellationToken cancellationToken)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var response = await BusinessCentralService.GetEntityAsync<TBusinessCentralEntity>(
            id,
            cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            if (httpResponse.StatusCode == HttpStatusCode.NotFound) return null;

            await EnsureSuccessAsync(
                httpResponse,
                $"read {typeof(TBusinessCentralEntity).Name}/{id} before exact-ETag test cleanup",
                cancellationToken);
        }

        return response.AsT0;
    }

    private async Task DeleteBusinessCentralEntityWithExactETagAsync<TBusinessCentralEntity>(
        TBusinessCentralEntity entity,
        string entityName,
        CancellationToken cancellationToken)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new InvalidOperationException(
                $"Cannot clean up Business Central {entityName} because it has no id.");
        }
        if (string.IsNullOrWhiteSpace(entity.ETag) ||
            string.Equals(entity.ETag.Trim(), "*", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Business Central {entityName} {entity.Id} has no exact ETag. " +
                "Cleanup will not use a wildcard If-Match value.");
        }

        var response = await BusinessCentralService.DeleteEntityAsync(entity, cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            if (httpResponse.StatusCode != HttpStatusCode.NotFound)
            {
                await EnsureSuccessAsync(
                    httpResponse,
                    $"delete Business Central {entityName} {entity.Id} with its exact ETag",
                    cancellationToken);
            }
        }

        await VerifyPreflightEntityDeletedAsync<TBusinessCentralEntity>(entity.Id);
    }

    private async Task DeleteParentScopedBusinessCentralEntityWithExactETagAsync<TBusinessCentralEntity>(
        Guid parentId,
        TBusinessCentralEntity entity,
        string entityName,
        CancellationToken cancellationToken)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        if (string.IsNullOrWhiteSpace(entity.Id) || !Guid.TryParse(entity.Id, out var entityId))
        {
            throw new InvalidOperationException(
                $"Cannot clean up Business Central {entityName} because it has no valid id.");
        }
        if (string.IsNullOrWhiteSpace(entity.ETag) ||
            string.Equals(entity.ETag.Trim(), "*", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Business Central {entityName} {entity.Id} has no exact ETag. " +
                "Cleanup will not use a wildcard If-Match value.");
        }

        var response = await BusinessCentralService.DeleteEntityAsync(entity, cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            if (httpResponse.StatusCode != HttpStatusCode.NotFound)
            {
                await EnsureSuccessAsync(
                    httpResponse,
                    $"delete Business Central {entityName} {entity.Id} with its exact ETag",
                    cancellationToken);
            }
        }

        await VerifyParentScopedEntityDeletedAsync<TBusinessCentralEntity>(parentId, entityId);
    }

    private async Task<TBusinessCentralEntity?> GetParentScopedEntityIfPresentAsync<TBusinessCentralEntity>(
        Guid parentId,
        Guid id,
        CancellationToken cancellationToken)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var response = await BusinessCentralService.GetEntityAsync<TBusinessCentralEntity>(
            parentId, id, cancellationToken);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            if (httpResponse.StatusCode == HttpStatusCode.NotFound) return null;

            await EnsureSuccessAsync(
                httpResponse,
                $"read parent-scoped {typeof(TBusinessCentralEntity).Name}/{parentId}/{id}",
                cancellationToken);
        }

        return response.AsT0;
    }

    private async Task VerifyParentScopedEntityDeletedAsync<TBusinessCentralEntity>(Guid parentId, Guid entityId)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
        do
        {
            var retained = await GetParentScopedEntityIfPresentAsync<TBusinessCentralEntity>(
                parentId, entityId, CancellationToken.None);
            if (retained is null) return;
            if (DateTimeOffset.UtcNow < timeoutAt)
            {
                await Task.Delay(250, CancellationToken.None);
            }
        }
        while (DateTimeOffset.UtcNow < timeoutAt);

        throw new InvalidOperationException(
            $"Business Central acknowledged deletion of parent-scoped {typeof(TBusinessCentralEntity).Name}/" +
            $"{parentId}/{entityId}, but the exact record is still readable.");
    }

    private async Task<TBusinessCentralEntity?> GetPreflightEntityIfPresentAsync<TBusinessCentralEntity>(
        Guid id)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        var response = await BusinessCentralService.GetEntityAsync<TBusinessCentralEntity>(
            id,
            CancellationToken.None);
        if (response.IsT2) throw response.AsT2;
        if (response.IsT1)
        {
            using var httpResponse = response.AsT1;
            if (httpResponse.StatusCode == HttpStatusCode.NotFound) return null;

            await EnsureSuccessAsync(
                httpResponse,
                $"read preflight {typeof(TBusinessCentralEntity).Name}/{id}",
                CancellationToken.None);
        }

        return response.AsT0;
    }

    private async Task VerifyPreflightEntityDeletedAsync<TBusinessCentralEntity>(string entityId)
        where TBusinessCentralEntity : BusinessCentralDocument
    {
        if (!Guid.TryParse(entityId, out var id))
        {
            throw new InvalidOperationException(
                $"Cannot verify deletion of {typeof(TBusinessCentralEntity).Name}/{entityId} because its id is not a GUID.");
        }

        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
        TBusinessCentralEntity? retainedRecord = null;
        do
        {
            var response = await BusinessCentralService.GetEntityAsync<TBusinessCentralEntity>(
                id,
                CancellationToken.None);
            if (response.IsT2) throw response.AsT2;
            if (response.IsT1)
            {
                using var httpResponse = response.AsT1;
                if (httpResponse.StatusCode == HttpStatusCode.NotFound) return;

                await EnsureSuccessAsync(
                    httpResponse,
                    $"verify deletion of preflight {typeof(TBusinessCentralEntity).Name}/{entityId}",
                    CancellationToken.None);
            }

            retainedRecord = response.AsT0;

            if (DateTimeOffset.UtcNow < timeoutAt)
            {
                await Task.Delay(250, CancellationToken.None);
            }
        }
        while (DateTimeOffset.UtcNow < timeoutAt);

        var status = retainedRecord?.GetAttributes()
            .FirstOrDefault(attribute =>
                string.Equals(attribute.Key, "status", StringComparison.OrdinalIgnoreCase))
            .Value?.ToString();
        var statusDetail = string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : $" Its current status is '{status}'.";

        throw new InvalidOperationException(
            $"Business Central acknowledged deletion of preflight {typeof(TBusinessCentralEntity).Name}/{entityId}, " +
            "but the record remained readable." + statusDetail +
            " A standard API aggregate transition, tenant workflow, job queue, or extension may have " +
            "changed the document. Stop the affected invoice tests and inspect the isolated company before rerunning.");
    }

    private static void EnsureDraftSalesInvoice(
        BusinessCentralSalesInvoice invoice,
        string operation)
    {
        if (string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase)) return;

        throw new InvalidOperationException(
            $"The preflight sales invoice was {operation} with status '{invoice.Status ?? "<missing>"}', not 'Draft'. " +
            "The test harness never calls post, postAndSend, send, cancel, or corrective actions. A tenant workflow, " +
            "job queue, or extension may be changing the invoice. Disable sales-invoice writes, inspect and clean up " +
            "the captured DHIT record in Business Central, and do not rerun until the invoice remains an unposted draft.");
    }

    private static void EnsureDraftSalesQuote(
        BusinessCentralSalesQuote quote,
        string operation)
    {
        if (string.Equals(quote.Status, "Draft", StringComparison.OrdinalIgnoreCase)) return;

        throw new InvalidOperationException(
            $"The preflight sales quote was {operation} with status '{quote.Status ?? "<missing>"}', not 'Draft'. " +
            "The test harness never calls send, makeOrder, makeInvoice, or any other lifecycle action. A tenant " +
            "workflow, job queue, or extension may be changing the quote. Inspect and clean up the captured DHIT " +
            "record before rerunning.");
    }

    private static void EnsureDraftSalesCreditMemo(
        BusinessCentralSalesCreditMemo creditMemo,
        string operation)
    {
        if (string.Equals(creditMemo.Status, "Draft", StringComparison.OrdinalIgnoreCase)) return;

        throw new InvalidOperationException(
            $"The preflight sales credit memo was {operation} with status " +
            $"'{creditMemo.Status ?? "<missing>"}', not 'Draft'. The test harness never calls post, send, " +
            "cancel, corrective, or any other lifecycle action. A tenant workflow, job queue, or extension may " +
            "be changing the credit memo. Disable sales-credit-memo writes, inspect and clean up the captured " +
            "DHIT record, and do not rerun until the credit memo remains an unposted draft.");
    }

    private static void EnsureDraftPurchaseOrder(
        BusinessCentralPurchaseOrder order,
        string operation)
    {
        if (string.Equals(order.Status, "Draft", StringComparison.OrdinalIgnoreCase)) return;

        throw new InvalidOperationException(
            $"The preflight purchase order was {operation} with status '{order.Status ?? "<missing>"}', not 'Draft'. " +
            "The test harness never calls receiveAndInvoice. A tenant workflow, job queue, or extension may be " +
            "changing the order. Inspect and clean up the captured DHIT record before rerunning.");
    }

    private static void EnsureDraftPurchaseInvoice(
        BusinessCentralPurchaseInvoice invoice,
        string operation)
    {
        if (string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase)) return;

        throw new InvalidOperationException(
            $"The preflight purchase invoice was {operation} with status '{invoice.Status ?? "<missing>"}', not 'Draft'. " +
            "The test harness never calls post. A tenant workflow, job queue, or extension may be changing the " +
            "invoice. Disable purchase-invoice writes, inspect and clean up the captured DHIT record, and do not " +
            "rerun until the invoice remains an unposted draft.");
    }

    private static void EnsureDraftPurchaseCreditMemo(
        BusinessCentralPurchaseCreditMemo creditMemo,
        string operation)
    {
        if (string.Equals(creditMemo.Status, "Draft", StringComparison.OrdinalIgnoreCase)) return;

        throw new InvalidOperationException(
            $"The preflight purchase credit memo was {operation} with status " +
            $"'{creditMemo.Status ?? "<missing>"}', not 'Draft'. The test harness never calls post, cancel, " +
            "or any other lifecycle action. Disable purchase-credit-memo writes, inspect the captured DHIT " +
            "record, and do not rerun until it remains an unposted draft.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var postedSalesDocumentDeletionBlocked =
            BusinessCentralIntegrationFailureDiagnostics.IsPostedSalesDocumentDeletionBlocked(body);
        var postedPurchaseDocumentDeletionBlocked =
            BusinessCentralIntegrationFailureDiagnostics.IsPostedPurchaseDocumentDeletionBlocked(body);
        var purchaseInvoicePlaceholderPermissionBlocked =
            BusinessCentralIntegrationFailureDiagnostics.IsPurchaseInvoicePlaceholderPermissionBlocked(body);
        var salesCreditMemoPlaceholderPermissionBlocked =
            BusinessCentralIntegrationFailureDiagnostics.IsSalesCreditMemoPlaceholderPermissionBlocked(body);
        var generalLedgerEntryReadPermissionBlocked =
            BusinessCentralIntegrationFailureDiagnostics.IsGeneralLedgerEntryReadPermissionBlocked(body);
        var permissionFailure = response.StatusCode == HttpStatusCode.Forbidden ||
            body.Contains("current permissions prevented", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("Contact Duplicate Indirect", StringComparison.OrdinalIgnoreCase);
        var diagnosis = postedSalesDocumentDeletionBlocked
            ? BusinessCentralIntegrationFailureDiagnostics.PostedSalesDocumentDeletionGuidance
            : postedPurchaseDocumentDeletionBlocked
            ? BusinessCentralIntegrationFailureDiagnostics.PostedPurchaseDocumentDeletionGuidance
            : purchaseInvoicePlaceholderPermissionBlocked
            ? BusinessCentralIntegrationFailureDiagnostics.PurchaseInvoicePlaceholderPermissionGuidance
            : salesCreditMemoPlaceholderPermissionBlocked
            ? BusinessCentralIntegrationFailureDiagnostics.SalesCreditMemoPlaceholderPermissionGuidance
            : generalLedgerEntryReadPermissionBlocked
            ? BusinessCentralIntegrationFailureDiagnostics.GeneralLedgerEntryReadPermissionGuidance
            : permissionFailure
            ? body.Contains("Contact Duplicate", StringComparison.OrdinalIgnoreCase)
                ? "The Entra app is authenticated but lacks Business Central permissions for this operation. " +
                  "Customer name updates require indirect read/insert/delete access to table 5085 Contact Duplicate " +
                  "and table 5086 Cont. Duplicate Search String."
                : BusinessCentralIntegrationFailureDiagnostics.MissingEntityPermissionGuidance(operation)
            : response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Token acquisition succeeded, but Business Central rejected the token. Check the tenant, client ID, secret, and API.ReadWrite.All admin consent.",
            HttpStatusCode.NotFound => "Check the Business Central environment name and company GUID.",
            _ => "Check the Business Central sandbox configuration and service availability."
        };

        throw new InvalidOperationException(
            $"Business Central could not {operation}: {(int)response.StatusCode} {response.ReasonPhrase}. {diagnosis} Response: {body}");
    }

    private static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        DataHubCosmosDbEmulator cosmos,
        BusinessCentralIntegrationSettings settings,
        AgentTestInstance testInstance)
    {
        services.AddDataHub(dataHubOptions => dataHubOptions
            .WithAppSettingsConfig(configuration, "DataHub")
            .WithProcessingLockOptions(cfg => cfg.UseRedisRepository(_ => { }))
            .WithDatabase(cfg => cfg.UseCosmosDatabase(db =>
            {
                db.ConnString = cosmos.ConnectionString!;
                db.Database = "DataHub";
                db.AutoCreateContainers = true;
                db.DataContainer = "Entities";
                db.TrackingDataContainer = "TrackingData";
                db.SyncMarkersContainer = "SyncMarkers";
                db.SyncFailuresContainer = "Logs";
                db.ResolutionPromisesContainer = "ResolutionPromises";
                db.ConfigsContainer = "Configs";
                db.ManagementContainer = "Management";
                db.UseGateway = true;
            }))
            .WithDataHubEntityDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "Entities";
                cfg.PartitionKey = "entityType/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item.Value<string>(nameof(DataHubEntity.entityType))}/{item.Value<string>(nameof(DataHubEntity.id))}";
            })
            .WithChangeTrackingDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "TrackingData";
                cfg.PartitionKey = "DataSource/EntityType/EntityId";
                cfg.GetItemPartitionKeyFunc = item => $"{item.DataSource}/{item.EntityType}/{item.EntityId}";
            })
            .WithSyncFailureDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "Logs";
                cfg.PartitionKey = "Type/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item.Type}/{item.id}";
            })
            .WithMergeMarkerDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "SyncMarkers";
                cfg.PartitionKey = "id";
                cfg.GetItemPartitionKeyFunc = item => item.id;
            })
            .WithSyncMarkerDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "SyncMarkers";
                cfg.PartitionKey = "id";
                cfg.GetItemPartitionKeyFunc = item => item.id;
            })
            .WithResolutionPromiseDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "ResolutionPromises";
                cfg.PartitionKey = "pk";
                cfg.GetItemPartitionKeyFunc = item => string.IsNullOrWhiteSpace(item.pk) ? item.id : item.pk;
                cfg.SetItemPartitionKeyFunc = item => item.pk = item.id;
            })
            .WithConfigDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "Configs";
                cfg.PartitionKey = "id";
                cfg.GetItemPartitionKeyFunc = item => item.id;
            })
            .WithAutoNumberSequenceDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "Configs";
                cfg.PartitionKey = "id";
                cfg.GetItemPartitionKeyFunc = item => item.id;
            })
            .WithUserDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "Management";
                cfg.PartitionKey = "_dt/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item._dt}/{item.id}";
            })
            .WithRoleDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "Management";
                cfg.PartitionKey = "_dt/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item._dt}/{item.id}";
            })
            .WithJobDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "Management";
                cfg.PartitionKey = "_dt/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item._dt}/{item.id}";
            })
            .WithDuplicateDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = "DataHub";
                cfg.ContainerName = "Management";
                cfg.PartitionKey = "_dt/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item._dt}/{item.id}";
            }));

        services.RemoveAll<CosmosClient>();
        services.AddSingleton(_ => new CosmosClient(
            cosmos.ConnectionString!,
            new CosmosClientOptions
            {
                Serializer = new DataHubDataSerializer(),
                MaxRetryAttemptsOnRateLimitedRequests = 500,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(300),
                HttpClientFactory = () => cosmos.CosmosDbContainer!.HttpClient,
                RequestTimeout = TimeSpan.FromMinutes(5),
                ConnectionMode = ConnectionMode.Gateway
            }));

        services.Configure<BusinessCentralIntegrationSettings>(configuration.GetSection(BusinessCentralIntegrationSettings.SectionName));
        services.AddBusinessCentralAgent(options => options.WithAppSettingsConfig(configuration, "BusinessCentralAgentOptions"));
        services.AddTransient<BusinessCentralAuthenticationHandler>();
        services.AddHttpClient("BusinessCentral").AddHttpMessageHandler<BusinessCentralAuthenticationHandler>();

        services.RemoveAll<IDataHubClient>();
        services.AddSingleton<IDataHubClient, InProcessDataHubClient>();
        services.AddTransient<IHandler<DeserializeClientRequestRequest, IRequest>, DeserializeClientRequestRequestHandler>();
        services.AddSingleton(testInstance);
        services.AddTransient<BusinessCentralAgent>();
        services.AddTransient<DataHubAgent>();

        services.AddTransient<IMapper, IntegrationMapper>();
        IntegrationMapper.RegisterMaps(services);
        services.AddTransient<ITypeMapper<DataHubAccount, BusinessCentralCustomer>, MapAccountToCustomer>();
        services.AddTransient<ITypeMapper<BusinessCentralCustomer, DataHubAccount>, MapCustomerToAccount>();
        services.AddTransient<ITypeMapper<DataHubContact, BusinessCentralContact>, MapContactToBusinessCentralContact>();
        services.AddTransient<ITypeMapper<BusinessCentralContact, DataHubContact>, MapBusinessCentralContactToContact>();
        services.AddTransient<ITypeMapper<DataHubProduct, BusinessCentralItem>, MapProductToItem>();
        services.AddTransient<ITypeMapper<BusinessCentralItem, DataHubProduct>, MapItemToProduct>();
        services.AddTransient<ITypeMapper<DataHubSalesInvoice, BusinessCentralSalesInvoice>, MapSalesInvoiceToBusinessCentralSalesInvoice>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesInvoice, DataHubSalesInvoice>, MapBusinessCentralSalesInvoiceToSalesInvoice>();
        services.AddTransient<ITypeMapper<DataHubSalesInvoiceLine, BusinessCentralSalesInvoiceLine>, MapSalesInvoiceLineToBusinessCentralSalesInvoiceLine>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesInvoiceLine, DataHubSalesInvoiceLine>, MapBusinessCentralSalesInvoiceLineToSalesInvoiceLine>();
        services.AddTransient<ITypeMapper<DataHubSalesCreditMemo, BusinessCentralSalesCreditMemo>, MapSalesCreditMemoToBusinessCentralSalesCreditMemo>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesCreditMemo, DataHubSalesCreditMemo>, MapBusinessCentralSalesCreditMemoToSalesCreditMemo>();
        services.AddTransient<ITypeMapper<DataHubSalesCreditMemoLine, BusinessCentralSalesCreditMemoLine>, MapSalesCreditMemoLineToBusinessCentralSalesCreditMemoLine>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesCreditMemoLine, DataHubSalesCreditMemoLine>, MapBusinessCentralSalesCreditMemoLineToSalesCreditMemoLine>();
        services.AddTransient<ITypeMapper<DataHubSalesOrder, BusinessCentralSalesOrder>, MapSalesOrderToBusinessCentralSalesOrder>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesOrder, DataHubSalesOrder>, MapBusinessCentralSalesOrderToSalesOrder>();
        services.AddTransient<ITypeMapper<DataHubSalesOrderLine, BusinessCentralSalesOrderLine>, MapSalesOrderLineToBusinessCentralSalesOrderLine>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesOrderLine, DataHubSalesOrderLine>, MapBusinessCentralSalesOrderLineToSalesOrderLine>();
        services.AddTransient<ITypeMapper<DataHubQuote, BusinessCentralSalesQuote>, MapQuoteToBusinessCentralSalesQuote>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesQuote, DataHubQuote>, MapBusinessCentralSalesQuoteToQuote>();
        services.AddTransient<ITypeMapper<DataHubQuoteLine, BusinessCentralSalesQuoteLine>, MapQuoteLineToBusinessCentralSalesQuoteLine>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesQuoteLine, DataHubQuoteLine>, MapBusinessCentralSalesQuoteLineToQuoteLine>();
        services.AddTransient<ITypeMapper<DataHubSupplier, BusinessCentralVendor>, MapSupplierToVendor>();
        services.AddTransient<ITypeMapper<BusinessCentralVendor, DataHubSupplier>, MapVendorToSupplier>();
        services.AddTransient<ITypeMapper<DataHubPurchaseOrder, BusinessCentralPurchaseOrder>, MapPurchaseOrderToBusinessCentralPurchaseOrder>();
        services.AddTransient<ITypeMapper<BusinessCentralPurchaseOrder, DataHubPurchaseOrder>, MapBusinessCentralPurchaseOrderToPurchaseOrder>();
        services.AddTransient<ITypeMapper<DataHubPurchaseOrderLine, BusinessCentralPurchaseOrderLine>, MapPurchaseOrderLineToBusinessCentralPurchaseOrderLine>();
        services.AddTransient<ITypeMapper<BusinessCentralPurchaseOrderLine, DataHubPurchaseOrderLine>, MapBusinessCentralPurchaseOrderLineToPurchaseOrderLine>();
        services.AddTransient<ITypeMapper<DataHubPurchaseInvoice, BusinessCentralPurchaseInvoice>, MapPurchaseInvoiceToBusinessCentralPurchaseInvoice>();
        services.AddTransient<ITypeMapper<BusinessCentralPurchaseInvoice, DataHubPurchaseInvoice>, MapBusinessCentralPurchaseInvoiceToPurchaseInvoice>();
        services.AddTransient<ITypeMapper<DataHubPurchaseInvoiceLine, BusinessCentralPurchaseInvoiceLine>, MapPurchaseInvoiceLineToBusinessCentralPurchaseInvoiceLine>();
        services.AddTransient<ITypeMapper<BusinessCentralPurchaseInvoiceLine, DataHubPurchaseInvoiceLine>, MapBusinessCentralPurchaseInvoiceLineToPurchaseInvoiceLine>();
        services.AddTransient<ITypeMapper<DataHubPurchaseCreditMemo, BusinessCentralPurchaseCreditMemo>, MapPurchaseCreditMemoToBusinessCentralPurchaseCreditMemo>();
        services.AddTransient<ITypeMapper<BusinessCentralPurchaseCreditMemo, DataHubPurchaseCreditMemo>, MapBusinessCentralPurchaseCreditMemoToPurchaseCreditMemo>();
        services.AddTransient<ITypeMapper<DataHubPurchaseCreditMemoLine, BusinessCentralPurchaseCreditMemoLine>, MapPurchaseCreditMemoLineToBusinessCentralPurchaseCreditMemoLine>();
        services.AddTransient<ITypeMapper<BusinessCentralPurchaseCreditMemoLine, DataHubPurchaseCreditMemoLine>, MapBusinessCentralPurchaseCreditMemoLineToPurchaseCreditMemoLine>();
        services.AddTransient<ITypeMapper<DataHubSalesShipment, BusinessCentralSalesShipment>, MapSalesShipmentToBusinessCentralSalesShipment>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesShipment, DataHubSalesShipment>, MapBusinessCentralSalesShipmentToSalesShipment>();
        services.AddTransient<ITypeMapper<DataHubSalesShipmentLine, BusinessCentralSalesShipmentLine>, MapSalesShipmentLineToBusinessCentralSalesShipmentLine>();
        services.AddTransient<ITypeMapper<BusinessCentralSalesShipmentLine, DataHubSalesShipmentLine>, MapBusinessCentralSalesShipmentLineToSalesShipmentLine>();
        services.AddTransient<ITypeMapper<DataHubPurchaseReceipt, BusinessCentralPurchaseReceipt>, MapPurchaseReceiptToBusinessCentralPurchaseReceipt>();
        services.AddTransient<ITypeMapper<BusinessCentralPurchaseReceipt, DataHubPurchaseReceipt>, MapBusinessCentralPurchaseReceiptToPurchaseReceipt>();
        services.AddTransient<ITypeMapper<DataHubPurchaseReceiptLine, BusinessCentralPurchaseReceiptLine>, MapPurchaseReceiptLineToBusinessCentralPurchaseReceiptLine>();
        services.AddTransient<ITypeMapper<BusinessCentralPurchaseReceiptLine, DataHubPurchaseReceiptLine>, MapBusinessCentralPurchaseReceiptLineToPurchaseReceiptLine>();
        services.AddTransient<ITypeMapper<DataHubGeneralLedgerAccount, BusinessCentralGeneralLedgerAccount>, MapGeneralLedgerAccountToBusinessCentralGeneralLedgerAccount>();
        services.AddTransient<ITypeMapper<BusinessCentralGeneralLedgerAccount, DataHubGeneralLedgerAccount>, MapBusinessCentralGeneralLedgerAccountToGeneralLedgerAccount>();
        services.AddTransient<ITypeMapper<DataHubBankAccount, BusinessCentralBankAccount>, MapBankAccountToBusinessCentralBankAccount>();
        services.AddTransient<ITypeMapper<BusinessCentralBankAccount, DataHubBankAccount>, MapBusinessCentralBankAccountToBankAccount>();
        services.AddTransient<ITypeMapper<DataHubFinancialDimension, BusinessCentralFinancialDimension>, MapFinancialDimensionToBusinessCentralFinancialDimension>();
        services.AddTransient<ITypeMapper<BusinessCentralFinancialDimension, DataHubFinancialDimension>, MapBusinessCentralFinancialDimensionToFinancialDimension>();
        services.AddTransient<ITypeMapper<DataHubFinancialDimensionValue, BusinessCentralFinancialDimensionValue>, MapFinancialDimensionValueToBusinessCentralFinancialDimensionValue>();
        services.AddTransient<ITypeMapper<BusinessCentralFinancialDimensionValue, DataHubFinancialDimensionValue>, MapBusinessCentralFinancialDimensionValueToFinancialDimensionValue>();
        services.AddTransient<ITypeMapper<DataHubCustomerPaymentJournal, BusinessCentralCustomerPaymentJournal>, MapCustomerPaymentJournalToBusinessCentralCustomerPaymentJournal>();
        services.AddTransient<ITypeMapper<BusinessCentralCustomerPaymentJournal, DataHubCustomerPaymentJournal>, MapBusinessCentralCustomerPaymentJournalToCustomerPaymentJournal>();
        services.AddTransient<ITypeMapper<DataHubCustomerPayment, BusinessCentralCustomerPayment>, MapCustomerPaymentToBusinessCentralCustomerPayment>();
        services.AddTransient<ITypeMapper<BusinessCentralCustomerPayment, DataHubCustomerPayment>, MapBusinessCentralCustomerPaymentToCustomerPayment>();
        services.AddTransient<ITypeMapper<DataHubCustomerPaymentDimension, BusinessCentralCustomerPaymentDimension>, MapCustomerPaymentDimensionToBusinessCentralCustomerPaymentDimension>();
        services.AddTransient<ITypeMapper<BusinessCentralCustomerPaymentDimension, DataHubCustomerPaymentDimension>, MapBusinessCentralCustomerPaymentDimensionToCustomerPaymentDimension>();
        services.AddTransient<ITypeMapper<DataHubVendorPaymentJournal, BusinessCentralVendorPaymentJournal>, MapVendorPaymentJournalToBusinessCentralVendorPaymentJournal>();
        services.AddTransient<ITypeMapper<BusinessCentralVendorPaymentJournal, DataHubVendorPaymentJournal>, MapBusinessCentralVendorPaymentJournalToVendorPaymentJournal>();
        services.AddTransient<ITypeMapper<DataHubVendorPayment, BusinessCentralVendorPayment>, MapVendorPaymentToBusinessCentralVendorPayment>();
        services.AddTransient<ITypeMapper<BusinessCentralVendorPayment, DataHubVendorPayment>, MapBusinessCentralVendorPaymentToVendorPayment>();
        services.AddTransient<ITypeMapper<DataHubVendorPaymentDimension, BusinessCentralVendorPaymentDimension>, MapVendorPaymentDimensionToBusinessCentralVendorPaymentDimension>();
        services.AddTransient<ITypeMapper<BusinessCentralVendorPaymentDimension, DataHubVendorPaymentDimension>, MapBusinessCentralVendorPaymentDimensionToVendorPaymentDimension>();
        services.AddTransient<ITypeMapper<DataHubGeneralJournal, BusinessCentralGeneralJournal>, MapGeneralJournalToBusinessCentralGeneralJournal>();
        services.AddTransient<ITypeMapper<BusinessCentralGeneralJournal, DataHubGeneralJournal>, MapBusinessCentralGeneralJournalToGeneralJournal>();
        services.AddTransient<ITypeMapper<DataHubGeneralJournalLine, BusinessCentralGeneralJournalLine>, MapGeneralJournalLineToBusinessCentralGeneralJournalLine>();
        services.AddTransient<ITypeMapper<BusinessCentralGeneralJournalLine, DataHubGeneralJournalLine>, MapBusinessCentralGeneralJournalLineToGeneralJournalLine>();
        services.AddTransient<ITypeMapper<DataHubGeneralJournalDimension, BusinessCentralGeneralJournalDimension>, MapGeneralJournalDimensionToBusinessCentralGeneralJournalDimension>();
        services.AddTransient<ITypeMapper<BusinessCentralGeneralJournalDimension, DataHubGeneralJournalDimension>, MapBusinessCentralGeneralJournalDimensionToGeneralJournalDimension>();
        services.AddTransient<ITypeMapper<DataHubGeneralLedgerEntry, BusinessCentralGeneralLedgerEntry>, MapGeneralLedgerEntryToBusinessCentralGeneralLedgerEntry>();
        services.AddTransient<ITypeMapper<BusinessCentralGeneralLedgerEntry, DataHubGeneralLedgerEntry>, MapBusinessCentralGeneralLedgerEntryToGeneralLedgerEntry>();
        services.AddTransient<ITypeMapper<DataHubItemLedgerEntry, BusinessCentralItemLedgerEntry>, MapItemLedgerEntryToBusinessCentralItemLedgerEntry>();
        services.AddTransient<ITypeMapper<BusinessCentralItemLedgerEntry, DataHubItemLedgerEntry>, MapBusinessCentralItemLedgerEntryToItemLedgerEntry>();
        services.AddTransient<ITypeMapper<DataHubCurrency, BusinessCentralCurrency>, MapCurrencyToBusinessCentralCurrency>();
        services.AddTransient<ITypeMapper<BusinessCentralCurrency, DataHubCurrency>, MapBusinessCentralCurrencyToCurrency>();
        services.AddTransient<ITypeMapper<DataHubPaymentTerm, BusinessCentralPaymentTerm>, MapPaymentTermToBusinessCentralPaymentTerm>();
        services.AddTransient<ITypeMapper<BusinessCentralPaymentTerm, DataHubPaymentTerm>, MapBusinessCentralPaymentTermToPaymentTerm>();
        services.AddTransient<ITypeMapper<DataHubPaymentMethod, BusinessCentralPaymentMethod>, MapPaymentMethodToBusinessCentralPaymentMethod>();
        services.AddTransient<ITypeMapper<BusinessCentralPaymentMethod, DataHubPaymentMethod>, MapBusinessCentralPaymentMethodToPaymentMethod>();
        services.AddTransient<ITypeMapper<DataHubUnitOfMeasure, BusinessCentralUnitOfMeasure>, MapUnitOfMeasureToBusinessCentralUnitOfMeasure>();
        services.AddTransient<ITypeMapper<BusinessCentralUnitOfMeasure, DataHubUnitOfMeasure>, MapBusinessCentralUnitOfMeasureToUnitOfMeasure>();
        services.AddTransient<ITypeMapper<DataHubInventoryLocation, BusinessCentralLocation>, MapInventoryLocationToBusinessCentralLocation>();
        services.AddTransient<ITypeMapper<BusinessCentralLocation, DataHubInventoryLocation>, MapBusinessCentralLocationToInventoryLocation>();
        services.AddTransient<ITypeMapper<DataHubProductVariant, BusinessCentralItemVariant>, MapProductVariantToBusinessCentralItemVariant>();
        services.AddTransient<ITypeMapper<BusinessCentralItemVariant, DataHubProductVariant>, MapBusinessCentralItemVariantToProductVariant>();

        RegisterHandlers(services);
    }

    private static void RegisterHandlers(IServiceCollection services)
    {
        services.AddTransient<IHandler<SendMergeFailuresToDataHubRequest, NullResponse>, SendMergeFailuresToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendMergeSuccessesToDataHubRequest, NullResponse>, SendMergeSuccessesToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendSyncFailuresToDataHubRequest, NullResponse>, SendSyncFailuresToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendSyncSuccessesToDataHubRequest, NullResponse>, SendSyncSuccessesToDataHubRequestHandler>();

        services.AddTransient<IHandler<GetBusinessCentralMergeMarkerRequest, GetBusinessCentralMergeMarkerResponse>, GetBusinessCentralMergeMarkerRequestHandler>();
        services.AddTransient<IHandler<UpdateBusinessCentralMergeMarkerRequest, UpdateBusinessCentralMergeMarkerResponse>, UpdateBusinessCentralMergeMarkerRequestHandler>();
        services.AddTransient<IHandler<GetBusinessCentralSyncMarkerRequest, GetBusinessCentralSyncMarkerResponse>, GetBusinessCentralSyncMarkerRequestHandler>();
        services.AddTransient<IHandler<UpdateBusinessCentralSyncMarkerRequest, UpdateBusinessCentralSyncMarkerResponse>, UpdateBusinessCentralSyncMarkerRequestHandler>();

        RegisterIncrementalBusinessCentralPair<DataHubAccount, BusinessCentralCustomer>(services);
        RegisterIncrementalBusinessCentralPair<DataHubContact, BusinessCentralContact>(services);
        RegisterIncrementalBusinessCentralPair<DataHubProduct, BusinessCentralItem>(services);
        RegisterIncrementalBusinessCentralPair<DataHubSalesInvoice, BusinessCentralSalesInvoice>(services);
        RegisterBusinessCentralPair<DataHubSalesInvoiceLine, BusinessCentralSalesInvoiceLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubSalesCreditMemo, BusinessCentralSalesCreditMemo>(services);
        RegisterBusinessCentralPair<DataHubSalesCreditMemoLine, BusinessCentralSalesCreditMemoLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubSalesOrder, BusinessCentralSalesOrder>(services);
        RegisterBusinessCentralPair<DataHubSalesOrderLine, BusinessCentralSalesOrderLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubQuote, BusinessCentralSalesQuote>(services);
        RegisterBusinessCentralPair<DataHubQuoteLine, BusinessCentralSalesQuoteLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubSupplier, BusinessCentralVendor>(services);
        RegisterIncrementalBusinessCentralPair<DataHubPurchaseOrder, BusinessCentralPurchaseOrder>(services);
        RegisterBusinessCentralPair<DataHubPurchaseOrderLine, BusinessCentralPurchaseOrderLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubPurchaseInvoice, BusinessCentralPurchaseInvoice>(services);
        RegisterBusinessCentralPair<DataHubPurchaseInvoiceLine, BusinessCentralPurchaseInvoiceLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubPurchaseCreditMemo, BusinessCentralPurchaseCreditMemo>(services);
        RegisterBusinessCentralPair<DataHubPurchaseCreditMemoLine, BusinessCentralPurchaseCreditMemoLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubSalesShipment, BusinessCentralSalesShipment>(services);
        RegisterBusinessCentralPair<DataHubSalesShipmentLine, BusinessCentralSalesShipmentLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubPurchaseReceipt, BusinessCentralPurchaseReceipt>(services);
        RegisterBusinessCentralPair<DataHubPurchaseReceiptLine, BusinessCentralPurchaseReceiptLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubGeneralLedgerAccount, BusinessCentralGeneralLedgerAccount>(services);
        RegisterIncrementalBusinessCentralPair<DataHubBankAccount, BusinessCentralBankAccount>(services);
        RegisterIncrementalBusinessCentralPair<DataHubFinancialDimension, BusinessCentralFinancialDimension>(services);
        RegisterIncrementalBusinessCentralPair<DataHubFinancialDimensionValue, BusinessCentralFinancialDimensionValue>(services);
        RegisterIncrementalBusinessCentralPair<DataHubCustomerPaymentJournal, BusinessCentralCustomerPaymentJournal>(services);
        RegisterBusinessCentralPair<DataHubCustomerPayment, BusinessCentralCustomerPayment>(services);
        RegisterIncrementalBusinessCentralPair<DataHubVendorPaymentJournal, BusinessCentralVendorPaymentJournal>(services);
        RegisterBusinessCentralPair<DataHubVendorPayment, BusinessCentralVendorPayment>(services);
        RegisterIncrementalBusinessCentralPair<DataHubGeneralJournal, BusinessCentralGeneralJournal>(services);
        RegisterBusinessCentralPair<DataHubGeneralJournalLine, BusinessCentralGeneralJournalLine>(services);
        RegisterIncrementalBusinessCentralPair<DataHubGeneralLedgerEntry, BusinessCentralGeneralLedgerEntry>(services);
        RegisterIncrementalBusinessCentralPair<DataHubItemLedgerEntry, BusinessCentralItemLedgerEntry>(services);
        RegisterIncrementalBusinessCentralPair<DataHubCurrency, BusinessCentralCurrency>(services);
        RegisterIncrementalBusinessCentralPair<DataHubPaymentTerm, BusinessCentralPaymentTerm>(services);
        RegisterIncrementalBusinessCentralPair<DataHubPaymentMethod, BusinessCentralPaymentMethod>(services);
        RegisterIncrementalBusinessCentralPair<DataHubUnitOfMeasure, BusinessCentralUnitOfMeasure>(services);
        RegisterIncrementalBusinessCentralPair<DataHubInventoryLocation, BusinessCentralLocation>(services);
        RegisterIncrementalBusinessCentralPair<DataHubProductVariant, BusinessCentralItemVariant>(services);
    }

    private static void RegisterBusinessCentralPair<TDataHubEntity, TBusinessCentralEntity>(
        IServiceCollection services)
        where TDataHubEntity : DataHubEntity, new()
        where TBusinessCentralEntity : BusinessCentralDocument, new()
    {
        services.AddTransient<IHandler<GetSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity>, List<TBusinessCentralEntity>>, GetSpecificBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity>>();
        services.AddTransient<IHandler<CreateBusinessCentralRecordsCommand<TBusinessCentralEntity>, CreateBusinessCentralRecordsResponse<TBusinessCentralEntity>>, CreateBusinessCentralRecordsCommandHandler<TBusinessCentralEntity>>();
        services.AddTransient<IHandler<UpdateBusinessCentralRecordsCommand<TBusinessCentralEntity>, UpdateBusinessCentralRecordsResponse<TBusinessCentralEntity>>, UpdateBusinessCentralRecordsCommandHandler<TBusinessCentralEntity>>();
        services.AddTransient<IHandler<RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity>, RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>>, RetrieveUpdatedDataHubEntitiesRequestHandler<TDataHubEntity>>();

        services.AddTransient<IHandler<SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse>, SyncSpecificDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<SyncDataHubEntitiesWithLocksRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse>, SyncDataHubEntitiesWithLocksRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse>, ProcessDataHubEntitySyncRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TBusinessCentralEntity>, EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TBusinessCentralEntity>>, EnsureReferencedEntitiesAreSyncdRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<ResolveResolutionPromisesRequest<TDataHubEntity, TBusinessCentralEntity>, ResolveResolutionPromisesResponse<TDataHubEntity, TBusinessCentralEntity>>, ResolveResolutionPromisesRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse>, SyncDependencyDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();
        services.AddTransient<IHandler<SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, NullResponse>, SyncUpdatedDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity>>();

        services.AddTransient<IHandler<MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>, MergeSpecificBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<MergeBusinessCentralEntitiesWithLocksRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>, MergeBusinessCentralEntitiesWithLocksRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<ProcessBusinessCentralEntityMergeRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>, ProcessBusinessCentralEntityMergeRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<MergeDependencyBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>>, MergeDependencyBusinessCentralEntitiesRequestRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
    }

    private static void RegisterIncrementalBusinessCentralPair<TDataHubEntity, TBusinessCentralEntity>(
        IServiceCollection services)
        where TDataHubEntity : DataHubEntity, new()
        where TBusinessCentralEntity : BusinessCentralDocument, IBusinessCentralIncrementalEntity, new()
    {
        RegisterBusinessCentralPair<TDataHubEntity, TBusinessCentralEntity>(services);
        services.AddTransient<IHandler<MergeUpdatedBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity>, NullResponse>, MergeUpdatedBusinessCentralEntitiesRequestHandler<TBusinessCentralEntity, TDataHubEntity>>();
    }

}

public sealed class BusinessCentralContactInformation
{
    public Guid ContactId { get; set; }

    public string? ContactNumber { get; set; }

    public string? ContactName { get; set; }

    public string? ContactType { get; set; }

    public Guid RelatedId { get; set; }

    public string? RelatedType { get; set; }
}

public sealed record BusinessCentralSalesOrderTestReferences(
    BusinessCentralCustomer Customer,
    BusinessCentralItem Item);

public sealed record BusinessCentralSalesInvoiceTestReferences(
    BusinessCentralCustomer Customer,
    BusinessCentralItem Item);

public sealed record BusinessCentralSalesCreditMemoTestReferences(
    BusinessCentralCustomer Customer,
    BusinessCentralItem Item);

public sealed record BusinessCentralSalesQuoteTestReferences(
    BusinessCentralCustomer Customer,
    BusinessCentralItem Item);

public sealed record BusinessCentralPurchaseOrderTestReferences(
    BusinessCentralVendor Vendor,
    BusinessCentralItem Item);

public sealed record BusinessCentralPurchaseInvoiceTestReferences(
    BusinessCentralVendor Vendor,
    BusinessCentralItem Item);

public sealed record BusinessCentralPurchaseCreditMemoTestReferences(
    BusinessCentralVendor Vendor,
    BusinessCentralItem Item);

public sealed record BusinessCentralSalesShipmentTestReference(
    BusinessCentralSalesShipment Shipment,
    IReadOnlyList<BusinessCentralSalesShipmentLine> Lines);

public sealed record BusinessCentralPurchaseReceiptTestReference(
    BusinessCentralPurchaseReceipt Receipt,
    IReadOnlyList<BusinessCentralPurchaseReceiptLine> Lines);

public sealed record BusinessCentralAccountingFoundationTestReferences(
    BusinessCentralGeneralLedgerAccount GeneralLedgerAccount,
    BusinessCentralBankAccount BankAccount,
    BusinessCentralFinancialDimension Dimension,
    BusinessCentralFinancialDimensionValue DimensionValue);

public sealed record BusinessCentralCustomerPaymentTestReferences(
    BusinessCentralCustomer Customer,
    BusinessCentralGeneralLedgerAccount GeneralLedgerAccount,
    BusinessCentralFinancialDimension Dimension,
    BusinessCentralFinancialDimensionValue DimensionValue);

public sealed record BusinessCentralVendorPaymentTestReferences(
    BusinessCentralVendor Vendor,
    BusinessCentralGeneralLedgerAccount GeneralLedgerAccount,
    BusinessCentralFinancialDimension Dimension,
    BusinessCentralFinancialDimensionValue DimensionValue);

public sealed record BusinessCentralGeneralJournalTestReferences(
    BusinessCentralGeneralLedgerAccount GeneralLedgerAccount,
    BusinessCentralBankAccount BankAccount,
    BusinessCentralFinancialDimension Dimension,
    BusinessCentralFinancialDimensionValue DimensionValue);

public sealed record BusinessCentralGeneralLedgerEntryTestReferences(
    BusinessCentralGeneralLedgerEntry GeneralLedgerEntry,
    BusinessCentralGeneralLedgerAccount GeneralLedgerAccount);

public sealed record BusinessCentralItemVariantTestReferences(
    BusinessCentralItem Item,
    BusinessCentralItemVariant ItemVariant);
