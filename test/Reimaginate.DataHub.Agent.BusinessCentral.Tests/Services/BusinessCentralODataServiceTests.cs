using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.CustomExceptions;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeUpdatedBusinessCentralEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;
using BusinessCentralSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesOrder;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseOrder = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrder;
using BusinessCentralPurchaseOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseOrderLine;
using BusinessCentralGeneralJournalLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournalLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralODataServiceTests
{
    [Fact(DisplayName = "Business Central service options retain the custom API route by default")]
    [Trait("Category", "Unit")]
    public void ServiceOptionsUseCustomApiRouteByDefault()
    {
        var options = new BusinessCentralServiceOptions();

        options.ApiRoute.Should().Be("api/inviga/datahub/v2.0");
        options.CorrelationReservationsEnabled.Should().BeFalse();
        options.CorrelationApiRoute.Should().Be("api/reimaginate/dataHub/v1.0");
    }

    [Fact(DisplayName = "Incremental requests default inbound and outbound batches to 500")]
    [Trait("Category", "Unit")]
    public void IncrementalRequestsDefaultToFiveHundred()
    {
        new MergeUpdatedBusinessCentralEntitiesRequest<IncrementalCustomerDocument, TestDataHubEntity>()
            .BatchSize.Should().Be(500);
        new SyncUpdatedDataHubEntitiesRequest<TestDataHubEntity, CustomerDocument>()
            .BatchSize.Should().Be(500);
    }

    [Theory(DisplayName = "OData service composes configured API routes and entity-set names")]
    [Trait("Category", "Unit")]
    [InlineData(null, "api/inviga/datahub/v2.0/companies(company-1)/FallbackDocument")]
    [InlineData("/api/v2.0/", "api/v2.0/companies(company-1)/customers")]
    public async Task ComposesConfiguredRoutes(string? route, string expectedPath)
    {
        var handler = new RecordingHandler(_ => JsonResponse("{\"value\":[]}"));
        var service = CreateService(handler, route);

        if (route is null)
        {
            await service.GetEntitiesAsync<FallbackDocument>();
        }
        else
        {
            await service.GetEntitiesAsync<CustomerDocument>();
        }

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.PathAndQuery.Should().StartWith($"/{expectedPath}?");
    }

    [Fact(DisplayName = "OData service honors an entity-specific API route")]
    [Trait("Category", "Unit")]
    public async Task EntitySpecificApiRouteOverridesConfiguredRoute()
    {
        var handler = new RecordingHandler(_ => JsonResponse("{\"value\":[]}"));
        var service = CreateService(handler, "api/v2.0");

        await service.GetEntitiesAsync<CustomRouteDocument>();

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.PathAndQuery.Should().StartWith(
            "/api/reimaginate/dataHub/v1.0/companies(company-1)/generalLedgerEntries?");
    }

    [Fact(DisplayName = "OData service sends wildcard If-Match when deleting")]
    [Trait("Category", "Unit")]
    public async Task DeleteUsesEntitySetOverrideAndWildcardIfMatch()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var service = CreateService(handler, "api/v2.0");

        var result = await service.DeleteEntityAsync<CustomerDocument>("customer-1");

        result.IsT0.Should().BeTrue();
        var request = handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri!.PathAndQuery.Should().Be("/api/v2.0/companies(company-1)/customers(customer-1)");
        request.Headers.GetValues("If-Match").Should().ContainSingle().Which.Should().Be("*");
    }

    [Fact(DisplayName = "OData uses the parent invoice route and current ETag for line mutations")]
    [Trait("Category", "Unit")]
    public async Task SalesInvoiceLineMutationsUseParentRouteAndCurrentEtag()
    {
        var invoiceId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var handler = new RecordingHandler(request => request.Method == HttpMethod.Delete
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : JsonResponse($"{{\"id\":\"{lineId}\",\"documentId\":\"{invoiceId}\",\"quantity\":2}}"));
        var service = CreateService(handler, "api/v2.0");

        var created = await service.CreateEntityAsync(new BusinessCentralSalesInvoiceLine
        {
            DocumentId = invoiceId,
            Quantity = 1m
        });
        created.AsT0.Success.Should().BeTrue();

        var updated = await service.UpdateEntityAsync(new BusinessCentralSalesInvoiceLine
        {
            Id = lineId.ToString(),
            DocumentId = invoiceId,
            ETag = "W/\"line-etag\"",
            Quantity = 2m
        }, CancellationToken.None);
        updated.Success.Should().BeTrue();

        var deleted = await service.DeleteEntityAsync(new BusinessCentralSalesInvoiceLine
        {
            Id = lineId.ToString(),
            DocumentId = invoiceId,
            ETag = "W/\"delete-etag\""
        });
        deleted.IsT0.Should().BeTrue();

        var expectedCollection =
            $"/api/v2.0/companies(company-1)/salesInvoices({invoiceId})/salesInvoiceLines";
        handler.Requests.Should().HaveCount(3);
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be(expectedCollection);
        handler.Requests[1].Method.Should().Be(HttpMethod.Patch);
        handler.Requests[1].RequestUri!.PathAndQuery.Should().Be($"{expectedCollection}({lineId})");
        handler.Requests[1].Headers.GetValues("if-match").Should().ContainSingle()
            .Which.Should().Be("W/\"line-etag\"");
        handler.Requests[2].Method.Should().Be(HttpMethod.Delete);
        handler.Requests[2].RequestUri!.PathAndQuery.Should().Be($"{expectedCollection}({lineId})");
        handler.Requests[2].Headers.GetValues("If-Match").Should().ContainSingle()
            .Which.Should().Be("W/\"delete-etag\"");
    }

    [Fact(DisplayName = "OData reads parent-scoped collections and records through the parent navigation route")]
    [Trait("Category", "Unit")]
    public async Task ParentScopedReadsUseParentNavigationRoute()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => JsonResponse(
            $"{{\"@odata.count\":1,\"value\":[{{\"id\":\"{childId}\",\"parentId\":\"{parentId}\"}}]}}"));
        var service = CreateService(handler, "api/v2.0");

        var collection = await service.GetEntitiesAsync<ParentScopedDocument>(parentId, top: 10);
        var entity = await service.GetEntityAsync<ParentScopedDocument>(parentId, childId);

        collection.IsT0.Should().BeTrue();
        entity.IsT0.Should().BeTrue();
        handler.Requests.Should().HaveCount(2);
        var expectedPrefix =
            $"/api/v2.0/companies(company-1)/customerPayments({parentId})/dimensionSetLines?";
        handler.Requests.Should().OnlyContain(request =>
            request.RequestUri!.PathAndQuery.StartsWith(expectedPrefix, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "OData rejects parent-scoped writes without a parent id")]
    [Trait("Category", "Unit")]
    public async Task ParentScopedWriteRequiresParentId()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("Transport should not be called."));
        var service = CreateService(handler, "api/v2.0");

        var result = await service.CreateEntityAsync(new BusinessCentralSalesInvoiceLine
        {
            Quantity = 1m
        });

        result.IsT1.Should().BeTrue();
        result.AsT1.Message.Should().Contain("requires 'DocumentId'");
        handler.Requests.Should().BeEmpty();
    }

    [Fact(DisplayName = "OData service safely encodes every collection query option value")]
    [Trait("Category", "Unit")]
    public async Task GetEntitiesUsesStandardQueryOptions()
    {
        const string filter = "externalDocumentNumber eq 'DHIT-O''BRIEN-&-TOKYO'";
        const string order = "lastModifiedDateTime desc";
        const string select = "id,externalDocumentNumber,lastModifiedDateTime";
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.OK, "{\"value\":[]}");
        var service = CreateService(transport, "api/v2.0");

        await service.GetEntitiesAsync<CustomerDocument>(
            filter: filter,
            skip: 10,
            top: 25,
            order: order,
            select: select);

        var requestUri = transport.Requests.Should().ContainSingle().Subject.Uri;
        var queryOptions = requestUri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(option => option.Split('=', 2))
            .ToDictionary(
                option => Uri.UnescapeDataString(option[0]),
                option => Uri.UnescapeDataString(option[1]),
                StringComparer.Ordinal);

        queryOptions.Should().HaveCount(6);
        queryOptions["$filter"].Should().Be(filter);
        queryOptions["$skip"].Should().Be("10");
        queryOptions["$top"].Should().Be("25");
        queryOptions["$orderby"].Should().Be(order);
        queryOptions["$select"].Should().Be(select);
        queryOptions["$count"].Should().Be("true");
        requestUri.OriginalString.Should().Contain("%26");
    }

    [Theory(DisplayName = "OData create failures identify the diagnostic category")]
    [Trait("Category", "FaultInjection")]
    [InlineData(HttpStatusCode.Unauthorized, "authentication")]
    [InlineData(HttpStatusCode.Forbidden, "authorization")]
    [InlineData(HttpStatusCode.NotFound, "environment, company, or record")]
    [InlineData(HttpStatusCode.BadRequest, "validation")]
    public async Task CreateFailuresAreCategorised(HttpStatusCode statusCode, string category)
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(statusCode, "{\"error\":{\"message\":\"failure\"}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 1);

        var response = await service.CreateEntityAsync(new CustomerDocument { Number = "DHIT-FAIL" });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Should().BeOfType<BusinessCentralHttpException>();
        response.AsT0.Exception.Message.Should().Contain(category);
        transport.Requests.Should().ContainSingle();
    }

    [Fact(DisplayName = "OData writes decimal values as JSON numbers and reads string decimals")]
    [Trait("Category", "Unit")]
    public async Task DecimalValuesUseBusinessCentralCompatibleJson()
    {
        var id = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.OK, $"{{\"id\":\"{id}\",\"unitPrice\":\"12.34\"}}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.CreateEntityAsync(new DecimalDocument { UnitPrice = 12.34m });

        response.AsT0.Success.Should().BeTrue();
        response.AsT0.ResultingEntity!.UnitPrice.Should().Be(12.34m);
        transport.Requests.Should().ContainSingle().Which.Body.Should().Contain("\"unitPrice\":12.34");
        transport.Requests[0].Body.Should().NotContain("\"unitPrice\":\"12.34\"");
    }

    [Fact(DisplayName = "General journal creates send a bank balancing account by number")]
    [Trait("Category", "Unit")]
    public async Task GeneralJournalCreateUsesBankBalancingAccountNumber()
    {
        var journalId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Created,
                $"{{\"id\":\"{lineId}\",\"journalId\":\"{journalId}\",\"accountType\":\"G_x002F_L_x0020_Account\",\"accountId\":\"{accountId}\",\"balanceAccountType\":\"Bank_x0020_Account\",\"balancingAccountNumber\":\"BANK-1\"}}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.CreateEntityAsync(new BusinessCentralGeneralJournalLine
        {
            JournalId = journalId,
            AccountType = "G/L Account",
            AccountId = accountId,
            BalanceAccountType = "Bank Account",
            BalancingAccountNumber = "BANK-1"
        });

        response.AsT0.Success.Should().BeTrue();
        response.AsT0.ResultingEntity!.AccountType.Should().Be("G/L Account");
        response.AsT0.ResultingEntity.BalanceAccountType.Should().Be("Bank Account");
        var body = transport.Requests.Should().ContainSingle().Subject.Body!;
        body.Should().Contain("\"balanceAccountType\":\"Bank Account\"");
        body.Should().Contain("\"balancingAccountNumber\":\"BANK-1\"");
        body.Should().NotContain("\"balancingAccountId\"");
        body.IndexOf("\"balanceAccountType\"", StringComparison.Ordinal).Should().BeLessThan(
            body.IndexOf("\"balancingAccountNumber\"", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "OData retries throttling and transient server failures within a bound")]
    [Trait("Category", "FaultInjection")]
    public async Task RetriesTransientReads()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.TooManyRequests)
            .Respond(HttpStatusCode.ServiceUnavailable)
            .Respond(HttpStatusCode.OK, "{\"value\":[{\"id\":\"11111111-1111-1111-1111-111111111111\"}]}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.GetEntitiesAsync<CustomerDocument>();

        response.IsT0.Should().BeTrue();
        response.AsT0.Value.Should().ContainSingle();
        transport.Requests.Should().HaveCount(3);
    }

    [Fact(DisplayName = "OData does not retry permanent authentication failures")]
    [Trait("Category", "FaultInjection")]
    public async Task DoesNotRetryPermanentFailures()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Unauthorized);
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.GetEntitiesAsync<CustomerDocument>();

        response.IsT1.Should().BeTrue();
        response.AsT1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        transport.Requests.Should().ContainSingle();
        response.AsT1.Dispose();
    }

    [Fact(DisplayName = "OData distinguishes a missing customer from a malformed response")]
    [Trait("Category", "FaultInjection")]
    public async Task ReportsMissingCustomer()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.OK, "{\"value\":[]}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.GetEntityAsync<CustomerDocument>(Guid.NewGuid());

        response.IsT1.Should().BeTrue();
        response.AsT1.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.AsT1.ReasonPhrase.Should().Contain("was not found");
        response.AsT1.Dispose();
    }

    [Fact(DisplayName = "OData follows every nextLink page exactly once")]
    [Trait("Category", "FaultInjection")]
    public async Task FollowsNextLinkPages()
    {
        var nextLink =
            "https://businesscentral.example/api/v2.0/companies(company-1)/customers" +
            "?$skiptoken=second%2Bpage" +
            "&$filter=externalDocumentNumber%20eq%20%27DHIT-O%27%27BRIEN-%26-TOKYO%27";
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.OK,
                $"{{\"@odata.count\":2,\"value\":[{{\"id\":\"11111111-1111-1111-1111-111111111111\"}}],\"@odata.nextLink\":\"{nextLink}\"}}")
            .Respond(HttpStatusCode.OK,
                "{\"value\":[{\"id\":\"22222222-2222-2222-2222-222222222222\"}]}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.GetEntitiesAsync<CustomerDocument>();

        response.IsT0.Should().BeTrue();
        response.AsT0.Value.Select(customer => customer.Id).Should().Equal(
            "11111111-1111-1111-1111-111111111111",
            "22222222-2222-2222-2222-222222222222");
        transport.Requests.Should().HaveCount(2);
        transport.Requests[1].Uri.OriginalString.Should().Be(nextLink);
        transport.Requests[1].Uri.OriginalString.Should().NotContain("%2526");
    }

    [Fact(DisplayName = "OData rejects repeated nextLink pages")]
    [Trait("Category", "FaultInjection")]
    public async Task RejectsRepeatedNextLink()
    {
        const string nextLink = "https://businesscentral.example/repeated";
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.OK, $"{{\"value\":[],\"@odata.nextLink\":\"{nextLink}\"}}")
            .Respond(HttpStatusCode.OK, $"{{\"value\":[],\"@odata.nextLink\":\"{nextLink}\"}}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.GetEntitiesAsync<CustomerDocument>();

        response.IsT2.Should().BeTrue();
        response.AsT2.Message.Should().Contain("repeated @odata.nextLink");
    }

    [Fact(DisplayName = "OData recovers an ambiguous create by deterministic customer number")]
    [Trait("Category", "FaultInjection")]
    public async Task RecoversCreateAfterTimeout()
    {
        var id = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Throw(new HttpRequestException("Connection closed after the server committed the create."))
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{id}\",\"number\":\"DHIT-RECOVER\"}}]}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(new CustomerDocument { Number = "DHIT-RECOVER" });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeTrue();
        response.AsT0.EntityId.Should().Be(id.ToString());
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests.Count(request => request.Method == HttpMethod.Get).Should().Be(1);
    }

    [Fact(DisplayName = "OData recovers an ambiguous sales-order create by external document number")]
    [Trait("Category", "FaultInjection")]
    public async Task RecoversSalesOrderCreateAfterTimeout()
    {
        var id = Guid.NewGuid();
        const string externalDocumentNumber = "DHIT-SO-RECOVER";
        var transport = new DeterministicBusinessCentralTransport()
            .Throw(new TaskCanceledException("The response timed out after Business Central committed the order."))
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{id}\",\"externalDocumentNumber\":\"{externalDocumentNumber}\"}}]}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(new BusinessCentralSalesOrder
        {
            ExternalDocumentNumber = externalDocumentNumber
        });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeTrue();
        response.AsT0.EntityId.Should().Be(id.ToString());
        transport.Requests.Should().HaveCount(2);
        transport.Requests[0].Method.Should().Be(HttpMethod.Post);
        transport.Requests[1].Method.Should().Be(HttpMethod.Get);
        transport.Requests[1].Uri.Query.Should().Contain("externalDocumentNumber");
    }

    [Fact(DisplayName = "OData recovers an ambiguous purchase invoice only by vendor and invoice number")]
    [Trait("Category", "FaultInjection")]
    public async Task RecoversPurchaseInvoiceByCompositeVendorKey()
    {
        var id = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        const string vendorInvoiceNumber = "DHIT-PI-RECOVER";
        var transport = new DeterministicBusinessCentralTransport()
            .Throw(new HttpRequestException("Connection closed after the server committed the invoice."))
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{id}\",\"vendorId\":\"{vendorId}\",\"vendorInvoiceNumber\":\"{vendorInvoiceNumber}\"}}]}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(new BusinessCentralPurchaseInvoice
        {
            VendorId = vendorId,
            VendorInvoiceNumber = vendorInvoiceNumber
        });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeTrue();
        response.AsT0.EntityId.Should().Be(id.ToString());
        transport.Requests.Should().HaveCount(2);
        Uri.UnescapeDataString(transport.Requests[1].Uri.Query).Should().Be(
            $"?$filter=vendorInvoiceNumber eq '{vendorInvoiceNumber}' and vendorId eq {vendorId}" +
            "&$top=2&$count=true");
    }

    [Fact(DisplayName = "OData rejects a purchase-invoice recovery match from another vendor")]
    [Trait("Category", "FaultInjection")]
    public async Task DoesNotRecoverPurchaseInvoiceFromAnotherVendor()
    {
        var expectedVendorId = Guid.NewGuid();
        var otherVendorId = Guid.NewGuid();
        const string vendorInvoiceNumber = "DHIT-PI-COLLISION";
        var transport = new DeterministicBusinessCentralTransport()
            .Throw(new HttpRequestException("Connection closed after the server may have committed the invoice."))
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{Guid.NewGuid()}\",\"vendorId\":\"{otherVendorId}\",\"vendorInvoiceNumber\":\"{vendorInvoiceNumber}\"}}]}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 1);

        var response = await service.CreateEntityAsync(new BusinessCentralPurchaseInvoice
        {
            VendorId = expectedVendorId,
            VendorInvoiceNumber = vendorInvoiceNumber
        });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeFalse();
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests.Count(request => request.Method == HttpMethod.Get).Should().Be(1);
        Uri.UnescapeDataString(transport.Requests[1].Uri.Query).Should().Be(
            $"?$filter=vendorInvoiceNumber eq '{vendorInvoiceNumber}' and vendorId eq {expectedVendorId}" +
            "&$top=2&$count=true");
    }

    [Fact(DisplayName = "OData fails closed when a composite purchase-invoice recovery key is incomplete")]
    [Trait("Category", "FaultInjection")]
    public async Task DoesNotRetryPurchaseInvoiceWithIncompleteCompositeKey()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Throw(new TaskCanceledException("The response timed out after the server may have committed."))
            .Respond(HttpStatusCode.OK, $"{{\"id\":\"{Guid.NewGuid()}\"}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(new BusinessCentralPurchaseInvoice
        {
            VendorInvoiceNumber = "DHIT-PI-INCOMPLETE"
        });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Message.Should().Contain("no deterministic recovery key");
        transport.Requests.Should().ContainSingle();
    }

    [Fact(DisplayName = "OData does not retry an ambiguous create without a recovery key")]
    [Trait("Category", "FaultInjection")]
    public async Task DoesNotRetryAmbiguousUnkeyedCreate()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Throw(new TaskCanceledException("The response timed out after the server may have committed."))
            .Respond(HttpStatusCode.OK, $"{{\"id\":\"{Guid.NewGuid()}\"}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(new UnkeyedDocument());

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Should().BeOfType<InvalidOperationException>();
        response.AsT0.Exception.Message.Should().Contain("not retried");
        response.AsT0.Exception.Message.Should().Contain("duplicate");
        transport.Requests.Should().ContainSingle();
    }

    [Fact(DisplayName = "OData does not re-post an ambiguous keyed create when recovery finds no record")]
    [Trait("Category", "FaultInjection")]
    public async Task DoesNotRetryAmbiguousKeyedCreateAfterRecoveryMiss()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Throw(new HttpRequestException("The connection closed before the create response was received."))
            .Respond(HttpStatusCode.OK, "{\"value\":[]}")
            .Respond(HttpStatusCode.Created,
                $"{{\"id\":\"{Guid.NewGuid()}\",\"number\":\"DHIT-AMBIGUOUS\"}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(
            new CustomerDocument { Number = "DHIT-AMBIGUOUS" });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Message.Should().Contain("recovery lookup");
        response.AsT0.Exception.Message.Should().Contain("could create a duplicate");
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests.Count(request => request.Method == HttpMethod.Get).Should().Be(1);
        transport.Requests.Should().HaveCount(2);
    }

    [Fact(DisplayName = "OData does not re-post a keyed create after an ambiguous server failure and recovery miss")]
    [Trait("Category", "FaultInjection")]
    public async Task DoesNotRetryAmbiguousKeyedCreateAfterServerFailureRecoveryMiss()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.ServiceUnavailable, "{\"error\":{\"message\":\"service unavailable\"}}")
            .Respond(HttpStatusCode.OK, "{\"value\":[]}")
            .Respond(HttpStatusCode.Created,
                $"{{\"id\":\"{Guid.NewGuid()}\",\"number\":\"DHIT-AMBIGUOUS-503\"}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(
            new CustomerDocument { Number = "DHIT-AMBIGUOUS-503" });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Message.Should().Contain("returned 503");
        response.AsT0.Exception.Message.Should().Contain("recovery lookup");
        response.AsT0.Exception.Message.Should().Contain("could create a duplicate");
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests.Count(request => request.Method == HttpMethod.Get).Should().Be(1);
        transport.Requests.Should().HaveCount(2);
    }

    [Fact(DisplayName = "OData recovers a create retry after tracking failure by deterministic customer number")]
    [Trait("Category", "FaultInjection")]
    public async Task RecoversCreateAfterConflict()
    {
        var id = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Conflict, "{\"error\":{\"message\":\"number already exists\"}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{id}\",\"number\":\"DHIT-DUPLICATE\"}}]}}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.CreateEntityAsync(new CustomerDocument { Number = "DHIT-DUPLICATE" });

        response.AsT0.Success.Should().BeTrue();
        response.AsT0.EntityId.Should().Be(id.ToString());
        transport.Requests.Should().HaveCount(2);
    }

    [Fact(DisplayName = "OData recovers Business Central's duplicate-key 400 by deterministic customer number")]
    [Trait("Category", "FaultInjection")]
    public async Task RecoversCreateAfterDuplicateKeyBadRequest()
    {
        var id = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.BadRequest,
                "{\"error\":{\"code\":\"Internal_EntityWithSameKeyExists\",\"message\":\"The record already exists. Identification fields and values: No.='DHIT-DUPLICATE'\"}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{id}\",\"number\":\"DHIT-DUPLICATE\"}}]}}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.CreateEntityAsync(new CustomerDocument { Number = "DHIT-DUPLICATE" });

        response.AsT0.Success.Should().BeTrue();
        response.AsT0.EntityId.Should().Be(id.ToString());
        transport.Requests.Should().HaveCount(2);
        transport.Requests[0].Method.Should().Be(HttpMethod.Post);
        transport.Requests[1].Method.Should().Be(HttpMethod.Get);
    }

    [Fact(DisplayName = "OData recovers a malformed successful keyed create exactly once")]
    [Trait("Category", "FaultInjection")]
    public async Task RecoversMalformedSuccessfulKeyedCreate()
    {
        var recoveredId = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Created,
                "{\"number\":\"DHIT-MALFORMED-RECOVERED\",\"displayName\":\"missing id\"}")
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{recoveredId}\",\"number\":\"DHIT-MALFORMED-RECOVERED\"}}]}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(
            new CustomerDocument { Number = "DHIT-MALFORMED-RECOVERED" });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeTrue();
        response.AsT0.EntityId.Should().Be(recoveredId.ToString());
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests.Count(request => request.Method == HttpMethod.Get).Should().Be(1);
        transport.Requests.Should().HaveCount(2);
    }

    [Fact(DisplayName = "OData fails closed after malformed successful keyed create recovery misses")]
    [Trait("Category", "FaultInjection")]
    public async Task DoesNotRepostMalformedSuccessfulKeyedCreateAfterRecoveryMiss()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Created, "{\"number\":")
            .Respond(HttpStatusCode.OK, "{\"value\":[]}")
            .Respond(HttpStatusCode.Created,
                $"{{\"id\":\"{Guid.NewGuid()}\",\"number\":\"DHIT-MALFORMED-MISS\"}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(
            new CustomerDocument { Number = "DHIT-MALFORMED-MISS" });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Should().BeOfType<InvalidOperationException>();
        response.AsT0.Exception.Message.Should().Contain("create outcome is ambiguous");
        response.AsT0.Exception.Message.Should().Contain("recovery lookup");
        response.AsT0.Exception.Message.Should().Contain("duplicate");
        response.AsT0.Exception.Message.Should().Contain("reconcile");
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests.Count(request => request.Method == HttpMethod.Get).Should().Be(1);
        transport.Requests.Should().HaveCount(2);
    }

    [Fact(DisplayName = "OData fails closed after a null successful unkeyed create response")]
    [Trait("Category", "FaultInjection")]
    public async Task DoesNotRepostMalformedSuccessfulUnkeyedCreate()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Created, "null")
            .Respond(HttpStatusCode.Created, $"{{\"id\":\"{Guid.NewGuid()}\"}}");
        var service = CreateService(transport, "api/v2.0", maxRetryAttempts: 3);

        var response = await service.CreateEntityAsync(new UnkeyedDocument());

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Should().BeOfType<InvalidOperationException>();
        response.AsT0.Exception.Message.Should().Contain("create outcome is ambiguous");
        response.AsT0.Exception.Message.Should().Contain("no deterministic recovery key");
        response.AsT0.Exception.Message.Should().Contain("duplicate");
        response.AsT0.Exception.Message.Should().Contain("reconcile");
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests.Should().ContainSingle();
    }

    [Fact(DisplayName = "OData rejects malformed collection responses")]
    [Trait("Category", "FaultInjection")]
    public async Task RejectsMalformedCollectionResponses()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.OK, "{}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.GetEntitiesAsync<CustomerDocument>();

        response.IsT2.Should().BeTrue();
        response.AsT2.Message.Should().Contain("without a value array");
    }

    [Fact(DisplayName = "OData PATCH preserves ETag and explicit field clearing")]
    [Trait("Category", "FaultInjection")]
    public async Task PatchPreservesConcurrencyAndNullClearing()
    {
        var id = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.OK, $"{{\"id\":\"{id}\",\"phoneNumber\":\"\"}}");
        var service = CreateService(transport, "api/v2.0");
        var patch = new CustomerDocument { Id = id.ToString(), ETag = "W/\"etag-7\"", PhoneNumber = null };

        var result = await service.UpdateEntityAsync(patch, CancellationToken.None);

        result.Success.Should().BeTrue();
        var request = transport.Requests.Should().ContainSingle().Subject;
        request.Headers["if-match"].Should().ContainSingle().Which.Should().Be("W/\"etag-7\"");
        request.Body.Should().Contain("\"phoneNumber\":\"\"");
        request.Body.Should().NotContain("\"id\"");
        request.Body.Should().NotContain("@odata.etag");
        result.ResultingEntity!.PhoneNumber.Should().BeNull();
    }

    [Fact(DisplayName = "OData PATCH uses and normalizes Business Central's undefined date")]
    [Trait("Category", "FaultInjection")]
    public async Task PatchUsesBusinessCentralUndefinedDateForDateClearing()
    {
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.OK,
                $"{{\"id\":\"{orderId}\",\"requestedReceiptDate\":\"0001-01-01\"}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"id\":\"{lineId}\",\"documentId\":\"{orderId}\",\"expectedReceiptDate\":\"0001-01-01\"}}");
        var service = CreateService(transport, "api/v2.0");

        var orderResult = await service.UpdateEntityAsync(new BusinessCentralPurchaseOrder
        {
            Id = orderId.ToString(),
            ETag = "W/\"order-etag\"",
            RequestedReceiptDate = null
        }, CancellationToken.None);
        var lineResult = await service.UpdateEntityAsync(new BusinessCentralPurchaseOrderLine
        {
            Id = lineId.ToString(),
            DocumentId = orderId,
            ETag = "W/\"line-etag\"",
            ExpectedReceiptDate = null
        }, CancellationToken.None);

        orderResult.Success.Should().BeTrue();
        lineResult.Success.Should().BeTrue();
        transport.Requests.Should().HaveCount(2);
        transport.Requests[0].Body.Should().Contain("\"requestedReceiptDate\":\"0001-01-01\"");
        transport.Requests[0].Body.Should().NotContain("\"requestedReceiptDate\":\"\"");
        transport.Requests[1].Body.Should().Contain("\"expectedReceiptDate\":\"0001-01-01\"");
        transport.Requests[1].Body.Should().NotContain("\"expectedReceiptDate\":\"\"");
        orderResult.ResultingEntity!.RequestedReceiptDate.Should().BeNull();
        lineResult.ResultingEntity!.ExpectedReceiptDate.Should().BeNull();
    }

    [Fact(DisplayName = "OData reports detected PATCH concurrency conflicts without overwriting")]
    [Trait("Category", "FaultInjection")]
    public async Task PatchReportsConcurrencyConflict()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.PreconditionFailed, "{\"error\":{\"message\":\"ETag changed\"}}");
        var service = CreateService(transport, "api/v2.0");

        var result = await service.UpdateEntityAsync(new CustomerDocument
        {
            Id = Guid.NewGuid().ToString(),
            ETag = "W/\"old\"",
            DisplayName = "Data Hub value"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        result.Exception!.Message.Should().Contain("concurrency");
        transport.Requests.Should().ContainSingle();
    }

    [Fact(DisplayName = "Correlation reservations create once and finish through the standard API")]
    [Trait("Category", "FaultInjection")]
    public async Task CorrelationReservationCreatesAndPatchesStandardEntity()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Created, $"{{\"id\":\"{orderId}\"}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{orderId}\",\"@odata.etag\":\"W/\\\"reserved\\\"\",\"customerId\":\"{customerId}\"}}]}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"id\":\"{orderId}\",\"@odata.etag\":\"W/\\\"updated\\\"\",\"customerId\":\"{customerId}\",\"externalDocumentNumber\":\"DHIT-CORR\"}}");
        var service = CreateService(
            transport,
            "api/v2.0",
            correlationReservationsEnabled: true);

        var response = await service.CreateEntityAsync(new BusinessCentralSalesOrder
        {
            DataHubCorrelationId = correlationId,
            CustomerId = customerId,
            ExternalDocumentNumber = "DHIT-CORR"
        });

        response.IsT0.Should().BeTrue();
        response.AsT0.Success.Should().BeTrue();
        response.AsT0.EntityId.Should().Be(orderId.ToString());
        transport.Requests.Should().HaveCount(3);
        transport.Requests[0].Method.Should().Be(HttpMethod.Post);
        transport.Requests[0].Uri.PathAndQuery.Should().Be(
            "/api/reimaginate/dataHub/v1.0/companies(company-1)/salesDocumentReservations");
        transport.Requests[0].Body.Should().Contain($"\"correlationId\":\"{correlationId}\"");
        transport.Requests[0].Body.Should().Contain($"\"customerId\":\"{customerId}\"");
        transport.Requests[0].Body.Should().Contain("\"documentType\":\"Order\"");
        transport.Requests[1].Method.Should().Be(HttpMethod.Get);
        transport.Requests[1].Uri.AbsolutePath.Should().EndWith("/salesOrders");
        transport.Requests[2].Method.Should().Be(HttpMethod.Patch);
        transport.Requests[2].Uri.AbsolutePath.Should().EndWith($"/salesOrders({orderId})");
        transport.Requests[2].Headers["if-match"].Should().ContainSingle().Which.Should().Be("W/\"reserved\"");
        transport.Requests[2].Body.Should().Contain("\"externalDocumentNumber\":\"DHIT-CORR\"");
        transport.Requests[2].Body.Should().NotContain("correlationId");
        transport.Requests.Count(request =>
            request.Method == HttpMethod.Post && request.Uri.AbsolutePath.EndsWith("/salesOrders", StringComparison.Ordinal))
            .Should().Be(0);
    }

    [Fact(DisplayName = "An ambiguous reservation recovers by unique correlation without another POST")]
    [Trait("Category", "FaultInjection")]
    public async Task CorrelationReservationRecoversAmbiguousCreate()
    {
        var orderId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.ServiceUnavailable, "{\"error\":{\"message\":\"response lost\"}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{orderId}\",\"correlationId\":\"{correlationId}\"}}]}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{orderId}\",\"@odata.etag\":\"W/\\\"reserved\\\"\",\"vendorId\":\"{vendorId}\"}}]}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"id\":\"{orderId}\",\"vendorId\":\"{vendorId}\"}}");
        var service = CreateService(
            transport,
            "api/v2.0",
            correlationReservationsEnabled: true);

        var response = await service.CreateEntityAsync(new BusinessCentralPurchaseOrder
        {
            DataHubCorrelationId = correlationId,
            VendorId = vendorId
        });

        response.AsT0.Success.Should().BeTrue();
        transport.Requests.Should().HaveCount(4);
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests[1].Method.Should().Be(HttpMethod.Get);
        Uri.UnescapeDataString(transport.Requests[1].Uri.Query).Should().Contain(
            $"$filter=correlationId eq {correlationId}");
        transport.Requests[2].Uri.AbsolutePath.Should().EndWith("/purchaseOrders");
        transport.Requests[3].Method.Should().Be(HttpMethod.Patch);
    }

    [Fact(DisplayName = "Line reservations finish through the standard parent-scoped route")]
    [Trait("Category", "FaultInjection")]
    public async Task CorrelationLineReservationUsesParentStandardRoute()
    {
        var documentId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Created, $"{{\"id\":\"{lineId}\"}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"value\":[{{\"id\":\"{lineId}\",\"@odata.etag\":\"W/\\\"reserved-line\\\"\",\"documentId\":\"{documentId}\",\"itemId\":\"{itemId}\"}}]}}")
            .Respond(HttpStatusCode.OK,
                $"{{\"id\":\"{lineId}\",\"documentId\":\"{documentId}\",\"itemId\":\"{itemId}\",\"quantity\":2}}");
        var service = CreateService(
            transport,
            "api/v2.0",
            correlationReservationsEnabled: true);

        var response = await service.CreateEntityAsync(new BusinessCentralPurchaseOrderLine
        {
            DataHubCorrelationId = correlationId,
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Quantity = 2m
        });

        response.AsT0.Success.Should().BeTrue();
        transport.Requests.Should().HaveCount(3);
        transport.Requests[0].Uri.AbsolutePath.Should().EndWith("/purchaseDocumentLineReservations");
        transport.Requests[0].Body.Should().Contain($"\"documentId\":\"{documentId}\"");
        transport.Requests[0].Body.Should().Contain($"\"itemId\":\"{itemId}\"");
        var standardCollection = $"/purchaseOrders({documentId})/purchaseOrderLines";
        transport.Requests[1].Uri.AbsolutePath.Should().EndWith(standardCollection);
        transport.Requests[2].Uri.AbsolutePath.Should().EndWith($"{standardCollection}({lineId})");
        transport.Requests[2].Headers["if-match"].Should().ContainSingle()
            .Which.Should().Be("W/\"reserved-line\"");
    }

    [Fact(DisplayName = "Correlation reservations remain disabled unless explicitly opted in")]
    [Trait("Category", "Unit")]
    public async Task CorrelationReservationIsDisabledByDefault()
    {
        var orderId = Guid.NewGuid();
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.Created, $"{{\"id\":\"{orderId}\"}}");
        var service = CreateService(transport, "api/v2.0");

        var response = await service.CreateEntityAsync(new BusinessCentralSalesOrder
        {
            DataHubCorrelationId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid()
        });

        response.AsT0.Success.Should().BeTrue();
        transport.Requests.Should().ContainSingle();
        transport.Requests[0].Uri.AbsolutePath.Should().EndWith("/salesOrders");
    }

    [Fact(DisplayName = "Correlation reservations fail before HTTP when required fields are absent")]
    [Trait("Category", "FaultInjection")]
    public async Task CorrelationReservationRequiresCompleteIdentity()
    {
        var transport = new DeterministicBusinessCentralTransport();
        var service = CreateService(
            transport,
            "api/v2.0",
            correlationReservationsEnabled: true);

        var response = await service.CreateEntityAsync(new BusinessCentralSalesOrder
        {
            DataHubCorrelationId = Guid.NewGuid()
        });

        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Message.Should().Contain("requires 'customerId'");
        transport.Requests.Should().BeEmpty();
    }

    [Fact(DisplayName = "A failed correlation reservation never falls back to an ordinary POST")]
    [Trait("Category", "FaultInjection")]
    public async Task CorrelationReservationFailureDoesNotFallBack()
    {
        var transport = new DeterministicBusinessCentralTransport()
            .Respond(HttpStatusCode.ServiceUnavailable, "{\"error\":{\"message\":\"unavailable\"}}")
            .Respond(HttpStatusCode.OK, "{\"value\":[]}");
        var service = CreateService(
            transport,
            "api/v2.0",
            correlationReservationsEnabled: true);

        var response = await service.CreateEntityAsync(new BusinessCentralSalesOrder
        {
            DataHubCorrelationId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid()
        });

        response.AsT0.Success.Should().BeFalse();
        response.AsT0.Exception.Message.Should().Contain("unique correlation lookup");
        transport.Requests.Should().HaveCount(2);
        transport.Requests.Count(request => request.Method == HttpMethod.Post).Should().Be(1);
        transport.Requests.Should().NotContain(request =>
            request.Method == HttpMethod.Post && request.Uri.AbsolutePath.EndsWith("/salesOrders", StringComparison.Ordinal));
    }

    private static BusinessCentralODataService CreateService(
        HttpMessageHandler handler,
        string? route,
        int maxRetryAttempts = 3,
        bool correlationReservationsEnabled = false)
    {
        var options = new BusinessCentralServiceOptions
        {
            CompanyId = "company-1",
            ApiRoute = route ?? BusinessCentralServiceOptions.DefaultApiRoute,
            CorrelationReservationsEnabled = correlationReservationsEnabled,
            MaxRetryAttempts = maxRetryAttempts,
            RetryBaseDelayMilliseconds = 0
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://businesscentral.example/") };
        return new BusinessCentralODataService(new StaticHttpClientFactory(client), Options.Create(options));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class FallbackDocument : BusinessCentralDocument;

    [BusinessCentralUrl("purchaseOrders")]
    private sealed class UnkeyedDocument : BusinessCentralDocument;

    [BusinessCentralUrl("customers")]
    private sealed class CustomerDocument : BusinessCentralDocument
    {
        public string? Number
        {
            get => GetAttributeValue<string>("number");
            set => SetAttributeValue("number", value);
        }

        public string? DisplayName
        {
            get => GetAttributeValue<string>("displayName");
            set => SetAttributeValue("displayName", value);
        }

        public string? PhoneNumber
        {
            get => GetAttributeValue<string>("phoneNumber");
            set => SetAttributeValue("phoneNumber", value);
        }
    }

    [BusinessCentralApiRoute("api/reimaginate/dataHub/v1.0")]
    [BusinessCentralUrl("generalLedgerEntries")]
    private sealed class CustomRouteDocument : BusinessCentralDocument;

    [BusinessCentralUrl("dimensionSetLines")]
    [BusinessCentralParentUrl("customerPayments", nameof(ParentScopedDocument.ParentId))]
    private sealed class ParentScopedDocument : BusinessCentralDocument
    {
        public Guid? ParentId
        {
            get => GetAttributeValue<Guid?>("parentId");
            set => SetAttributeValue("parentId", value);
        }
    }

    [BusinessCentralUrl("items")]
    private sealed class DecimalDocument : BusinessCentralDocument
    {
        public decimal? UnitPrice
        {
            get => GetAttributeValue<decimal?>("unitPrice");
            set => SetAttributeValue("unitPrice", value);
        }
    }

    [BusinessCentralUrl("customers")]
    [BusinessCentralLastModified("lastModifiedDateTime")]
    private sealed class IncrementalCustomerDocument : BusinessCentralDocument, IBusinessCentralIncrementalEntity
    {
        public DateTimeOffset? LastModifiedAt { get; set; }
    }

    private sealed class TestDataHubEntity : DataHubEntity;
}
