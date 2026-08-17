using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Xunit;
using BusinessCentralPurchaseCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemo;
using BusinessCentralPurchaseCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.PurchaseCreditMemoLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralPurchaseCreditMemoCleanupTests
{
    [Fact]
    public void ExactCanonicalPaidPlaceholderIsAccepted()
    {
        var (header, lines) = Placeholder();

        Assert.True(BusinessCentralPurchaseCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(header, lines));
    }

    [Fact]
    public void NonTestNonZeroOrUnexpectedLinePlaceholdersAreRejected()
    {
        var (header, lines) = Placeholder();
        header.VendorCreditMemoNumber = "REAL-PCM-1";
        Assert.False(BusinessCentralPurchaseCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(header, lines));

        (header, lines) = Placeholder();
        header.TotalAmountIncludingTax = 1m;
        Assert.False(BusinessCentralPurchaseCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(header, lines));

        (header, lines) = Placeholder();
        lines[0].LineType = "Item";
        Assert.False(BusinessCentralPurchaseCreditMemoCleanupPolicy.IsSafeNoSeriesPlaceholder(header, lines));
    }

    [Fact]
    public async Task DraftUsesExactEntityOnceAndPaidRequiresCompleteValidatedLines()
    {
        var id = Guid.NewGuid();
        var draft = new BusinessCentralPurchaseCreditMemo
        {
            Id = id.ToString(),
            ETag = "draft-etag",
            VendorCreditMemoNumber = "DHIT-PCM-1",
            Status = "Draft"
        };
        var deleted = new List<string?>();

        var result = await BusinessCentralPurchaseCreditMemoCleanupFlow.DeleteCurrentAsync(
            id,
            (_, _) => Task.FromResult<BusinessCentralPurchaseCreditMemo?>(draft),
            (_, _) => throw new InvalidOperationException("Draft cleanup must not read placeholder lines."),
            (entity, _) => { deleted.Add(entity.ETag); return Task.CompletedTask; });

        Assert.Equal(BusinessCentralPurchaseCreditMemoCleanupDisposition.DraftDeleted, result.Disposition);
        Assert.Equal(["draft-etag"], deleted);

        var (paid, lines) = Placeholder(id);
        var paidResult = await BusinessCentralPurchaseCreditMemoCleanupFlow.DeleteCurrentAsync(
            id,
            (_, _) => Task.FromResult<BusinessCentralPurchaseCreditMemo?>(paid),
            (_, _) => Task.FromResult(new BusinessCentralPurchaseCreditMemoLineSnapshot(lines.Count, lines)),
            (entity, _) => { deleted.Add(entity.ETag); return Task.CompletedTask; },
            allowDraftDelete: false);

        Assert.Equal(BusinessCentralPurchaseCreditMemoCleanupDisposition.NoSeriesPlaceholderDeleted, paidResult.Disposition);
        Assert.Equal("paid-etag", deleted[^1]);
    }

    [Fact]
    public async Task InitiallyPaidUnsafeOrIncompleteAggregatePerformsNoDelete()
    {
        var id = Guid.NewGuid();
        var (header, lines) = Placeholder(id);
        var deletes = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BusinessCentralPurchaseCreditMemoCleanupFlow.DeleteCurrentAsync(
                id,
                (_, _) => Task.FromResult<BusinessCentralPurchaseCreditMemo?>(header),
                (_, _) => Task.FromResult(new BusinessCentralPurchaseCreditMemoLineSnapshot(lines.Count + 1, lines)),
                (_, _) => { deletes++; return Task.CompletedTask; },
                allowDraftDelete: false));

        Assert.Equal(0, deletes);
    }

    private static (BusinessCentralPurchaseCreditMemo Header, List<BusinessCentralPurchaseCreditMemoLine> Lines)
        Placeholder(Guid? requestedId = null)
    {
        var id = requestedId ?? Guid.NewGuid();
        return (
            new BusinessCentralPurchaseCreditMemo
            {
                Id = id.ToString(),
                ETag = "paid-etag",
                VendorCreditMemoNumber = "DHIT-PCM-PLACEHOLDER",
                Status = "Paid",
                DiscountAmount = 0m,
                TotalAmountExcludingTax = 0m,
                TotalTaxAmount = 0m,
                TotalAmountIncludingTax = 0m
            },
            [
                new BusinessCentralPurchaseCreditMemoLine
                {
                    Id = Guid.NewGuid().ToString(),
                    DocumentId = id,
                    LineType = "Comment",
                    Description = BusinessCentralPurchaseCreditMemoCleanupPolicy.NoSeriesPlaceholderDescription,
                    ItemId = Guid.Empty,
                    LineObjectNumber = string.Empty,
                    Quantity = 0m,
                    UnitCost = 0m,
                    DiscountAmount = 0m,
                    DiscountPercent = 0m,
                    AmountExcludingTax = 0m,
                    TaxPercent = 0m,
                    TotalTaxAmount = 0m,
                    AmountIncludingTax = 0m
                }
            ]);
    }
}
