using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ResolveResolutionPromises
{
    public class ResolveResolutionPromisesRequest<TDataHubEntity, TBusinessCentralSibling> : IRequest<ResolveResolutionPromisesResponse<TDataHubEntity, TBusinessCentralSibling>> where TDataHubEntity : DataHubEntity where TBusinessCentralSibling : BusinessCentralDocument
    {
        public List<ResolutionPromise> ResolutionPromises { get; set; }
        public List<TDataHubEntity> EntitiesToResolve { get; set; }
    }
}
