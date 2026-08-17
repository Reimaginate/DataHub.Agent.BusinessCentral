namespace Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

/// <summary>
/// Supplies the implementation-specific last-modified value used by incremental merge processing.
/// </summary>
public interface IBusinessCentralIncrementalEntity
{
    DateTimeOffset? LastModifiedAt { get; set; }
}
