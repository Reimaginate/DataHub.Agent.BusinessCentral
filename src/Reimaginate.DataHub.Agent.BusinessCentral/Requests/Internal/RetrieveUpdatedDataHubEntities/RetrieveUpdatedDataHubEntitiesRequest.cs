using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RetrieveUpdatedDataHubEntities
{
    public class RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity> : IRequest<RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>> where TDataHubEntity : DataHubEntity
    {
        public DateTimeOffset FromDateTime { get; set; }
        public string? ContinuationToken { get; set; }
        public int PageSize { get; set; } = 500;
    }
}
