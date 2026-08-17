using Reimaginate.DataHub.Agent.BusinessCentral.Helpers;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RetrieveUpdatedDataHubEntities
{
    public class RetrieveUpdatedDataHubEntitiesRequestHandler<TDataHubEntity> : IHandler<RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity>, RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>> where TDataHubEntity : DataHubEntity
    {
        private readonly IDataHubClient _dataHubClient;
      
        public RetrieveUpdatedDataHubEntitiesRequestHandler(IDataHubClient dataHubClient)
        {
            _dataHubClient = dataHubClient;
        }

        public async Task<RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>> HandleAsync(RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity> request, CancellationToken cancellationToken)
        {
            var req = new GetUpdatedDataHubEntitiesRequest()
            {
                EntityType = typeof(TDataHubEntity).Name,
                FromDateTime = request.FromDateTime,
                ContinuationToken = request.ContinuationToken,
                PageSize = Math.Max(1, request.PageSize),
                Select = "x.id,x.lastUpdated"
            };

            var ret = await _dataHubClient.PostRequestAsync<GetUpdatedDataHubEntitiesRequest, GetDataHubEntitiesResponse>(req, cancellationToken);

            if (!ret.Success)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(ret.FailureReason)
                        ? $"Data Hub failed to retrieve updated {typeof(TDataHubEntity).Name} records without a failure reason."
                        : $"Data Hub failed to retrieve updated {typeof(TDataHubEntity).Name} records: {ret.FailureReason}");
            }

            return new RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>()
            {
                Results = (ret.Results ?? []).Select(s => s.ToObjectIgnoreErrors<TDataHubEntity>()).ToList(),
                ResultCount = ret.ResultCount,
                MoreResultsAvailable = ret.MoreResultsAvailable,
                ContinuationToken = ret.ContinuationToken
            };
        }
    }
}
