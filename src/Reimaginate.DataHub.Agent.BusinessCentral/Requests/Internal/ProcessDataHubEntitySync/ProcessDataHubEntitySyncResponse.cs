using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;

public class ProcessDataHubEntitySyncResponse
{
    public List<SyncEntityResult> Results { get; set; } = new();
    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
}