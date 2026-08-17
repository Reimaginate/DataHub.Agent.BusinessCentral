using FluentAssertions;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncFailuresToDataHub;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;
using Xunit;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class SendSyncFailuresToDataHubRequestHandlerTests
{
    [Fact(DisplayName = "A sync result without a reason is registered with valid diagnostics")]
    public async Task MissingFailureReasonUsesFallback()
    {
        var client = new RecordingDataHubClient();
        var options = Microsoft.Extensions.Options.Options.Create(new BusinessCentralAgentOptions
        {
            AgentId = "configured-agent",
            DataSource = "BusinessCentral"
        });
        var handler = new SendSyncFailuresToDataHubRequestHandler(options, client);

        await handler.HandleAsync(new SendSyncFailuresToDataHubRequest
        {
            Failures =
            [
                new SyncEntityResult
                {
                    SyncOutcome = SyncOutcomes.SyncFailed,
                    DataHubEntityId = "account-123",
                    DataHubEntityType = "Account",
                    SourceEntityType = "Customer"
                }
            ]
        }, CancellationToken.None);

        var request = client.LastRequest.Should().BeOfType<RegisterSyncFailuresRequest>().Subject;
        var failure = request.SyncFailures.Should().ContainSingle().Subject;
        failure.FailureType.Should().Be("BusinessCentralSyncFailed");
        failure.FailureReason.Should().Be("Business Central sync failed without a failure reason.");
        failure.Description.Should().Be(failure.FailureReason);
    }

    private sealed class RecordingDataHubClient : IDataHubClient
    {
        public object? LastRequest { get; private set; }

        public Task<TResponse> PostRequestAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken cancellationToken)
            where TRequest : DataHubClientRequest<TResponse>
            where TResponse : class
        {
            LastRequest = request;
            return Task.FromResult((TResponse)(object)new NullResponse());
        }
    }
}
