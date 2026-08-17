using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RetrieveUpdatedDataHubEntities;

public class RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity> where TDataHubEntity : DataHubEntity
{
    public List<TDataHubEntity> Results { get; set; } = new();
    public string? ContinuationToken { get; set; }
    public int ResultCount { get; set; }
    public bool MoreResultsAvailable { get; set; }
}
