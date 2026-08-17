using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.EnsureReferencedEntitiesAreSyncd;

public class EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TBusinessCentralEntity> where TDataHubEntity : DataHubEntity, new() where TBusinessCentralEntity :BusinessCentralDocument, new()
{
    public List<JObject> CachedEntities { get; set; }
    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
    public List<ReferenceEntitySyncFailure> Failures { get; set; } = new();
}
