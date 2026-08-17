using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ResolveResolutionPromises;

public class ResolveResolutionPromisesResponse<TDataHubEntity, TBusinessCentralSibling> where TDataHubEntity : DataHubEntity where TBusinessCentralSibling : BusinessCentralDocument
{
    public List<ResolvedResolutionPromise> UpdatedEntities { get; set; }
    public List<ResolutionPromise> ResolvedPromises { get; set; }
}
