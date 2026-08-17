using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

// ReSharper disable IdentifierTypo

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.MergeDependencyBusinessCentralEntities
{
    public class MergeDependencyBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity> : IRequest<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
    {
        public MergeDependencyBusinessCentralEntitiesRequest()
        { }

        public MergeDependencyBusinessCentralEntitiesRequest(List<string> entityIds)
        {
            EntityIds = entityIds;
        }
        public MergeDependencyBusinessCentralEntitiesRequest(List<string> entityIds, List<ExternalEntityReference> dependencyTree)
        {
            EntityIds = entityIds;
            DependencyTree = dependencyTree;
        }

        public MergeDependencyBusinessCentralEntitiesRequest(List<string> entityIds, List<ExternalEntityReference> dependencyTree, string correlationId)
        {
            EntityIds = entityIds;
            DependencyTree = dependencyTree;
            CorrelationId = correlationId;
        }


        public List<string> EntityIds { get; set; }

        public List<ExternalEntityReference> DependencyTree { get; set; } = new();

        public string CorrelationId { get; set; }
    }
}
