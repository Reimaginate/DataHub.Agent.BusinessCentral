using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using BusinessCentralCustomer = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Customer;
using BusinessCentralItem = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.Item;
using BusinessCentralSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoice;
using BusinessCentralSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesInvoiceLine;
using DataHubAccount = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Account;
using DataHubProduct = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.Product;
using DataHubSalesInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoice;
using DataHubSalesInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.SalesInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Mapping;

public sealed class SalesInvoiceMappingTests
{
    [Theory]
    [InlineData(typeof(DataHubSalesInvoice), "SalesInvoice")]
    [InlineData(typeof(DataHubSalesInvoiceLine), "SalesInvoiceLine")]
    public void InvoiceModelsDeclareBusinessCentralRelatedTypes(Type dataHubType, string businessCentralType)
    {
        var attribute = Assert.Single(dataHubType
            .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), inherit: true)
            .Cast<RelatedEntityTypeAttribute>());

        Assert.Equal("BusinessCentral", attribute.DataSource);
        Assert.Equal(businessCentralType, attribute.TypeName);
    }

    [Fact]
    public async Task DataHubInvoiceMapsOnlyEditableDraftFields()
    {
        var customerId = Guid.NewGuid();
        var source = NewInvoice();

        var result = await new MapSalesInvoiceToBusinessCentralSalesInvoice().MapAsync(
            source,
            CancellationToken.None,
            Cache<DataHubAccount>(source.Customer!.EntityId, "customer", customerId));

        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(source.ExternalDocumentNumber, result.ExternalDocumentNumber);
        Assert.Equal(source.InvoiceDate, result.InvoiceDate);
        Assert.Equal(source.PostingDate, result.PostingDate);
        Assert.Equal(source.DueDate, result.DueDate);
        Assert.Null(result.PricesIncludeTax);
        Assert.Null(result.Number);
        Assert.Null(result.Status);
        Assert.Null(result.RemainingAmount);
        Assert.Null(result.TotalAmountIncludingTax);
        Assert.DoesNotContain("number", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", result.GetAttributes().Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BusinessCentralInvoiceMapsCustomerAndCalculatedFields()
    {
        var customerId = Guid.NewGuid();
        var source = new BusinessCentralSalesInvoice
        {
            Id = Guid.NewGuid().ToString(),
            Number = "INV-100",
            ExternalDocumentNumber = "DHIT-INV-100",
            InvoiceDate = "2026-08-14",
            PostingDate = "2026-08-14",
            DueDate = "2026-09-13",
            CustomerId = customerId,
            Status = "Draft",
            PricesIncludeTax = false,
            RemainingAmount = 110m,
            TotalAmountExcludingTax = 100m,
            TotalTaxAmount = 10m,
            TotalAmountIncludingTax = 110m,
            LastModifiedDateTime = new DateTimeOffset(2026, 8, 14, 1, 2, 3, TimeSpan.Zero)
        };

        var result = await new MapBusinessCentralSalesInvoiceToSalesInvoice()
            .MapAsync(source, CancellationToken.None);

        Assert.Equal(source.Number, result.InvoiceNumber);
        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.TotalAmountIncludingTax, result.TotalAmountIncludingTax);
        var customer = Assert.IsType<ExternalEntityReference>(result.Customer);
        Assert.Equal(typeof(DataHubAccount).Name, customer.EntityType);
        Assert.Equal(typeof(BusinessCentralCustomer).Name, customer.SourceEntityType);
        Assert.Equal(customerId.ToString(), customer.EntityId);
    }

    [Fact]
    public async Task InvoiceLineMapsReferencesAndOneDiscountRepresentation()
    {
        var source = NewLine();
        var invoiceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cache = Cache<DataHubSalesInvoice>(source.SalesInvoice!.EntityId, "salesinvoice", invoiceId);
        cache[typeof(DataHubProduct).Name] = CacheObjects(source.Product!.EntityId, "item", itemId);

        var result = await new MapSalesInvoiceLineToBusinessCentralSalesInvoiceLine()
            .MapAsync(source, CancellationToken.None, cache);

        Assert.Equal(invoiceId, result.DocumentId);
        Assert.Equal(itemId, result.ItemId);
        Assert.Equal("Item", result.LineType);
        Assert.Equal(source.DiscountPercent, result.DiscountPercent);
        Assert.DoesNotContain("discountAmount", result.GetAttributes().Keys,
            StringComparer.OrdinalIgnoreCase);
        Assert.Null(result.Sequence);
        Assert.Null(result.AmountIncludingTax);
    }

    [Fact]
    public async Task MissingInvoiceReferencesFailClearly()
    {
        var invoice = NewInvoice();
        invoice.Customer = null;
        var line = NewLine();
        line.Product = null;

        var invoiceFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapSalesInvoiceToBusinessCentralSalesInvoice()
                .MapAsync(invoice, CancellationToken.None));
        var lineFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MapSalesInvoiceLineToBusinessCentralSalesInvoiceLine()
                .MapAsync(line, CancellationToken.None));

        Assert.Contains("reference a customer", invoiceFailure.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("both a sales invoice and a product", lineFailure.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static DataHubSalesInvoice NewInvoice() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        ExternalDocumentNumber = "DHIT-INV-100",
        InvoiceDate = "2026-08-14",
        PostingDate = "2026-08-14",
        DueDate = "2026-09-13",
        Customer = new EntityReference
        {
            EntityType = typeof(DataHubAccount).Name,
            EntityId = "account-1"
        },
        PhoneNumber = "+61 2 9000 0000",
        Email = "invoice@example.test",
        PricesIncludeTax = false
    };

    private static DataHubSalesInvoiceLine NewLine() => new()
    {
        id = Guid.NewGuid().ToString("N"),
        SalesInvoice = new EntityReference
        {
            EntityType = typeof(DataHubSalesInvoice).Name,
            EntityId = "invoice-1"
        },
        Product = new EntityReference
        {
            EntityType = typeof(DataHubProduct).Name,
            EntityId = "product-1"
        },
        Description = "Draft invoice item",
        Description2 = "August",
        Quantity = 2m,
        UnitPrice = 100m,
        DiscountPercent = 5m
    };

    private static Dictionary<string, object> Cache<TDataHubEntity>(
        string dataHubId,
        string businessCentralType,
        Guid businessCentralId)
        where TDataHubEntity : DataHubEntity => new()
        {
            [typeof(TDataHubEntity).Name] = CacheObjects(
                dataHubId,
                businessCentralType,
                businessCentralId)
        };

    private static List<JObject> CacheObjects(
        string dataHubId,
        string businessCentralType,
        Guid businessCentralId) =>
    [
        new JObject
        {
            [nameof(DataHubEntity.id)] = dataHubId,
            [nameof(DataHubEntity.alternateKeys)] = new JArray
            {
                new JObject
                {
                    [nameof(AlternateKey.Key)] = $"businesscentral.{businessCentralType}",
                    [nameof(AlternateKey.Value)] = businessCentralId.ToString()
                }
            }
        }
    ];
}
