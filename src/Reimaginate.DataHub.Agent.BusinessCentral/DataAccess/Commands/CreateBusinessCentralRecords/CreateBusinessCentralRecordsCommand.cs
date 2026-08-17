using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.CreateBusinessCentralRecords
{
    public class CreateBusinessCentralRecordsCommand<TBDocument> : IRequest<CreateBusinessCentralRecordsResponse<TBDocument>> where TBDocument : BusinessCentralDocument
    {
        public List<TBDocument> RecordsToCreate { get; set; }
    }
}
