using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDataHubEntitiesWithLocks;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.SyncDependencyDataHubEntities;

public class SyncDependencyDataHubEntitiesRequestHandler<TDataHubEntity, TBusinessCentralEntity> : IHandler<SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity>, ProcessDataHubEntitySyncResponse> where TDataHubEntity : DataHubEntity where TBusinessCentralEntity : BusinessCentralDocument
{
    private readonly IOptions<BusinessCentralAgentOptions> _config;
    private readonly IDataHubClient _dataHubClient;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SyncDependencyDataHubEntitiesRequestHandler(IOptions<BusinessCentralAgentOptions> config, IDataHubClient dataHubClient, IMediator mediator, IMapper mapper)
    {
        _config = config;
        _dataHubClient = dataHubClient;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<ProcessDataHubEntitySyncResponse> HandleAsync(SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TBusinessCentralEntity> request, CancellationToken cancellationToken)
    {
        var response = (await _mediator.TrySend<ProcessDataHubEntitySyncResponse>(new SyncDataHubEntitiesWithLocksRequest<TDataHubEntity, TBusinessCentralEntity>()
        {
            CorrelationId = request.CorrelationId,
            EntityIds = request.EntityIds,
            DependencyTree = request.DependencyTree,
            ResolutionPromises = request.ResolutionPromises
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        return response;
    }
}
