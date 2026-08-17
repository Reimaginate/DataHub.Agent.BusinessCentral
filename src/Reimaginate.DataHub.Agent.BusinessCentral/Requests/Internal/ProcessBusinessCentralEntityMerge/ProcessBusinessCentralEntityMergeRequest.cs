using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge
{
    public class ProcessBusinessCentralEntityMergeRequest<TBusinessCentralEntity, TDataHubEntity> : IRequest<ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity>> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
    {
        public ProcessBusinessCentralEntityMergeRequest()
        {
            
        }

        public ProcessBusinessCentralEntityMergeRequest(List<TBusinessCentralEntity> businessCentralEntities, List<ExternalEntityReference>? dependencyTree = null)
        {
            BusinessCentralEntities = businessCentralEntities;
            DependencyTree = dependencyTree ?? new();
        }

        public Dictionary<string, object> Cache { get; set; } = new();

        public List<TBusinessCentralEntity>? BusinessCentralEntities { get; set; }

        public List<ExternalEntityReference>? DependencyTree { get; set; }

        public string? CorrelationId { get; set; }
    }
}
