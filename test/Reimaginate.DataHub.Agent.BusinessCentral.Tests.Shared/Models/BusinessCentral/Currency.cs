using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("currencies")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class Currency : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("code")]
    [JsonPropertyName("code")]
    public string? Code
    {
        get => GetAttributeValue<string>("code");
        set => SetWithNotification("code", value);
    }

    [JsonProperty("displayName")]
    [JsonPropertyName("displayName")]
    public string? DisplayName
    {
        get => GetAttributeValue<string>("displayName");
        set => SetWithNotification("displayName", value);
    }

    [JsonProperty("symbol")]
    [JsonPropertyName("symbol")]
    public string? Symbol
    {
        get => GetAttributeValue<string>("symbol");
        set => SetWithNotification("symbol", value);
    }

    [JsonProperty("amountDecimalPlaces")]
    [JsonPropertyName("amountDecimalPlaces")]
    public string? AmountDecimalPlaces
    {
        get => GetAttributeValue<string>("amountDecimalPlaces");
        set => SetWithNotification("amountDecimalPlaces", value);
    }

    [JsonProperty("amountRoundingPrecision")]
    [JsonPropertyName("amountRoundingPrecision")]
    public decimal? AmountRoundingPrecision
    {
        get => GetAttributeValue<decimal?>("amountRoundingPrecision");
        set => SetWithNotification("amountRoundingPrecision", value);
    }

    [JsonProperty("lastModifiedDateTime")]
    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime
    {
        get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime");
        set => SetWithNotification("lastModifiedDateTime", value);
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt
    {
        get => LastModifiedDateTime;
        set => LastModifiedDateTime = value;
    }
}
