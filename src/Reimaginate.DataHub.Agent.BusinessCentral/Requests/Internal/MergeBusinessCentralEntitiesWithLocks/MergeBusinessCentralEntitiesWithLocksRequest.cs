using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeBusinessCentralEntitiesWithLocks
{
    public class MergeBusinessCentralEntitiesWithLocksRequest<TBusinessCentralEntity, TDataHubEntity> : IRequest<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
    {
        public List<string> BusinessCentralEntityIds { get; set; } = new();

        public List<ExternalEntityReference> DependencyTree { get; set; } = new();

        public string? CorrelationId { get; set; }

        public bool ForceUpdate { get; set; } = false;
    }
}
