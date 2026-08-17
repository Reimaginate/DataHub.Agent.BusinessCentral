using System.Net;
using FluentAssertions;
using Reimaginate.DataHub.Agent.BusinessCentral.CustomExceptions;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Xunit;
using BusinessCentralPurchaseInvoice = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoice;
using BusinessCentralPurchaseInvoiceLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseInvoiceLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralPurchaseInvoiceCleanupFlowTests
{
    [Fact(DisplayName = "Purchase-invoice cleanup performs no delete for an unsafe paid invoice")]
    [Trait("Category", "Unit")]
    public async Task UnsafePaidInvoicePerformsNoDelete()
    {
        var invoice = Invoice("Paid", "W/\"ETAG-B\"");
        var unsafeLine = PlaceholderLine(invoice);
        unsafeLine.Description = "Not the canonical no-series placeholder";
        var deletes = new List<BusinessCentralPurchaseInvoice>();

        var action = () => BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(invoice.Id!),
            (_, _) => Task.FromResult<BusinessCentralPurchaseInvoice?>(invoice),
            (_, _) => Task.FromResult(new BusinessCentralPurchaseInvoiceLineSnapshot(1, [unsafeLine])),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not the exact zero-value*");
        deletes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Purchase-invoice cleanup deletes a draft once with its exact ETag")]
    [Trait("Category", "Unit")]
    public async Task DraftUsesExactETag()
    {
        var invoice = Invoice("Draft", "W/\"ETAG-A\"");
        var deletes = new List<BusinessCentralPurchaseInvoice>();

        var result = await BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(invoice.Id!),
            (_, _) => Task.FromResult<BusinessCentralPurchaseInvoice?>(invoice),
            (_, _) => throw new InvalidOperationException("Draft cleanup must not read posted lines."),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            });

        result.Disposition.Should().Be(BusinessCentralPurchaseInvoiceCleanupDisposition.DraftDeleted);
        deletes.Should().ContainSingle().Which.ETag.Should().Be("W/\"ETAG-A\"");
    }

    [Fact(DisplayName = "Purchase-invoice cleanup deletes the safe placeholder once with its exact ETag")]
    [Trait("Category", "Unit")]
    public async Task SafePlaceholderUsesExactETag()
    {
        var invoice = Invoice("Paid", "W/\"ETAG-B\"");
        var line = PlaceholderLine(invoice);
        var deletes = new List<BusinessCentralPurchaseInvoice>();

        var result = await BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(invoice.Id!),
            (_, _) => Task.FromResult<BusinessCentralPurchaseInvoice?>(invoice),
            (_, _) => Task.FromResult(new BusinessCentralPurchaseInvoiceLineSnapshot(1, [line])),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            });

        result.Disposition.Should().Be(
            BusinessCentralPurchaseInvoiceCleanupDisposition.NoSeriesPlaceholderDeleted);
        result.CapturedLines.Should().ContainSingle().Which.Should().BeSameAs(line);
        deletes.Should().ContainSingle().Which.ETag.Should().Be("W/\"ETAG-B\"");
    }

    [Fact(DisplayName = "Purchase-invoice cleanup refuses a missing ETag without deleting")]
    [Trait("Category", "Unit")]
    public async Task MissingETagPerformsNoDelete()
    {
        var invoice = Invoice("Draft", null);
        var deleteCount = 0;

        var action = () => BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(invoice.Id!),
            (_, _) => Task.FromResult<BusinessCentralPurchaseInvoice?>(invoice),
            (_, _) => throw new InvalidOperationException("Lines must not be read."),
            (_, _) =>
            {
                deleteCount++;
                return Task.CompletedTask;
            });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*will not use a wildcard If-Match*");
        deleteCount.Should().Be(0);
    }

    [Fact(DisplayName = "Purchase-invoice cleanup does not retry a stale exact ETag")]
    [Trait("Category", "Unit")]
    public async Task StaleETagFailsWithoutRetry()
    {
        var invoice = Invoice("Draft", "W/\"ETAG-A\"");
        var deleteCount = 0;
        string? attemptedETag = null;

        var action = () => BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(invoice.Id!),
            (_, _) => Task.FromResult<BusinessCentralPurchaseInvoice?>(invoice),
            (_, _) => throw new InvalidOperationException("Lines must not be read."),
            (entity, _) =>
            {
                deleteCount++;
                attemptedETag = entity.ETag;
                throw new BusinessCentralHttpException(
                    HttpStatusCode.PreconditionFailed,
                    "delete purchase invoice",
                    "The ETag no longer matches.");
            });

        await action.Should().ThrowAsync<BusinessCentralHttpException>();
        deleteCount.Should().Be(1);
        attemptedETag.Should().Be("W/\"ETAG-A\"");
    }

    [Fact(DisplayName = "Purchase-invoice placeholder phase never retries a draft delete")]
    [Trait("Category", "Unit")]
    public async Task PlaceholderPhaseRefusesDraftRetry()
    {
        var invoice = Invoice("Draft", "W/\"ETAG-C\"");
        var deleteCount = 0;

        var action = () => BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(invoice.Id!),
            (_, _) => Task.FromResult<BusinessCentralPurchaseInvoice?>(invoice),
            (_, _) => throw new InvalidOperationException("Lines must not be read."),
            (_, _) =>
            {
                deleteCount++;
                return Task.CompletedTask;
            },
            allowDraftDelete: false);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*will not retry the draft DELETE*");
        deleteCount.Should().Be(0);
    }

    [Fact(DisplayName = "Purchase-invoice cleanup rejects an incomplete destructive line read")]
    [Trait("Category", "Unit")]
    public async Task IncompleteLineSetPerformsNoDelete()
    {
        var invoice = Invoice("Paid", "W/\"ETAG-B\"");
        var line = PlaceholderLine(invoice);
        var deleteCount = 0;

        var action = () => BusinessCentralPurchaseInvoiceCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(invoice.Id!),
            (_, _) => Task.FromResult<BusinessCentralPurchaseInvoice?>(invoice),
            (_, _) => Task.FromResult(new BusinessCentralPurchaseInvoiceLineSnapshot(2, [line])),
            (_, _) =>
            {
                deleteCount++;
                return Task.CompletedTask;
            });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*incomplete line set*");
        deleteCount.Should().Be(0);
    }

    private static BusinessCentralPurchaseInvoice Invoice(string status, string? etag) => new()
    {
        Id = Guid.NewGuid().ToString(),
        ETag = etag,
        VendorInvoiceNumber = "DHIT-PI-FLOW",
        Status = status,
        DiscountAmount = 0m,
        TotalAmountExcludingTax = 0m,
        TotalTaxAmount = 0m,
        TotalAmountIncludingTax = 0m
    };

    private static BusinessCentralPurchaseInvoiceLine PlaceholderLine(
        BusinessCentralPurchaseInvoice invoice) => new()
    {
        Id = Guid.NewGuid().ToString(),
        DocumentId = Guid.Parse(invoice.Id!),
        LineType = "Comment",
        Description = BusinessCentralPurchaseInvoiceCleanupPolicy.NoSeriesPlaceholderDescription,
        Description2 = string.Empty,
        LineObjectNumber = string.Empty,
        ItemId = Guid.Empty,
        Quantity = 0m,
        UnitCost = 0m,
        DiscountAmount = 0m,
        DiscountPercent = 0m,
        AmountExcludingTax = 0m,
        TaxPercent = 0m,
        TotalTaxAmount = 0m,
        AmountIncludingTax = 0m
    };
}
