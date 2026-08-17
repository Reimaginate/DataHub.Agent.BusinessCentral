using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Mapping;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.SharedModels.Core;
using BCCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.Customer;
using BCItem = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.Item;
using BCSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrder;
using BCSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrderLine;
using DHAccount = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.Account;
using DHProduct = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.Product;
using DHSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrder;
using DHSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Tests;

public sealed class ReferenceImplementationTests
{
    private static readonly Guid CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void ConfigurationValidationRejectsPlaceholdersAndAcceptsStandardRoute()
    {
        var invalid = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:BaseUrl"] =
                "https://api.businesscentral.dynamics.com/v2.0/YOUR-TENANT-ID/YOUR-ENVIRONMENT/",
            ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:CompanyId"] = "YOUR-COMPANY-ID",
            ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:ApiRoute"] = "api/v2.0"
        }).Build();
        Assert.Equal(2, ReferenceConfiguration.Validate(invalid).Count());

        var valid = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:BaseUrl"] =
                "https://api.businesscentral.dynamics.com/v2.0/tenant/Sandbox/",
            ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:CompanyId"] = CompanyId.ToString(),
            ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:ApiRoute"] = "api/v2.0"
        }).Build();
        Assert.Empty(ReferenceConfiguration.Validate(valid));
    }

    [Fact]
    public async Task InboundMergeAndOutboundRoundTripPreserveOwnedCustomerFields()
    {
        var id = Guid.NewGuid();
        var modified = DateTimeOffset.Parse("2026-08-17T00:00:00Z");
        var source = new BCCustomer
        {
            Id = id.ToString(),
            Number = "C-100",
            DisplayName = "Reference customer",
            Email = "customer@example.test",
            PhoneNumber = "+61 2 5550 0100",
            LastModifiedDateTime = modified
        };

        var account = await new MapCustomerToAccount().MapAsync(source, CancellationToken.None);
        var roundTrip = await new MapAccountToCustomer().MapAsync(account, CancellationToken.None);

        Assert.Equal(id.ToString(), account.id);
        Assert.Equal(modified, account.lastUpdated);
        Assert.Equal(source.Number, roundTrip.Number);
        Assert.Equal(source.DisplayName, roundTrip.DisplayName);
        Assert.Equal(source.Email, roundTrip.Email);
        Assert.Equal(source.PhoneNumber, roundTrip.PhoneNumber);
    }

    [Fact]
    public async Task ParentAndProductReferencesResolveForSalesOrderLine()
    {
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var line = new DHSalesOrderLine
        {
            id = "line-1",
            SalesOrder = External<DHSalesOrder, BCSalesOrder>(orderId),
            Product = External<DHProduct, BCItem>(itemId),
            Description = "Consulting",
            Quantity = 2,
            UnitPrice = 125
        };

        var mapped = await new MapSalesOrderLineToBusinessCentral()
            .MapAsync(line, CancellationToken.None);

        Assert.Equal(orderId, mapped.DocumentId);
        Assert.Equal(itemId, mapped.ItemId);
        Assert.Equal("Item", mapped.LineType);
        Assert.NotEqual(Guid.Empty, mapped.DataHubCorrelationId);
    }

    [Fact]
    public async Task CreateRetriesRateLimitAndUsesStandardCompanyRoute()
    {
        var customerId = Guid.NewGuid();
        var handler = new QueueHandler(
            _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            request => JsonResponse(HttpStatusCode.Created,
                $$"""{"id":"{{customerId}}","number":"C-1","displayName":"Created"}"""));
        var service = CreateService(handler, maxRetryAttempts: 2);

        var result = await service.CreateEntityAsync(new BCCustomer
        {
            Number = "C-1",
            DisplayName = "Created"
        });

        Assert.True(result.IsT0);
        Assert.Equal(2, handler.Requests.Count(request => request.Method == HttpMethod.Post));
        Assert.Equal(2, handler.Requests.Count(request => request.Method == HttpMethod.Get));
        Assert.All(handler.Requests, request => Assert.StartsWith(
            $"https://example.test/api/v2.0/companies({CompanyId})/customers",
            request.RequestUri!.ToString(),
            StringComparison.Ordinal));
    }

    [Fact]
    public async Task LineCreateCarriesParentAndItemWithoutLifecycleAction()
    {
        var lineId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var handler = new QueueHandler(request => JsonResponse(HttpStatusCode.Created,
            $$"""{"id":"{{lineId}}","documentId":"{{documentId}}","itemId":"{{itemId}}","lineType":"Item"}"""));
        var service = CreateService(handler);

        var result = await service.CreateEntityAsync(new BCSalesOrderLine
        {
            DataHubCorrelationId = Guid.NewGuid(),
            DocumentId = documentId,
            ItemId = itemId,
            LineType = "Item",
            Quantity = 1
        });

        Assert.True(result.IsT0);
        var request = Assert.Single(handler.Requests);
        Assert.Equal($"https://example.test/api/v2.0/companies({CompanyId})/salesOrderLines", request.RequestUri!.ToString());
        Assert.DoesNotContain("post", request.RequestUri.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(documentId.ToString(), await request.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task ConcurrentPatchConflictFailsWithoutWildcardOrSilentOverwrite()
    {
        const string etag = "W/\"expected\"";
        var id = Guid.NewGuid();
        var handler = new QueueHandler(_ => JsonResponse(
            HttpStatusCode.PreconditionFailed,
            "{\"error\":{\"code\":\"Request_EntityChanged\"}}"));
        var service = CreateService(handler);
        var customer = new BCCustomer
        {
            Id = id.ToString(),
            ETag = etag,
            DisplayName = "Changed"
        };

        var result = await service.UpdateEntityAsync(customer, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.PreconditionFailed, result.StatusCode);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(etag, Assert.Single(request.Headers.IfMatch).ToString());
    }

    private static IBusinessCentralODataService CreateService(QueueHandler handler, int maxRetryAttempts = 1)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        return new BusinessCentralODataService(
            new SingleClientFactory(client),
            Options.Create(new BusinessCentralServiceOptions
            {
                BaseUrl = client.BaseAddress.ToString(),
                CompanyId = CompanyId.ToString(),
                ApiRoute = "api/v2.0",
                MaxRetryAttempts = maxRetryAttempts,
                RetryBaseDelayMilliseconds = 0
            }));
    }

    private static ExternalEntityReference External<TDataHub, TBusinessCentral>(Guid id) => new()
    {
        DataSource = "BusinessCentral",
        EntityType = typeof(TDataHub).Name,
        SourceEntityType = typeof(TBusinessCentral).Name,
        EntityId = id.ToString()
    };

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class QueueHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new(responses);
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = request.Content is null
                    ? null
                    : new StringContent(request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult())
            };
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Requests.Add(clone);
            return Task.FromResult(_responses.Dequeue()(request));
        }
    }
}
