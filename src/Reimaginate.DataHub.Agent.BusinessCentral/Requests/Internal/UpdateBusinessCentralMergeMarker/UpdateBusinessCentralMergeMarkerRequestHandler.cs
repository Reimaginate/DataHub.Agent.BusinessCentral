using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Client;
using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralMergeMarker;

public class UpdateBusinessCentralMergeMarkerRequestHandler : IHandler<UpdateBusinessCentralMergeMarkerRequest, UpdateBusinessCentralMergeMarkerResponse>
{
    private readonly IOptions<BusinessCentralAgentOptions> _config;
    private readonly IDataHubClient _dataHubClient;

    public UpdateBusinessCentralMergeMarkerRequestHandler(IOptions<BusinessCentralAgentOptions> config, IDataHubClient dataHubClient)
    {
        _config = config;
        _dataHubClient = dataHubClient;
    }

    public async Task<UpdateBusinessCentralMergeMarkerResponse> HandleAsync(UpdateBusinessCentralMergeMarkerRequest request, CancellationToken cancellationToken)
    {
        var dataHubRequest = new Reimaginate.DataHub.SharedModels.Requests.Client.UpdateMergeMarkerRequest()
        {
            MergeMarker = request.Marker,
            NewValue = request.NewValue
        };

        var orchestratorResponse = await _dataHubClient.PostRequestAsync<Reimaginate.DataHub.SharedModels.Requests.Client.UpdateMergeMarkerRequest, Reimaginate.DataHub.SharedModels.Requests.Client.UpdateMergeMarkerResponse>(dataHubRequest, cancellationToken);

        return new UpdateBusinessCentralMergeMarkerResponse()
        {
            ResultingMergeMarker = orchestratorResponse.ResultingMergeMarker
        };
    }
}