using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("paymentTerms")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class PaymentTerm : BusinessCentralDocument, IBusinessCentralIncrementalEntity
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

    [JsonProperty("dueDateCalculation")]
    [JsonPropertyName("dueDateCalculation")]
    public string? DueDateCalculation
    {
        get => GetAttributeValue<string>("dueDateCalculation");
        set => SetWithNotification("dueDateCalculation", value);
    }

    [JsonProperty("discountDateCalculation")]
    [JsonPropertyName("discountDateCalculation")]
    public string? DiscountDateCalculation
    {
        get => GetAttributeValue<string>("discountDateCalculation");
        set => SetWithNotification("discountDateCalculation", value);
    }

    [JsonProperty("discountPercent")]
    [JsonPropertyName("discountPercent")]
    public decimal? DiscountPercent
    {
        get => GetAttributeValue<decimal?>("discountPercent");
        set => SetWithNotification("discountPercent", value);
    }

    [JsonProperty("calculateDiscountOnCreditMemos")]
    [JsonPropertyName("calculateDiscountOnCreditMemos")]
    public bool? CalculateDiscountOnCreditMemos
    {
        get => GetAttributeValue<bool?>("calculateDiscountOnCreditMemos");
        set => SetWithNotification("calculateDiscountOnCreditMemos", value);
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
