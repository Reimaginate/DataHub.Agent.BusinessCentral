using Reimaginate.DataHub.SharedModels.Markers;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralSyncMarker;

public class GetBusinessCentralSyncMarkerResponse
{
    public GetBusinessCentralSyncMarkerResponse(SyncMarker syncMarker)
    {
        SyncMarker = syncMarker;
    }

    public SyncMarker SyncMarker { get; set; }
}