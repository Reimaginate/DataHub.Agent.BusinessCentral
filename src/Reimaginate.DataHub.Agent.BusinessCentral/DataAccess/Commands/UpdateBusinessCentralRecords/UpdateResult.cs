using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using System.Net;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.UpdateBusinessCentralRecords;

public class UpdateResult<TBusinessCentralDocument> where TBusinessCentralDocument : BusinessCentralDocument
{
    public string? EntityId { get; set; }
    public TBusinessCentralDocument? ResultingEntity { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
    public HttpStatusCode? StatusCode { get; set; }
}
