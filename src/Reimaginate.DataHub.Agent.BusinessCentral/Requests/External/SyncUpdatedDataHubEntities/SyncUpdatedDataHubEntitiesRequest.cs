using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Mediator;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncUpdatedDataHubEntities
{
    public class SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity> : IRequest<NullResponse> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
    {
        public Func<List<JObject>, string?, IRequest>? ProcessorOverride { get; set; }
        public string? CorrelationId { get; set; }
        public int BatchSize { get; set; } = 500;
    }
}
