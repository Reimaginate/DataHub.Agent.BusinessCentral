using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.CreateBusinessCentralRecords;

public class CreateResult<TBusinessCentralDocument> where TBusinessCentralDocument : BusinessCentralDocument
{
    public string EntityId { get; set; }
    public TBusinessCentralDocument ResultingEntity { get; set; }
    public bool Success { get; set; }
    public Exception Exception { get; set; }
}
