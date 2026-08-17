using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using EntityReference = Reimaginate.DataHub.SharedModels.Core.EntityReference;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.EnsureReferencedEntitiesAreSyncd
{
    public class EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TBusinessCentralEntity> : IRequest<EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TBusinessCentralEntity>> where TDataHubEntity : DataHubEntity, new() where TBusinessCentralEntity : BusinessCentralDocument, new()
    {
        public List<TDataHubEntity>? Entities { get; set; } 
        public List<EntityReference>? DependencyTree { get; set; }
        public List<ResolutionPromise>? ResolutionPromises { get; set; }
        public IDataHubTypeMapper<TDataHubEntity, TBusinessCentralEntity>? TypeMap { get; set; }
    }
}
