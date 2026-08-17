using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync
{
    public class ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralEntity> : IRequest<ProcessDataHubEntitySyncResponse> where TDataHubEntity : DataHubEntity where TBusinessCentralEntity : BusinessCentralDocument
    {
        public ProcessDataHubEntitySyncRequest()
        {
            
        }

        public ProcessDataHubEntitySyncRequest(List<TDataHubEntity> dataHubEntities, List<EntityReference> dependencyTree)
        {
            DataHubEntities = dataHubEntities;
            DependencyTree = dependencyTree;
        }

        public Dictionary<string, object> Cache { get; set; } = new();

        public List<TDataHubEntity> DataHubEntities { get; set; } = new();

        public List<EntityReference> DependencyTree { get; set; } = new();

        public string? CorrelationId { get; set; }

        public List<ResolutionPromise> ResolutionPromises { get; set; } = new();

        internal int ConflictRetryCount { get; set; }

    }
}
