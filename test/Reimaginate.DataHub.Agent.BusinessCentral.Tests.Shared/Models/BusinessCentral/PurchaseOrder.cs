using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("purchaseOrders")]
[BusinessCentralLastModified("lastModifiedDateTime")]
[BusinessCentralCreateReservation("purchaseDocumentReservations", "Order")]
public sealed class PurchaseOrder : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    [BusinessCentralReservationField("correlationId")]
    public Guid? DataHubCorrelationId { get; set; }

    [JsonProperty("number")]
    [JsonPropertyName("number")]
    public string? Number
    {
        get => GetAttributeValue<string>("number");
        set => SetWithNotification("number", value);
    }

    [JsonProperty("orderDate")]
    [JsonPropertyName("orderDate")]
    [BusinessCentralDate]
    public string? OrderDate
    {
        get => GetAttributeValue<string>("orderDate");
        set => SetWithNotification("orderDate", value);
    }

    [JsonProperty("postingDate")]
    [JsonPropertyName("postingDate")]
    [BusinessCentralDate]
    public string? PostingDate
    {
        get => GetAttributeValue<string>("postingDate");
        set => SetWithNotification("postingDate", value);
    }

    [JsonProperty("vendorId")]
    [JsonPropertyName("vendorId")]
    [BusinessCentralReservationField("vendorId")]
    public Guid? VendorId
    {
        get => GetAttributeValue<Guid?>("vendorId");
        set => SetWithNotification("vendorId", value);
    }

    [JsonProperty("vendorNumber")]
    [JsonPropertyName("vendorNumber")]
    public string? VendorNumber
    {
        get => GetAttributeValue<string>("vendorNumber");
        set => SetWithNotification("vendorNumber", value);
    }

    [JsonProperty("vendorName")]
    [JsonPropertyName("vendorName")]
    public string? VendorName
    {
        get => GetAttributeValue<string>("vendorName");
        set => SetWithNotification("vendorName", value);
    }

    [JsonProperty("requestedReceiptDate")]
    [JsonPropertyName("requestedReceiptDate")]
    [BusinessCentralDate]
    public string? RequestedReceiptDate
    {
        get => GetAttributeValue<string>("requestedReceiptDate");
        set => SetWithNotification("requestedReceiptDate", value);
    }

    [JsonProperty("discountAmount")]
    [JsonPropertyName("discountAmount")]
    public decimal? DiscountAmount
    {
        get => GetAttributeValue<decimal?>("discountAmount");
        set => SetWithNotification("discountAmount", value);
    }

    [JsonProperty("pricesIncludeTax")]
    [JsonPropertyName("pricesIncludeTax")]
    public bool? PricesIncludeTax
    {
        get => GetAttributeValue<bool?>("pricesIncludeTax");
        set => SetWithNotification("pricesIncludeTax", value);
    }

    [JsonProperty("status")]
    [JsonPropertyName("status")]
    public string? Status
    {
        get => GetAttributeValue<string>("status");
        set => SetWithNotification("status", value);
    }

    [JsonProperty("fullyReceived")]
    [JsonPropertyName("fullyReceived")]
    public bool? FullyReceived
    {
        get => GetAttributeValue<bool?>("fullyReceived");
        set => SetWithNotification("fullyReceived", value);
    }

    [JsonProperty("totalAmountExcludingTax")]
    [JsonPropertyName("totalAmountExcludingTax")]
    public decimal? TotalAmountExcludingTax
    {
        get => GetAttributeValue<decimal?>("totalAmountExcludingTax");
        set => SetWithNotification("totalAmountExcludingTax", value);
    }

    [JsonProperty("totalTaxAmount")]
    [JsonPropertyName("totalTaxAmount")]
    public decimal? TotalTaxAmount
    {
        get => GetAttributeValue<decimal?>("totalTaxAmount");
        set => SetWithNotification("totalTaxAmount", value);
    }

    [JsonProperty("totalAmountIncludingTax")]
    [JsonPropertyName("totalAmountIncludingTax")]
    public decimal? TotalAmountIncludingTax
    {
        get => GetAttributeValue<decimal?>("totalAmountIncludingTax");
        set => SetWithNotification("totalAmountIncludingTax", value);
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
