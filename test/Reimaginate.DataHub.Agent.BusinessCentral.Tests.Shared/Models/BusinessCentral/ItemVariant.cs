using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("itemVariants")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class ItemVariant : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("itemId")]
    [JsonPropertyName("itemId")]
    public Guid? ItemId
    {
        get => GetAttributeValue<Guid?>("itemId");
        set => SetWithNotification("itemId", value);
    }

    [JsonProperty("itemNumber")]
    [JsonPropertyName("itemNumber")]
    public string? ItemNumber
    {
        get => GetAttributeValue<string>("itemNumber");
        set => SetWithNotification("itemNumber", value);
    }

    [JsonProperty("code")]
    [JsonPropertyName("code")]
    public string? Code
    {
        get => GetAttributeValue<string>("code");
        set => SetWithNotification("code", value);
    }

    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string? Description
    {
        get => GetAttributeValue<string>("description");
        set => SetWithNotification("description", value);
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
