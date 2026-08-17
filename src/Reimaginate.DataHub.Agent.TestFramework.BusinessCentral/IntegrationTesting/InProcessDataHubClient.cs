using Newtonsoft.Json;
using Reimaginate.DataHub.Requests.External.Client.DeserializeClientRequest;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public sealed class InProcessDataHubClient(IMediator mediator) : IDataHubClient
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        DateParseHandling = DateParseHandling.DateTimeOffset
    };

    public async Task<TResponse> PostRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : DataHubClientRequest<TResponse>
        where TResponse : class
    {
        request.CorrelationId ??= Guid.NewGuid().ToString("N");

        var serializedRequest = new SerializedRequest
        {
            RequestType = request.RequestType,
            CorrelationId = request.CorrelationId,
            Data = JsonConvert.SerializeObject(request, SerializerSettings)
        };

        var deserializeResult = await mediator.TrySend<IRequest>(
            new DeserializeClientRequestRequest { SerializedRequest = serializedRequest },
            cancellationToken);
        if (deserializeResult.Item2 is not null)
        {
            throw deserializeResult.Item2;
        }

        var deserializedRequest = deserializeResult.Item1 as IRequest<TResponse>
            ?? throw new InvalidOperationException(
                $"Data Hub deserialized {typeof(TRequest).Name} to an incompatible or null mediator request.");

        var responseResult = await mediator.TrySend<TResponse>(deserializedRequest, cancellationToken);
        if (responseResult.Item2 is not null)
        {
            throw responseResult.Item2;
        }

        return responseResult.Item1
            ?? throw new InvalidOperationException($"Data Hub returned no {typeof(TResponse).Name} response.");
    }
}
