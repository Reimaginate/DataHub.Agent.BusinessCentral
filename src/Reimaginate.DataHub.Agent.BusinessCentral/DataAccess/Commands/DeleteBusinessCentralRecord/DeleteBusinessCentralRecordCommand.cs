using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.DeleteBusinessCentralRecord
{
    public class DeleteBusinessCentralRecordCommand<TBDocument> : IRequest<NullResponse> where TBDocument : BusinessCentralDocument
    {
        public string RecordId { get; set; }
    }
}
