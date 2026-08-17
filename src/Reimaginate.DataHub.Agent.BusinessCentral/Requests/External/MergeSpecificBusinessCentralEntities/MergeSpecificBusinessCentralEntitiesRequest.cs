using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

// ReSharper disable IdentifierTypo

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.MergeSpecificBusinessCentralEntities
{
    public class MergeSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity, TDataHubEntity> : IRequest<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
    {
        public MergeSpecificBusinessCentralEntitiesRequest()
        { }

        public MergeSpecificBusinessCentralEntitiesRequest(List<string> entityIds)
        {
            EntityIds = entityIds;
        }

        public MergeSpecificBusinessCentralEntitiesRequest(List<string> entityIds, string correlationId) : this(entityIds)
        {
            CorrelationId = correlationId;
        }

        public List<string> EntityIds { get; set; } = new();

        public string? CorrelationId { get; set; }

        public bool ForceUpdate { get; set; } = false;
    }
}
