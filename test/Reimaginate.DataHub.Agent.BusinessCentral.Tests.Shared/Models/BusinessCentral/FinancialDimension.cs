using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("dimensions")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class FinancialDimension : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("code"), JsonPropertyName("code")]
    public string? Code { get => GetAttributeValue<string>("code"); set => SetWithNotification("code", value); }

    [JsonProperty("displayName"), JsonPropertyName("displayName")]
    public string? DisplayName { get => GetAttributeValue<string>("displayName"); set => SetWithNotification("displayName", value); }

    [JsonProperty("consolidationCode"), JsonPropertyName("consolidationCode")]
    public string? ConsolidationCode { get => GetAttributeValue<string>("consolidationCode"); set => SetWithNotification("consolidationCode", value); }

    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }

    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
