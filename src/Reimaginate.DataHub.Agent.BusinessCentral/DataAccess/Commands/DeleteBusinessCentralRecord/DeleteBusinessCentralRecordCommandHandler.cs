using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.DeleteBusinessCentralRecord;

public class DeleteBusinessCentralRecordCommandHandler<TBusinessCentralDocument> : IHandler<DeleteBusinessCentralRecordCommand<TBusinessCentralDocument>, NullResponse> where TBusinessCentralDocument : BusinessCentralDocument
{
    private readonly IBusinessCentralODataService _businessCentralService;

    public DeleteBusinessCentralRecordCommandHandler(IBusinessCentralODataService businessCentralService)
    {
        _businessCentralService = businessCentralService;
    }

    public async Task<NullResponse> HandleAsync(DeleteBusinessCentralRecordCommand<TBusinessCentralDocument> command, CancellationToken cancellationToken)
    {
        var deleteResponse = await _businessCentralService.DeleteEntityAsync<TBusinessCentralDocument>(command.RecordId, cancellationToken);
        if (deleteResponse.IsT2) throw deleteResponse.AsT2;

        return new NullResponse();
    }
}
