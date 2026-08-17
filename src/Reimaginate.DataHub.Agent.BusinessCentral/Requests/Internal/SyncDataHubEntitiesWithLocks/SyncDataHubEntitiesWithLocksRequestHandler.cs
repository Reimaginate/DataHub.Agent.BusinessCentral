using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using Reimaginate.ProcessingLockService.Abstractions;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDataHubEntitiesWithLocks;

public class SyncDataHubEntitiesWithLocksRequestHandler<TDataHubEntity, TBusinessCentralEntity> : IHandler<SyncDataHubEntitiesWithLocksRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse> where TBusinessCentralEntity : BusinessCentralDocument where TDataHubEntity : DataHubEntity
{
    private readonly IOptions<BusinessCentralAgentOptions> _businessCentralAgentConfig;
    private readonly IDataHubClient _dataHubClient;
    private readonly IMediator _mediator;
    private readonly IProcessingLockService _processingLockService;

    public SyncDataHubEntitiesWithLocksRequestHandler(IOptions<BusinessCentralAgentOptions> businessCentralAgentConfig, IDataHubClient dataHubClient, IMediator mediator, IProcessingLockService processingLockService)
    {
        _businessCentralAgentConfig = businessCentralAgentConfig;
        _dataHubClient = dataHubClient;
        _mediator = mediator;
        _processingLockService = processingLockService;
    }

    public async Task<ProcessDataHubEntitySyncResponse> HandleAsync(SyncDataHubEntitiesWithLocksRequest<TDataHubEntity, TBusinessCentralEntity> request, CancellationToken cancellationToken)
    {
        if (!request.EntityIds.Any()) return new ProcessDataHubEntitySyncResponse();

        List<ProcessingLock>? dataHubEntityLocks = null;

        try
        {
            var entityIds = request.EntityIds.Distinct().ToList();
            var getLocksResponse = await _processingLockService.WaitForLocksAsync(entityIds.Select(s => $"{_businessCentralAgentConfig.Value.DataSource}.{typeof(TDataHubEntity).Name}.{s}").ToList(), request.CorrelationId, cancellationToken);
            getLocksResponse.ThrowIfUnsuccessful();
            dataHubEntityLocks = getLocksResponse.Result;

            var getDataHubEntitiesByIdResponse = await _dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
            {
                EntityType = typeof(TDataHubEntity).Name,
                EntityIds = entityIds
            }, cancellationToken);

            var dataHubEntities = getDataHubEntitiesByIdResponse.Results;

            ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralEntity>? processRequest;

            if (request.ProcessorOverride != null)
            {
                processRequest = (ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralEntity>)request.ProcessorOverride(dataHubEntities, request.CorrelationId);
            }
            else
            {
                var ser = new JsonSerializer();
                ser.Error += (_, args) =>
                {
                    if (args.ErrorContext.Error.Message.StartsWith("Error reading"))
                    {
                        args.ErrorContext.Handled = true;
                    }
                };
                processRequest = new ProcessDataHubEntitySyncRequest<TDataHubEntity, TBusinessCentralEntity>()
                {
                    CorrelationId = request.CorrelationId,
                    DependencyTree = request.DependencyTree,
                    DataHubEntities = dataHubEntities.Select(s => s.ToObject<TDataHubEntity>(ser)!).ToList(),
                    ResolutionPromises = request.ResolutionPromises
                };
            }

            var response = (await _mediator.TrySend<ProcessDataHubEntitySyncResponse>(processRequest, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
            return response;
        }
        finally
        {
            if (dataHubEntityLocks != null) await _processingLockService.ReleaseLocksAsync(dataHubEntityLocks, cancellationToken);
        }
    }
}
