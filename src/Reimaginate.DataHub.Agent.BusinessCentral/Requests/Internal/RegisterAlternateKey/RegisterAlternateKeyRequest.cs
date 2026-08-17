using Reimaginate.Mediator;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RegisterAlternateKey;

public class RegisterAlternateKeyRequest : IRequest<NullResponse>
{
    public string Key { get; set; }
    public string EntityType { get; set; }
    public string SourceSystemEntityId { get; set; }
    public string DataHubEntityId { get; set; }

}