
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ResolveResolutionPromises;

public class ResolvedResolutionPromise
{
    public Type DataHubType { get; set; }
    public Type BusinessCentralType { get; set; }
    public string BusinessCentralEntityId { get; set; }
    public string DataHubEntityId { get; set; }
}
