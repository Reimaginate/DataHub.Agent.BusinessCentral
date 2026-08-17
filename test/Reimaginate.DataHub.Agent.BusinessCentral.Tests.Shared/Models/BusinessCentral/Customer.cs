using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("customers")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class Customer : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("number")]
    [JsonPropertyName("number")]
    public string? Number
    {
        get => GetAttributeValue<string>("number");
        set => SetWithNotification("number", value!);
    }

    [JsonProperty("displayName")]
    [JsonPropertyName("displayName")]
    public string? DisplayName
    {
        get => GetAttributeValue<string>("displayName");
        set => SetWithNotification("displayName", value!);
    }

    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string? Type
    {
        get => GetAttributeValue<string>("type");
        set => SetWithNotification("type", value!);
    }

    [JsonProperty("addressLine1")]
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1
    {
        get => GetAttributeValue<string>("addressLine1");
        set => SetWithNotification("addressLine1", value!);
    }

    [JsonProperty("addressLine2")]
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2
    {
        get => GetAttributeValue<string>("addressLine2");
        set => SetWithNotification("addressLine2", value!);
    }

    [JsonProperty("city")]
    [JsonPropertyName("city")]
    public string? City
    {
        get => GetAttributeValue<string>("city");
        set => SetWithNotification("city", value!);
    }

    [JsonProperty("state")]
    [JsonPropertyName("state")]
    public string? State
    {
        get => GetAttributeValue<string>("state");
        set => SetWithNotification("state", value!);
    }

    [JsonProperty("postalCode")]
    [JsonPropertyName("postalCode")]
    public string? PostalCode
    {
        get => GetAttributeValue<string>("postalCode");
        set => SetWithNotification("postalCode", value!);
    }

    [JsonProperty("country")]
    [JsonPropertyName("country")]
    public string? Country
    {
        get => GetAttributeValue<string>("country");
        set => SetWithNotification("country", value!);
    }

    [JsonProperty("phoneNumber")]
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber
    {
        get => GetAttributeValue<string>("phoneNumber");
        set => SetWithNotification("phoneNumber", value!);
    }

    [JsonProperty("email")]
    [JsonPropertyName("email")]
    public string? Email
    {
        get => GetAttributeValue<string>("email");
        set => SetWithNotification("email", value!);
    }

    [JsonProperty("website")]
    [JsonPropertyName("website")]
    public string? Website
    {
        get => GetAttributeValue<string>("website");
        set => SetWithNotification("website", value!);
    }

    [JsonProperty("lastModifiedDateTime")]
    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime
    {
        get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime");
        set => SetWithNotification("lastModifiedDateTime", value!);
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt
    {
        get => LastModifiedDateTime;
        set => LastModifiedDateTime = value;
    }
}
