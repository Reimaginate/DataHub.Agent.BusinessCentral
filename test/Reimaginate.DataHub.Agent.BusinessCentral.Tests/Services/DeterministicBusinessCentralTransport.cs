using System.Net;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Unit.Services;

internal sealed class DeterministicBusinessCentralTransport : HttpMessageHandler
{
    private readonly Queue<Func<RecordedBusinessCentralRequest, HttpResponseMessage>> _steps = new();

    public List<RecordedBusinessCentralRequest> Requests { get; } = [];

    public DeterministicBusinessCentralTransport Respond(HttpStatusCode statusCode, string json = "")
    {
        _steps.Enqueue(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        return this;
    }

    public DeterministicBusinessCentralTransport Respond(
        Func<RecordedBusinessCentralRequest, HttpResponseMessage> responseFactory)
    {
        _steps.Enqueue(responseFactory);
        return this;
    }

    public DeterministicBusinessCentralTransport Throw(Exception exception)
    {
        _steps.Enqueue(_ => throw exception);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var recorded = new RecordedBusinessCentralRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase),
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
        Requests.Add(recorded);

        if (_steps.Count == 0)
        {
            throw new InvalidOperationException(
                $"The deterministic Business Central transport has no response configured for {request.Method} {request.RequestUri}.");
        }

        return _steps.Dequeue()(recorded);
    }
}

internal sealed record RecordedBusinessCentralRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string[]> Headers,
    string? Body);
