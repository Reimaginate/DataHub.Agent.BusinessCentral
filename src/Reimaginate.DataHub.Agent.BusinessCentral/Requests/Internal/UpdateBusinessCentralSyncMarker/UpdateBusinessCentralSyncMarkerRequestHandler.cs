using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Client;
using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralSyncMarker;

public class UpdateBusinessCentralSyncMarkerRequestHandler : IHandler<UpdateBusinessCentralSyncMarkerRequest, UpdateBusinessCentralSyncMarkerResponse>
{
    private readonly IOptions<BusinessCentralAgentOptions> _config;
    private readonly IDataHubClient _dataHubClient;

    public UpdateBusinessCentralSyncMarkerRequestHandler(IOptions<BusinessCentralAgentOptions> config, IDataHubClient dataHubClient)
    {
        _config = config;
        _dataHubClient = dataHubClient;
    }

    public async Task<UpdateBusinessCentralSyncMarkerResponse> HandleAsync(UpdateBusinessCentralSyncMarkerRequest request, CancellationToken cancellationToken)
    {
        var orchestratorRequest = new Reimaginate.DataHub.SharedModels.Requests.Client.UpdateSyncMarkerRequest()
        {
            SyncMarker = request.Marker,
            NewValue = request.NewValue
        };

        var orchestratorResponse = await _dataHubClient.PostRequestAsync<Reimaginate.DataHub.SharedModels.Requests.Client.UpdateSyncMarkerRequest, Reimaginate.DataHub.SharedModels.Requests.Client.UpdateSyncMarkerResponse>(orchestratorRequest, cancellationToken);

        return new UpdateBusinessCentralSyncMarkerResponse()
        {
            ResultingSyncMarker = orchestratorResponse.ResultingSyncMarker
        };
    }
}