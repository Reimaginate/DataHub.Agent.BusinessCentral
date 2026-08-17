using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.UpdateBusinessCentralRecords;

public class UpdateBusinessCentralRecordsCommandHandler<TBusinessCentralDocument> : IHandler<UpdateBusinessCentralRecordsCommand<TBusinessCentralDocument>, UpdateBusinessCentralRecordsResponse<TBusinessCentralDocument>> where TBusinessCentralDocument : BusinessCentralDocument
{
    private readonly IBusinessCentralODataService _businessCentralService;

    public UpdateBusinessCentralRecordsCommandHandler(IBusinessCentralODataService businessCentralService)
    {
        _businessCentralService = businessCentralService;
    }

    public async Task<UpdateBusinessCentralRecordsResponse<TBusinessCentralDocument>> HandleAsync(UpdateBusinessCentralRecordsCommand<TBusinessCentralDocument> command, CancellationToken cancellationToken)
    {
        var response = await _businessCentralService.UpdateEntitiesAsync(command.Records, cancellationToken);
        return new UpdateBusinessCentralRecordsResponse<TBusinessCentralDocument>()
        {
            Results = response
        };
    }
}
