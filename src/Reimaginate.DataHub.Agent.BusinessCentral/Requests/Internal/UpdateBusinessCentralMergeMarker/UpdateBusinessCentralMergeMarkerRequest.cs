using Reimaginate.DataHub.SharedModels.Markers;
using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.UpdateBusinessCentralMergeMarker;

public class UpdateBusinessCentralMergeMarkerRequest : IRequest<UpdateBusinessCentralMergeMarkerResponse>
{
    public MergeMarker? Marker { get; set; }
    public string? NewValue { get; set; }
}