using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessBusinessCentralEntityMerge;

public class ProcessBusinessCentralEntityMergeResponse<TBusinessCentralEntity, TDataHubEntity> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
{
    public List<MergeEntityResult> Results { get; set; } = new();
}
