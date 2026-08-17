using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeFailuresToDataHub;

public class SendMergeFailuresToDataHubRequestHandler : IHandler<SendMergeFailuresToDataHubRequest, NullResponse>
{
    private readonly IOptions<BusinessCentralAgentOptions> _businessCentralAgentConfig;
    private readonly IDataHubClient _dataHubClient;
    

    public SendMergeFailuresToDataHubRequestHandler(IOptions<BusinessCentralAgentOptions> businessCentralAgentConfig, IDataHubClient dataHubClient)
    {
        _businessCentralAgentConfig = businessCentralAgentConfig;
        _dataHubClient = dataHubClient;
    }

    public async Task<NullResponse> HandleAsync(SendMergeFailuresToDataHubRequest request, CancellationToken cancellationToken)
    {
        await _dataHubClient.PostRequestAsync<RegisterMergeFailuresRequest, NullResponse>(new RegisterMergeFailuresRequest()
        {
            MergeFailures = request.Failures.Select(s => new MergeFailure()
            {
                DataSource = _businessCentralAgentConfig.Value.DataSource,
                DataHubEntityType = s.DataHubEntityType,
                DataHubEntityId = s.DataHubEntityId,
                SourceEntityType = s.SourceEntityType,
                SourceEntityId = s.SourceEntityId,
                AgentId = _businessCentralAgentConfig.Value.AgentId,
                Description = s.FailureReason,
                Timestamp = DateTimeOffset.UtcNow
            }).ToList()
        }, cancellationToken);

        return new NullResponse();
    }
}
