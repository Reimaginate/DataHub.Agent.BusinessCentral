using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.CreateBusinessCentralRecords;

public class CreateBusinessCentralRecordsResponse<TBusinessCentralDocument> where TBusinessCentralDocument : BusinessCentralDocument
{
    public List<CreateResult<TBusinessCentralDocument>> Results { get; set; } = new();
}
