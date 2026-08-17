using FluentAssertions;
using Reimaginate.DataHub;
using Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.RetrieveUpdatedDataHubEntities;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Xunit;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

public sealed class RetrieveUpdatedDataHubEntitiesRequestHandlerTests
{
    [Fact(DisplayName = "Updated entity retrieval preserves the Data Hub failure reason")]
    public async Task PreservesDataHubFailureReason()
    {
        var client = new StubDataHubClient(new GetDataHubEntitiesResponse
        {
            Success = false,
            FailureReason = "Cosmos query failed"
        });
        var handler = new RetrieveUpdatedDataHubEntitiesRequestHandler<TestAccount>(client);

        var action = () => handler.HandleAsync(new RetrieveUpdatedDataHubEntitiesRequest<TestAccount>
        {
            FromDateTime = DateTimeOffset.UtcNow,
            PageSize = 2
        }, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cosmos query failed*");
    }

    [Fact(DisplayName = "Successful updated entity retrieval accepts an omitted result collection")]
    public async Task SuccessfulNullResultsAreEmpty()
    {
        var client = new StubDataHubClient(new GetDataHubEntitiesResponse
        {
            Success = true,
            Results = null!
        });
        var handler = new RetrieveUpdatedDataHubEntitiesRequestHandler<TestAccount>(client);

        var response = await handler.HandleAsync(new RetrieveUpdatedDataHubEntitiesRequest<TestAccount>
        {
            FromDateTime = DateTimeOffset.UtcNow,
            PageSize = 2
        }, CancellationToken.None);

        response.Results.Should().BeEmpty();
    }

    private sealed class StubDataHubClient(GetDataHubEntitiesResponse response) : IDataHubClient
    {
        public Task<TResponse> PostRequestAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken cancellationToken)
            where TRequest : DataHubClientRequest<TResponse>
            where TResponse : class => Task.FromResult((TResponse)(object)response);
    }

    private sealed class TestAccount : DataHubEntity;
}
