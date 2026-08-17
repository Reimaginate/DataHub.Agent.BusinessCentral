using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.GetBusinessCentralMergeMarker
{
    public class GetBusinessCentralMergeMarkerRequest : IRequest<GetBusinessCentralMergeMarkerResponse>
    {
        public string? EntityType { get; set; }
        public string? DefaultValue { get; set; }
    }
}
