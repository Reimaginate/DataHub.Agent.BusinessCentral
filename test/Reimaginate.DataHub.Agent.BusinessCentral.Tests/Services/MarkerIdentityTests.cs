using FluentAssertions;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralMergeMarker;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;
using Reimaginate.DataHub.SharedModels.Markers;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Xunit;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class MarkerIdentityTests
{
    [Fact(DisplayName = "Outbound markers use the configured agent id")]
    public async Task OutboundMarkerUsesAgentId()
    {
        var client = new RecordingDataHubClient();
        var handler = new GetBusinessCentralSyncMarkerRequestHandler(Options(), client);

        await handler.HandleAsync(new GetBusinessCentralSyncMarkerRequest
        {
            EntityType = "Account",
            DefaultValue = DateTimeOffset.MinValue.ToString("o")
        }, CancellationToken.None);

        var request = client.LastRequest.Should().BeOfType<GetSyncMarkerRequest>().Subject;
        request.AgentId.Should().Be("configured-agent");
        request.DataSource.Should().Be("BusinessCentral");
    }

    [Fact(DisplayName = "Inbound markers use the configured agent id")]
    public async Task InboundMarkerUsesAgentId()
    {
        var client = new RecordingDataHubClient();
        var handler = new GetBusinessCentralMergeMarkerRequestHandler(Options(), client);

        await handler.HandleAsync(new GetBusinessCentralMergeMarkerRequest
        {
            EntityType = "Customer",
            DefaultValue = DateTimeOffset.MinValue.ToString("o")
        }, CancellationToken.None);

        var request = client.LastRequest.Should().BeOfType<GetMergeMarkerRequest>().Subject;
        request.AgentId.Should().Be("configured-agent");
        request.DataSource.Should().Be("BusinessCentral");
    }

    private static IOptions<BusinessCentralAgentOptions> Options() =>
        Microsoft.Extensions.Options.Options.Create(new BusinessCentralAgentOptions
        {
            AgentId = "configured-agent",
            DataSource = "BusinessCentral"
        });

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
            object response = typeof(TResponse) == typeof(GetSyncMarkerResponse)
                ? new GetSyncMarkerResponse { Success = true, SyncMarker = new SyncMarker() }
                : new GetMergeMarkerResponse { Success = true, MergeMarker = new MergeMarker() };
            return Task.FromResult((TResponse)response);
        }
    }
}
