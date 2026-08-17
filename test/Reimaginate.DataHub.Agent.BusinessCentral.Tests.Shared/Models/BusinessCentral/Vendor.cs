using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("vendors")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class Vendor : BusinessCentralDocument, IBusinessCentralIncrementalEntity
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

    [JsonProperty("addressLine1")]
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1
    {
        get => GetAttributeValue<string>("addressLine1");
        set => SetWithNotification("addressLine1", value);
    }

    [JsonProperty("addressLine2")]
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2
    {
        get => GetAttributeValue<string>("addressLine2");
        set => SetWithNotification("addressLine2", value);
    }

    [JsonProperty("city")]
    [JsonPropertyName("city")]
    public string? City
    {
        get => GetAttributeValue<string>("city");
        set => SetWithNotification("city", value);
    }

    [JsonProperty("state")]
    [JsonPropertyName("state")]
    public string? State
    {
        get => GetAttributeValue<string>("state");
        set => SetWithNotification("state", value);
    }

    [JsonProperty("country")]
    [JsonPropertyName("country")]
    public string? Country
    {
        get => GetAttributeValue<string>("country");
        set => SetWithNotification("country", value);
    }

    [JsonProperty("postalCode")]
    [JsonPropertyName("postalCode")]
    public string? PostalCode
    {
        get => GetAttributeValue<string>("postalCode");
        set => SetWithNotification("postalCode", value);
    }

    [JsonProperty("phoneNumber")]
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber
    {
        get => GetAttributeValue<string>("phoneNumber");
        set => SetWithNotification("phoneNumber", value);
    }

    [JsonProperty("email")]
    [JsonPropertyName("email")]
    public string? Email
    {
        get => GetAttributeValue<string>("email");
        set => SetWithNotification("email", value);
    }

    [JsonProperty("website")]
    [JsonPropertyName("website")]
    public string? Website
    {
        get => GetAttributeValue<string>("website");
        set => SetWithNotification("website", value);
    }

    [JsonProperty("taxRegistrationNumber")]
    [JsonPropertyName("taxRegistrationNumber")]
    public string? TaxRegistrationNumber
    {
        get => GetAttributeValue<string>("taxRegistrationNumber");
        set => SetWithNotification("taxRegistrationNumber", value);
    }

    [JsonProperty("currencyId")]
    [JsonPropertyName("currencyId")]
    public Guid? CurrencyId
    {
        get => GetAttributeValue<Guid?>("currencyId");
        set => SetWithNotification("currencyId", value);
    }

    [JsonProperty("currencyCode")]
    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode
    {
        get => GetAttributeValue<string>("currencyCode");
        set => SetWithNotification("currencyCode", value);
    }

    [JsonProperty("irs1099Code")]
    [JsonPropertyName("irs1099Code")]
    public string? Irs1099Code
    {
        get => GetAttributeValue<string>("irs1099Code");
        set => SetWithNotification("irs1099Code", value);
    }

    [JsonProperty("paymentTermsId")]
    [JsonPropertyName("paymentTermsId")]
    public Guid? PaymentTermsId
    {
        get => GetAttributeValue<Guid?>("paymentTermsId");
        set => SetWithNotification("paymentTermsId", value);
    }

    [JsonProperty("paymentMethodId")]
    [JsonPropertyName("paymentMethodId")]
    public Guid? PaymentMethodId
    {
        get => GetAttributeValue<Guid?>("paymentMethodId");
        set => SetWithNotification("paymentMethodId", value);
    }

    [JsonProperty("taxLiable")]
    [JsonPropertyName("taxLiable")]
    public bool? TaxLiable
    {
        get => GetAttributeValue<bool?>("taxLiable");
        set => SetWithNotification("taxLiable", value);
    }

    [JsonProperty("blocked")]
    [JsonPropertyName("blocked")]
    public string? Blocked
    {
        get => GetAttributeValue<string>("blocked");
        set => SetWithNotification("blocked", value);
    }

    [JsonProperty("balance")]
    [JsonPropertyName("balance")]
    public decimal? Balance
    {
        get => GetAttributeValue<decimal?>("balance");
        set => SetWithNotification("balance", value);
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
