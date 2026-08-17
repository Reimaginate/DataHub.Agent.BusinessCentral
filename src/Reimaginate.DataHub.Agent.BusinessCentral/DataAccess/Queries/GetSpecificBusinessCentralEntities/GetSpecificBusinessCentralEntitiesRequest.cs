using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Queries.GetSpecificBusinessCentralEntities
{
    public class GetSpecificBusinessCentralEntitiesRequest<TBusinessCentralEntity> : IRequest<List<TBusinessCentralEntity>> where TBusinessCentralEntity : BusinessCentralDocument
    {
        public List<string> EntityIds { get; set; } = new();
    }
}
