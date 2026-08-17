using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.External.SyncUpdatedDataHubEntities;

public class SyncUpdatedDataHubEntitiesResponse<TDataHubEntity, TBusinessCentralEntity> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
{
    public Dictionary<string, string> ProcessedEntities { get; set; } = new();
}
