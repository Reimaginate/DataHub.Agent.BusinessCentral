using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SendMergeSuccessesToDataHub;

public class SendMergeSuccessesToDataHubRequestHandler : IHandler<SendMergeSuccessesToDataHubRequest, NullResponse>
{
    private readonly IOptions<BusinessCentralAgentOptions> _businessCentralAgentConfig;
    private readonly IDataHubClient _dataHubClient;
    

    public SendMergeSuccessesToDataHubRequestHandler(IOptions<BusinessCentralAgentOptions> businessCentralAgentConfig, IDataHubClient dataHubClient)
    {
        _businessCentralAgentConfig = businessCentralAgentConfig;
        _dataHubClient = dataHubClient;
    }

    public async Task<NullResponse> HandleAsync(SendMergeSuccessesToDataHubRequest request, CancellationToken cancellationToken)
    {
        await _dataHubClient.PostRequestAsync<RegisterMergeSuccessesRequest, NullResponse>(new RegisterMergeSuccessesRequest()
        {
            MergeSuccesses = request.Successes.Select(s => new MergeSuccess()
            {
                AgentId = _businessCentralAgentConfig.Value.AgentId,
                DataSource = _businessCentralAgentConfig.Value.DataSource,
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
