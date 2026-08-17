using Reimaginate.DataHub.SharedModels.Markers;
using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralSyncMarker;

public class UpdateBusinessCentralSyncMarkerRequest : IRequest<UpdateBusinessCentralSyncMarkerResponse>
{
    public SyncMarker? Marker { get; set; }
    public string? NewValue { get; set; }
}