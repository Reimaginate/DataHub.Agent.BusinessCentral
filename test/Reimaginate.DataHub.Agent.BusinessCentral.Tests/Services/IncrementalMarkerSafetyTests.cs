using FluentAssertions;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class IncrementalMarkerSafetyTests
{
    [Fact(DisplayName = "Inbound marker guard rejects a batch containing a failed customer")]
    [Trait("Category", "FaultInjection")]
    public void FailedInboundRecordBlocksMarkerAdvancement()
    {
        var results = new[]
        {
            new MergeEntityResult
            {
                MergeOutcome = MergeOutcomes.MergeFailed,
                SourceEntityId = "customer-123",
                DataHubEntityId = "account-456",
                FailureReason = "merge validation failed"
            }
        };

        var action = () => IncrementalMarkerSafety.EnsureMergeBatchCanAdvance<TestCustomer>(results);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*source=customer-123*dataHub=account-456*reason=merge validation failed*marker was not advanced*");
    }

    [Fact(DisplayName = "Outbound marker guard rejects a batch containing a failed account")]
    [Trait("Category", "FaultInjection")]
    public void FailedOutboundRecordBlocksMarkerAdvancement()
    {
        var results = new[]
        {
            new SyncEntityResult
            {
                SyncOutcome = SyncOutcomes.SyncFailed,
                DataHubEntityId = "account-123",
                SourceEntityId = "customer-456",
                FailureReason = "customer update failed"
            }
        };

        var action = () => IncrementalMarkerSafety.EnsureSyncBatchCanAdvance<TestAccount>(results);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*dataHub=account-123*source=customer-456*reason=customer update failed*marker was not advanced*");
    }

    private sealed class TestCustomer;
    private sealed class TestAccount : DataHubEntity;
}
