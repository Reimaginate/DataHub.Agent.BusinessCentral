using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncSpecificDataHubEntities
{
    public class SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity> : IRequest<ProcessDataHubEntitySyncResponse> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
    {
        public List<string> EntityIds { get; set; } = new();
        public Func<List<JObject>, string, IRequest>? ProcessorOverride { get; set; }
        public string? ContinuationToken { get; set; }
        public string? CorrelationId { get; set; }
    }
}
