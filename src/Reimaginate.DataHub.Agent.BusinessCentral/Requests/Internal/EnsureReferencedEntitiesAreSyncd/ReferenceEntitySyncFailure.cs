using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.EnsureReferencedEntitiesAreSyncd;

public class ReferenceEntitySyncFailure
{
    public ReferenceEntitySyncFailure()
    {
        
    }

    public ReferenceEntitySyncFailure(DataHubEntity entity, Exception exception)
    {
        Exception = exception;
        Entity = entity;
    }
    public DataHubEntity? Entity { get; set; }
    public Exception? Exception { get; set; }
}