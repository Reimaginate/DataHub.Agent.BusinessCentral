using System.Net;
using FluentAssertions;
using Reimaginate.DataHub.Agent.BusinessCentral.CustomExceptions;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;
using Xunit;
using BusinessCentralSalesCreditMemo = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemo;
using BusinessCentralSalesCreditMemoLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.SalesCreditMemoLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class BusinessCentralSalesCreditMemoCleanupFlowTests
{
    [Fact(DisplayName = "Deliberate sales-credit-memo aggregate deletion requires exact identity")]
    [Trait("Category", "Unit")]
    public void DeliberateAggregateDeleteApiRequiresExactExternalDocumentNumber()
    {
        var overloads = typeof(BusinessCentralIntegrationTestHost)
            .GetMethods()
            .Where(method => method.Name == nameof(
                BusinessCentralIntegrationTestHost.DeleteSalesCreditMemoAggregateAsync))
            .ToList();

        overloads.Should().ContainSingle();
        overloads.Single().GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(Guid), typeof(string), typeof(CancellationToken));
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup performs no delete for an unsafe paid credit memo")]
    [Trait("Category", "Unit")]
    public async Task UnsafePaidCreditMemoPerformsNoDelete()
    {
        var creditMemo = CreditMemo("Paid", "W/\"ETAG-B\"");
        var unsafeLine = PlaceholderLine(creditMemo);
        unsafeLine.Description = "Not the canonical no-series placeholder";
        var deletes = new List<BusinessCentralSalesCreditMemo>();

        var action = () => DeleteCurrentAsync(
            creditMemo,
            new BusinessCentralSalesCreditMemoLineSnapshot(1, [unsafeLine]),
            deletes,
            allowDraftDelete: false);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not the exact zero-value*");
        deletes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup deletes a draft once with its exact ETag")]
    [Trait("Category", "Unit")]
    public async Task DraftUsesExactETag()
    {
        var creditMemo = CreditMemo("Draft", "W/\"ETAG-A\"");
        var deletes = new List<BusinessCentralSalesCreditMemo>();

        var result = await BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
            (_, _) => throw new InvalidOperationException("Draft cleanup must not read posted lines."),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            });

        result.Disposition.Should().Be(BusinessCentralSalesCreditMemoCleanupDisposition.DraftDeleted);
        deletes.Should().ContainSingle().Which.ETag.Should().Be("W/\"ETAG-A\"");
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup deletes a safe placeholder once with its exact ETag")]
    [Trait("Category", "Unit")]
    public async Task SafePlaceholderUsesExactETag()
    {
        var creditMemo = CreditMemo("Paid", "W/\"ETAG-B\"");
        var line = PlaceholderLine(creditMemo);
        var deletes = new List<BusinessCentralSalesCreditMemo>();

        var result = await DeleteCurrentAsync(
            creditMemo,
            new BusinessCentralSalesCreditMemoLineSnapshot(1, [line]),
            deletes,
            allowDraftDelete: false);

        result.Disposition.Should().Be(
            BusinessCentralSalesCreditMemoCleanupDisposition.NoSeriesPlaceholderDeleted);
        result.CapturedLines.Should().ContainSingle().Which.Should().BeSameAs(line);
        deletes.Should().ContainSingle().Which.ETag.Should().Be("W/\"ETAG-B\"");
    }

    [Fact(DisplayName = "Sales-credit-memo normal cleanup never deletes an already-paid record")]
    [Trait("Category", "Unit")]
    public async Task InitiallyPaidRecordPerformsNoDelete()
    {
        var creditMemo = CreditMemo("Paid", "W/\"ETAG-B\"");
        var deletes = new List<BusinessCentralSalesCreditMemo>();

        var action = () => DeleteCurrentAsync(
            creditMemo,
            new BusinessCentralSalesCreditMemoLineSnapshot(1, [PlaceholderLine(creditMemo)]),
            deletes,
            allowDraftDelete: true);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already Paid when normal cleanup began*");
        deletes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup refuses a missing ETag without deleting")]
    [Trait("Category", "Unit")]
    public async Task MissingETagPerformsNoDelete()
    {
        var creditMemo = CreditMemo("Draft", null);
        var deleteCount = 0;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
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

    [Fact(DisplayName = "Sales-credit-memo cleanup refuses a wildcard ETag without deleting")]
    [Trait("Category", "Unit")]
    public async Task WildcardETagPerformsNoDelete()
    {
        var creditMemo = CreditMemo("Draft", "*");
        var deleteCount = 0;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
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

    [Fact(DisplayName = "Sales-credit-memo cleanup does not retry a stale exact ETag")]
    [Trait("Category", "Unit")]
    public async Task StaleETagFailsWithoutRetry()
    {
        var creditMemo = CreditMemo("Draft", "W/\"ETAG-A\"");
        var deleteCount = 0;
        string? attemptedETag = null;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
            (_, _) => throw new InvalidOperationException("Lines must not be read."),
            (entity, _) =>
            {
                deleteCount++;
                attemptedETag = entity.ETag;
                throw new BusinessCentralHttpException(
                    HttpStatusCode.PreconditionFailed,
                    "delete sales credit memo",
                    "The ETag no longer matches.");
            });

        await action.Should().ThrowAsync<BusinessCentralHttpException>();
        deleteCount.Should().Be(1);
        attemptedETag.Should().Be("W/\"ETAG-A\"");
    }

    [Fact(DisplayName = "Sales-credit-memo placeholder phase never retries a draft delete")]
    [Trait("Category", "Unit")]
    public async Task PlaceholderPhaseRefusesDraftRetry()
    {
        var creditMemo = CreditMemo("Draft", "W/\"ETAG-C\"");
        var deleteCount = 0;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
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

    [Fact(DisplayName = "Sales-credit-memo cleanup rejects an incomplete destructive line read")]
    [Trait("Category", "Unit")]
    public async Task IncompleteLineSetPerformsNoDelete()
    {
        var creditMemo = CreditMemo("Paid", "W/\"ETAG-B\"");
        var line = PlaceholderLine(creditMemo);
        var deletes = new List<BusinessCentralSalesCreditMemo>();

        var action = () => DeleteCurrentAsync(
            creditMemo,
            new BusinessCentralSalesCreditMemoLineSnapshot(2, [line]),
            deletes,
            allowDraftDelete: false);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*incomplete line set*");
        deletes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup requires the exact expected external document number")]
    [Trait("Category", "Unit")]
    public async Task ChangedExternalDocumentNumberPerformsNoDelete()
    {
        var creditMemo = CreditMemo("Paid", "W/\"ETAG-B\"");
        var deletes = new List<BusinessCentralSalesCreditMemo>();

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
            (_, _) => Task.FromResult(
                new BusinessCentralSalesCreditMemoLineSnapshot(1, [PlaceholderLine(creditMemo)])),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            },
            allowDraftDelete: false,
            expectedExternalDocumentNumber: "DHIT-DIFFERENT");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exact DHIT external document number*");
        deletes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup carries captured blank identity through one transition")]
    [Trait("Category", "Unit")]
    public async Task CapturedBlankDraftAuthorizesSameGuidPlaceholderOnce()
    {
        var creditMemoId = Guid.NewGuid();
        var draft = CreditMemo("Draft", "W/\"ETAG-A\"");
        draft.Id = creditMemoId.ToString();
        draft.ExternalDocumentNumber = string.Empty;
        var deletes = new List<BusinessCentralSalesCreditMemo>();

        var draftResult = await BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            creditMemoId,
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(draft),
            (_, _) => throw new InvalidOperationException("Draft cleanup must not read posted lines."),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            },
            expectedExternalDocumentNumber: string.Empty,
            allowCapturedBlankDraft: true);

        draftResult.Disposition.Should().Be(
            BusinessCentralSalesCreditMemoCleanupDisposition.DraftDeleted);
        draftResult.TransitionProvenance.Should().NotBeNull();

        var placeholder = CreditMemo("Paid", "W/\"ETAG-B\"");
        placeholder.Id = creditMemoId.ToString();
        placeholder.ExternalDocumentNumber = string.Empty;
        var placeholderLine = PlaceholderLine(placeholder);

        var placeholderResult = await BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            creditMemoId,
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(placeholder),
            (_, _) => Task.FromResult(
                new BusinessCentralSalesCreditMemoLineSnapshot(1, [placeholderLine])),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            },
            allowDraftDelete: false,
            expectedExternalDocumentNumber: string.Empty,
            transitionProvenance: draftResult.TransitionProvenance);

        placeholderResult.Disposition.Should().Be(
            BusinessCentralSalesCreditMemoCleanupDisposition.NoSeriesPlaceholderDeleted);
        deletes.Should().HaveCount(2);

        var retry = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            creditMemoId,
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(placeholder),
            (_, _) => Task.FromResult(
                new BusinessCentralSalesCreditMemoLineSnapshot(1, [placeholderLine])),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            },
            allowDraftDelete: false,
            expectedExternalDocumentNumber: string.Empty,
            transitionProvenance: draftResult.TransitionProvenance);

        await retry.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*one-shot*already used*");
        deletes.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup refuses an uncaptured blank draft")]
    [Trait("Category", "Unit")]
    public async Task UncapturedBlankDraftPerformsNoDelete()
    {
        var draft = CreditMemo("Draft", "W/\"ETAG-A\"");
        draft.ExternalDocumentNumber = string.Empty;
        var deleteCount = 0;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(draft.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(draft),
            (_, _) => throw new InvalidOperationException("Lines must not be read."),
            (_, _) =>
            {
                deleteCount++;
                return Task.CompletedTask;
            });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no captured-blank draft provenance authorized it*");
        deleteCount.Should().Be(0);
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup refuses a standalone paid blank placeholder")]
    [Trait("Category", "Unit")]
    public async Task StandalonePaidBlankPlaceholderPerformsNoDelete()
    {
        var placeholder = CreditMemo("Paid", "W/\"ETAG-B\"");
        placeholder.ExternalDocumentNumber = string.Empty;
        var deleteCount = 0;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(placeholder.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(placeholder),
            (_, _) => Task.FromResult(
                new BusinessCentralSalesCreditMemoLineSnapshot(1, [PlaceholderLine(placeholder)])),
            (_, _) =>
            {
                deleteCount++;
                return Task.CompletedTask;
            },
            allowDraftDelete: false,
            expectedExternalDocumentNumber: string.Empty);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*blank external document number and no same-operation Draft-delete provenance*");
        deleteCount.Should().Be(0);
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup refuses a response for a different id")]
    [Trait("Category", "Unit")]
    public async Task DifferentCurrentIdPerformsNoDelete()
    {
        var requestedId = Guid.NewGuid();
        var creditMemo = CreditMemo("Draft", "W/\"ETAG-A\"");
        var deleteCount = 0;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            requestedId,
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
            (_, _) => throw new InvalidOperationException("Lines must not be read."),
            (_, _) =>
            {
                deleteCount++;
                return Task.CompletedTask;
            });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*cleanup requested '{requestedId}'*");
        deleteCount.Should().Be(0);
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup refuses a non-DHIT identity")]
    [Trait("Category", "Unit")]
    public async Task NonTestIdentityPerformsNoDelete()
    {
        var creditMemo = CreditMemo("Draft", "W/\"ETAG-A\"");
        creditMemo.ExternalDocumentNumber = "CUSTOMER-CREDIT-1";
        var deleteCount = 0;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
            (_, _) => throw new InvalidOperationException("Lines must not be read."),
            (_, _) =>
            {
                deleteCount++;
                return Task.CompletedTask;
            });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a DHIT test artifact*");
        deleteCount.Should().Be(0);
    }

    [Fact(DisplayName = "Sales-credit-memo cleanup refuses every non-Draft non-Paid status")]
    [Trait("Category", "Unit")]
    public async Task UnsafeStatusPerformsNoDelete()
    {
        var creditMemo = CreditMemo("Open", "W/\"ETAG-A\"");
        var deleteCount = 0;

        var action = () => BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
            (_, _) => throw new InvalidOperationException("Lines must not be read."),
            (_, _) =>
            {
                deleteCount++;
                return Task.CompletedTask;
            });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cleanup-unsafe status 'Open'*");
        deleteCount.Should().Be(0);
    }

    private static Task<BusinessCentralSalesCreditMemoCleanupResult> DeleteCurrentAsync(
        BusinessCentralSalesCreditMemo creditMemo,
        BusinessCentralSalesCreditMemoLineSnapshot snapshot,
        ICollection<BusinessCentralSalesCreditMemo> deletes,
        bool allowDraftDelete) =>
        BusinessCentralSalesCreditMemoCleanupFlow.DeleteCurrentAsync(
            Guid.Parse(creditMemo.Id!),
            (_, _) => Task.FromResult<BusinessCentralSalesCreditMemo?>(creditMemo),
            (_, _) => Task.FromResult(snapshot),
            (entity, _) =>
            {
                deletes.Add(entity);
                return Task.CompletedTask;
            },
            allowDraftDelete,
            expectedExternalDocumentNumber: creditMemo.ExternalDocumentNumber);

    private static BusinessCentralSalesCreditMemo CreditMemo(string status, string? etag) => new()
    {
        Id = Guid.NewGuid().ToString(),
        ETag = etag,
        ExternalDocumentNumber = "DHIT-SCM-FLOW",
        Status = status,
        DiscountAmount = 0m,
        TotalAmountExcludingTax = 0m,
        TotalTaxAmount = 0m,
        TotalAmountIncludingTax = 0m
    };

    private static BusinessCentralSalesCreditMemoLine PlaceholderLine(
        BusinessCentralSalesCreditMemo creditMemo) => new()
    {
        Id = Guid.NewGuid().ToString(),
        DocumentId = Guid.Parse(creditMemo.Id!),
        Sequence = 10000,
        LineType = "Comment",
        Description = BusinessCentralSalesCreditMemoCleanupPolicy.NoSeriesPlaceholderDescription,
        Description2 = string.Empty,
        LineObjectNumber = string.Empty,
        ItemId = Guid.Empty,
        Quantity = 0m,
        UnitPrice = 0m,
        DiscountAmount = 0m,
        DiscountPercent = 0m,
        AmountExcludingTax = 0m,
        TaxPercent = 0m,
        TotalTaxAmount = 0m,
        AmountIncludingTax = 0m,
        ShipmentDate = "0001-01-01"
    };
}
