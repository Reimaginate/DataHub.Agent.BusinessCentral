using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Client;
using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;

public class GetBusinessCentralSyncMarkerRequestHandler : IHandler<GetBusinessCentralSyncMarkerRequest, GetBusinessCentralSyncMarkerResponse>
{
    private readonly IOptions<BusinessCentralAgentOptions> _config;
    private readonly IDataHubClient _dataHubClient;

    public GetBusinessCentralSyncMarkerRequestHandler(IOptions<BusinessCentralAgentOptions> config, IDataHubClient dataHubClient)
    {
        _config = config;
        _dataHubClient = dataHubClient;
    }

    public async Task<GetBusinessCentralSyncMarkerResponse> HandleAsync(GetBusinessCentralSyncMarkerRequest request, CancellationToken cancellationToken)
    {
        var syncMarkerRequest = new SharedModels.Requests.Client.GetSyncMarkerRequest()
        {
            DataSource = _config.Value.DataSource,
            AgentId = _config.Value.AgentId,
            DataHubEntityType = request.EntityType,
            DefaultValue = request.DefaultValue
        };

        var dataHubResponse = await _dataHubClient.PostRequestAsync<SharedModels.Requests.Client.GetSyncMarkerRequest, SharedModels.Requests.Client.GetSyncMarkerResponse>(syncMarkerRequest, cancellationToken);
        return new GetBusinessCentralSyncMarkerResponse(dataHubResponse.SyncMarker);
    }
}
