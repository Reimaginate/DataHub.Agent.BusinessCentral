using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("accounts")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class GeneralLedgerAccount : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("number"), JsonPropertyName("number")]
    public string? Number { get => GetAttributeValue<string>("number"); set => SetWithNotification("number", value); }

    [JsonProperty("displayName"), JsonPropertyName("displayName")]
    public string? DisplayName { get => GetAttributeValue<string>("displayName"); set => SetWithNotification("displayName", value); }

    [JsonProperty("category"), JsonPropertyName("category")]
    public string? Category { get => GetAttributeValue<string>("category"); set => SetWithNotification("category", value); }

    [JsonProperty("subCategory"), JsonPropertyName("subCategory")]
    public string? SubCategory { get => GetAttributeValue<string>("subCategory"); set => SetWithNotification("subCategory", value); }

    [JsonProperty("blocked"), JsonPropertyName("blocked")]
    public bool? Blocked { get => GetAttributeValue<bool?>("blocked"); set => SetWithNotification("blocked", value); }

    [JsonProperty("accountType"), JsonPropertyName("accountType")]
    public string? AccountType { get => GetAttributeValue<string>("accountType"); set => SetWithNotification("accountType", value); }

    [JsonProperty("directPosting"), JsonPropertyName("directPosting")]
    public bool? DirectPosting { get => GetAttributeValue<bool?>("directPosting"); set => SetWithNotification("directPosting", value); }

    [JsonProperty("netChange"), JsonPropertyName("netChange")]
    public decimal? NetChange { get => GetAttributeValue<decimal?>("netChange"); set => SetWithNotification("netChange", value); }

    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }

    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
