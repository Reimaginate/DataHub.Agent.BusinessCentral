using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;

public class GetBusinessCentralSyncMarkerRequest : IRequest<GetBusinessCentralSyncMarkerResponse>
{
    public string? EntityType { get; set; }
    public string? DefaultValue { get; set; }
}