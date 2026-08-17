using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncFailuresToDataHub;

public class SendSyncFailuresToDataHubRequestHandler : IHandler<SendSyncFailuresToDataHubRequest, NullResponse>
{
    private readonly IOptions<BusinessCentralAgentOptions> _businessCentralAgentConfig;
    private readonly IDataHubClient _dataHubClient;
    

    public SendSyncFailuresToDataHubRequestHandler(IOptions<BusinessCentralAgentOptions> businessCentralAgentConfig, IDataHubClient dataHubClient)
    {
        _businessCentralAgentConfig = businessCentralAgentConfig;
        _dataHubClient = dataHubClient;
    }

    public async Task<NullResponse> HandleAsync(SendSyncFailuresToDataHubRequest request, CancellationToken cancellationToken)
    {

        var req = new RegisterSyncFailuresRequest()
        {
            SyncFailures = request.Failures.Select(s =>
            {
                var failureReason = string.IsNullOrWhiteSpace(s.FailureReason)
                    ? "Business Central sync failed without a failure reason."
                    : s.FailureReason;
                return new SyncFailure()
                {
                    DataSource = s.DataSource,
                    DataHubEntityType = s.DataHubEntityType,
                    DataHubEntityId = s.DataHubEntityId,
                    SourceEntityType = s.SourceEntityType,
                    SourceEntityId = s.SourceEntityId,
                    AgentId = _businessCentralAgentConfig.Value.AgentId,
                    FailureType = "BusinessCentralSyncFailed",
                    FailureReason = failureReason,
                    Description = failureReason,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }).ToList()
        };

        await _dataHubClient.PostRequestAsync<RegisterSyncFailuresRequest, NullResponse>(req, cancellationToken);

        return new NullResponse();
    }
}
