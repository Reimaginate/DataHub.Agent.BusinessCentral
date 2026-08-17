using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.CreateBusinessCentralRecords;

public class CreateBusinessCentralRecordsCommandHandler<TBusinessCentralDocument> : IHandler<CreateBusinessCentralRecordsCommand<TBusinessCentralDocument>, CreateBusinessCentralRecordsResponse<TBusinessCentralDocument>> where TBusinessCentralDocument : BusinessCentralDocument
{
    private readonly IBusinessCentralODataService _businessCentralService;

    public CreateBusinessCentralRecordsCommandHandler(IBusinessCentralODataService businessCentralService)
    {
        _businessCentralService = businessCentralService;
    }

    public async Task<CreateBusinessCentralRecordsResponse<TBusinessCentralDocument>> HandleAsync(CreateBusinessCentralRecordsCommand<TBusinessCentralDocument> command, CancellationToken cancellationToken)
    {
        var createResponse = await _businessCentralService.CreateEntitiesAsync(command.RecordsToCreate, cancellationToken);
        if (createResponse.IsT1) throw createResponse.AsT1;
       
        return new CreateBusinessCentralRecordsResponse<TBusinessCentralDocument>()
        {
            Results = createResponse.AsT0
        };
    }
}
