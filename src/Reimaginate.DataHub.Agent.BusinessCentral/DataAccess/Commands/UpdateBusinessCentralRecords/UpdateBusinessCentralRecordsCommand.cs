using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.UpdateBusinessCentralRecords
{
    public class UpdateBusinessCentralRecordsCommand<TBusinessCentralDocument> : IRequest<UpdateBusinessCentralRecordsResponse<TBusinessCentralDocument>> where TBusinessCentralDocument : BusinessCentralDocument
    {
        public List<TBusinessCentralDocument> Records { get; set; }
        public bool? DisableRowVersionCheck { get; set; }
    }
}
