using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("items")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class Item : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("number")]
    [JsonPropertyName("number")]
    public string? Number
    {
        get => GetAttributeValue<string>("number");
        set => SetWithNotification("number", value);
    }

    [JsonProperty("displayName")]
    [JsonPropertyName("displayName")]
    public string? DisplayName
    {
        get => GetAttributeValue<string>("displayName");
        set => SetWithNotification("displayName", value);
    }

    [JsonProperty("displayName2")]
    [JsonPropertyName("displayName2")]
    public string? DisplayName2
    {
        get => GetAttributeValue<string>("displayName2");
        set => SetWithNotification("displayName2", value);
    }

    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string? Type
    {
        get => GetAttributeValue<string>("type");
        set => SetWithNotification("type", value);
    }

    [JsonProperty("blocked")]
    [JsonPropertyName("blocked")]
    public bool? Blocked
    {
        get => GetAttributeValue<bool?>("blocked");
        set => SetWithNotification("blocked", value);
    }

    [JsonProperty("gtin")]
    [JsonPropertyName("gtin")]
    public string? Gtin
    {
        get => GetAttributeValue<string>("gtin");
        set => SetWithNotification("gtin", value);
    }

    [JsonProperty("inventory")]
    [JsonPropertyName("inventory")]
    public decimal? Inventory
    {
        get => GetAttributeValue<decimal?>("inventory");
        set => SetWithNotification("inventory", value);
    }

    [JsonProperty("unitPrice")]
    [JsonPropertyName("unitPrice")]
    public decimal? UnitPrice
    {
        get => GetAttributeValue<decimal?>("unitPrice");
        set => SetWithNotification("unitPrice", value);
    }

    [JsonProperty("priceIncludesTax")]
    [JsonPropertyName("priceIncludesTax")]
    public bool? PriceIncludesTax
    {
        get => GetAttributeValue<bool?>("priceIncludesTax");
        set => SetWithNotification("priceIncludesTax", value);
    }

    [JsonProperty("unitCost")]
    [JsonPropertyName("unitCost")]
    public decimal? UnitCost
    {
        get => GetAttributeValue<decimal?>("unitCost");
        set => SetWithNotification("unitCost", value);
    }

    [JsonProperty("baseUnitOfMeasureId")]
    [JsonPropertyName("baseUnitOfMeasureId")]
    public Guid? BaseUnitOfMeasureId
    {
        get => GetAttributeValue<Guid?>("baseUnitOfMeasureId");
        set => SetWithNotification("baseUnitOfMeasureId", value);
    }

    [JsonProperty("baseUnitOfMeasureCode")]
    [JsonPropertyName("baseUnitOfMeasureCode")]
    public string? BaseUnitOfMeasureCode
    {
        get => GetAttributeValue<string>("baseUnitOfMeasureCode");
        set => SetWithNotification("baseUnitOfMeasureCode", value);
    }

    [JsonProperty("generalProductPostingGroupCode")]
    [JsonPropertyName("generalProductPostingGroupCode")]
    public string? GeneralProductPostingGroupCode
    {
        get => GetAttributeValue<string>("generalProductPostingGroupCode");
        set => SetWithNotification("generalProductPostingGroupCode", value);
    }

    [JsonProperty("inventoryPostingGroupCode")]
    [JsonPropertyName("inventoryPostingGroupCode")]
    public string? InventoryPostingGroupCode
    {
        get => GetAttributeValue<string>("inventoryPostingGroupCode");
        set => SetWithNotification("inventoryPostingGroupCode", value);
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
