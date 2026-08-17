using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendSyncSuccessesToDataHub;

public class SendSyncSuccessesToDataHubRequestHandler : IHandler<SendSyncSuccessesToDataHubRequest, NullResponse>
{
    private readonly IOptions<BusinessCentralAgentOptions> _businessCentralAgentConfig;
    private readonly IDataHubClient _dataHubClient;
    

    public SendSyncSuccessesToDataHubRequestHandler(IOptions<BusinessCentralAgentOptions> businessCentralAgentConfig, IDataHubClient dataHubClient)
    {
        _businessCentralAgentConfig = businessCentralAgentConfig;
        _dataHubClient = dataHubClient;
    }

    public async Task<NullResponse> HandleAsync(SendSyncSuccessesToDataHubRequest request, CancellationToken cancellationToken)
    {
        await _dataHubClient.PostRequestAsync<RegisterSyncSuccessesRequest, NullResponse>(new RegisterSyncSuccessesRequest()
        {
            SyncSuccesses = request.Successes.Select(s => new SyncSuccess()
            {
                AgentId = _businessCentralAgentConfig.Value.AgentId,
                DataSource = s.DataSource,
                DataHubEntityType = s.DataHubEntityType,
                DataHubEntityId = s.DataHubEntityId,
                SourceEntityType = s.SourceEntityType,
                SourceEntityId = s.SourceEntityId,
                Timestamp = DateTimeOffset.UtcNow
            }).ToList()
        }, cancellationToken);

        return new NullResponse();
    }
}
