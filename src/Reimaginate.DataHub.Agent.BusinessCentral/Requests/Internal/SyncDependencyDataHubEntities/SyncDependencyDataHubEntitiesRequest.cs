using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDependencyDataHubEntities
{
    public class SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity> : IRequest<ProcessDataHubEntitySyncResponse> where TDataHubEntity : DataHubEntity where TBusinessCentralEntity : BusinessCentralDocument
    {
        public SyncDependencyDataHubEntitiesRequest()
        { }

        public SyncDependencyDataHubEntitiesRequest(List<string> entityIds)
        {
            EntityIds = entityIds;
        }
        public SyncDependencyDataHubEntitiesRequest(List<string> entityIds, List<EntityReference> dependencyTree, List<ResolutionPromise> resolutionPromises)
        {
            EntityIds = entityIds;
            DependencyTree = dependencyTree;
            ResolutionPromises = resolutionPromises;
        }


        public List<string> EntityIds { get; set; }

        public List<EntityReference> DependencyTree { get; set; } = new();

        public string CorrelationId { get; set; }

        public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
    }
}
