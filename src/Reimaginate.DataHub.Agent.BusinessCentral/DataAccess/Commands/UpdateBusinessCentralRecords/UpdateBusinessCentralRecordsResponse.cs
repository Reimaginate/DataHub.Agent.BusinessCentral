using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.UpdateBusinessCentralRecords;

public class UpdateBusinessCentralRecordsResponse<TBusinessCentralDocument> where TBusinessCentralDocument : BusinessCentralDocument
{
    public List<UpdateResult<TBusinessCentralDocument>> Results { get; set; } = new();
}
