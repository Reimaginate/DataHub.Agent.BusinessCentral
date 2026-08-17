using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Client;
using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralMergeMarker;

public class GetBusinessCentralMergeMarkerRequestHandler : IHandler<GetBusinessCentralMergeMarkerRequest, GetBusinessCentralMergeMarkerResponse>
{
    private readonly IOptions<BusinessCentralAgentOptions> _config;
    private readonly IDataHubClient _dataHubClient;

    public GetBusinessCentralMergeMarkerRequestHandler(IOptions<BusinessCentralAgentOptions> config, IDataHubClient dataHubClient)
    {
        _config = config;
        _dataHubClient = dataHubClient;
    }

    public async Task<GetBusinessCentralMergeMarkerResponse> HandleAsync(GetBusinessCentralMergeMarkerRequest request, CancellationToken cancellationToken)
    {
        var dataHubRequest = new Reimaginate.DataHub.SharedModels.Requests.Client.GetMergeMarkerRequest()
        {
            DataSource = _config.Value.DataSource,
            AgentId = _config.Value.AgentId,
            SourceEntityType = request.EntityType,
            DefaultValue = request.DefaultValue
        };
        
        var response = await _dataHubClient.PostRequestAsync<Reimaginate.DataHub.SharedModels.Requests.Client.GetMergeMarkerRequest, Reimaginate.DataHub.SharedModels.Requests.Client.GetMergeMarkerResponse>(dataHubRequest, cancellationToken);

        return new GetBusinessCentralMergeMarkerResponse()
        {
            MergeMarker = response.MergeMarker
        };
    }
}
