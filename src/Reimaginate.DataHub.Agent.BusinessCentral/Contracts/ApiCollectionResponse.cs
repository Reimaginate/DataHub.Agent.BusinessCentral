using System.Text.Json.Serialization;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Contracts;

/// <summary>
/// Business Central OData collection envelope used by the runtime transport service.
/// </summary>
public sealed class ApiCollectionResponse<T>
{
    [JsonPropertyName("@odata.count")]
    public int? Count { get; set; }

    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}
