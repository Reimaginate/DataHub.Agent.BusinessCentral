using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("purchaseReceipts")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class PurchaseReceipt : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("number"), JsonPropertyName("number")]
    public string? Number { get => GetAttributeValue<string>("number"); set => SetWithNotification("number", value); }
    [JsonProperty("invoiceDate"), JsonPropertyName("invoiceDate"), BusinessCentralDate]
    public string? InvoiceDate { get => GetAttributeValue<string>("invoiceDate"); set => SetWithNotification("invoiceDate", value); }
    [JsonProperty("postingDate"), JsonPropertyName("postingDate"), BusinessCentralDate]
    public string? PostingDate { get => GetAttributeValue<string>("postingDate"); set => SetWithNotification("postingDate", value); }
    [JsonProperty("dueDate"), JsonPropertyName("dueDate"), BusinessCentralDate]
    public string? DueDate { get => GetAttributeValue<string>("dueDate"); set => SetWithNotification("dueDate", value); }
    [JsonProperty("vendorNumber"), JsonPropertyName("vendorNumber")]
    public string? VendorNumber { get => GetAttributeValue<string>("vendorNumber"); set => SetWithNotification("vendorNumber", value); }
    [JsonProperty("vendorName"), JsonPropertyName("vendorName")]
    public string? VendorName { get => GetAttributeValue<string>("vendorName"); set => SetWithNotification("vendorName", value); }
    [JsonProperty("payToName"), JsonPropertyName("payToName")]
    public string? PayToName { get => GetAttributeValue<string>("payToName"); set => SetWithNotification("payToName", value); }
    [JsonProperty("payToContact"), JsonPropertyName("payToContact")]
    public string? PayToContact { get => GetAttributeValue<string>("payToContact"); set => SetWithNotification("payToContact", value); }
    [JsonProperty("shipToName"), JsonPropertyName("shipToName")]
    public string? ShipToName { get => GetAttributeValue<string>("shipToName"); set => SetWithNotification("shipToName", value); }
    [JsonProperty("shipToContact"), JsonPropertyName("shipToContact")]
    public string? ShipToContact { get => GetAttributeValue<string>("shipToContact"); set => SetWithNotification("shipToContact", value); }
    [JsonProperty("shipToCity"), JsonPropertyName("shipToCity")]
    public string? ShipToCity { get => GetAttributeValue<string>("shipToCity"); set => SetWithNotification("shipToCity", value); }
    [JsonProperty("shipToCountry"), JsonPropertyName("shipToCountry")]
    public string? ShipToCountry { get => GetAttributeValue<string>("shipToCountry"); set => SetWithNotification("shipToCountry", value); }
    [JsonProperty("shipToState"), JsonPropertyName("shipToState")]
    public string? ShipToState { get => GetAttributeValue<string>("shipToState"); set => SetWithNotification("shipToState", value); }
    [JsonProperty("shipToPostCode"), JsonPropertyName("shipToPostCode")]
    public string? ShipToPostCode { get => GetAttributeValue<string>("shipToPostCode"); set => SetWithNotification("shipToPostCode", value); }
    [JsonProperty("currencyCode"), JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get => GetAttributeValue<string>("currencyCode"); set => SetWithNotification("currencyCode", value); }
    [JsonProperty("orderNumber"), JsonPropertyName("orderNumber")]
    public string? OrderNumber { get => GetAttributeValue<string>("orderNumber"); set => SetWithNotification("orderNumber", value); }
    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
