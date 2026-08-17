using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RegisterAlternateKey;

public class RegisterAlternateKeyRequestHandler : IHandler<RegisterAlternateKeyRequest, NullResponse>
{
    private readonly IDataHubClient _dataHubClient;

    public RegisterAlternateKeyRequestHandler(IDataHubClient dataHubClient)
    {
        _dataHubClient = dataHubClient;
    }

    public async Task<NullResponse> HandleAsync(RegisterAlternateKeyRequest request, CancellationToken cancellationToken)
    {
        var req = new Reimaginate.DataHub.SharedModels.Requests.Client.RegisterAlternateKeyRequest()
        {
            EntityType = request.EntityType,
            DataHubEntityId = request.DataHubEntityId,
            Key = request.Key ?? "BusinessCentral",
            SourceEntityId = request.SourceSystemEntityId
        };

        var response = await _dataHubClient.PostRequestAsync<Reimaginate.DataHub.SharedModels.Requests.Client.RegisterAlternateKeyRequest, RegisterAlternateKeyResponse>(req, cancellationToken);
        return new NullResponse();
    }
}
