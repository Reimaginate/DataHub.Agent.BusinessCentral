using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("salesOrders")]
[BusinessCentralLastModified("lastModifiedDateTime")]
[BusinessCentralCreateReservation("salesDocumentReservations", "Order")]
public sealed class SalesOrder : BusinessCentralDocument, IBusinessCentralIncrementalEntity
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

    [JsonProperty("externalDocumentNumber")]
    [JsonPropertyName("externalDocumentNumber")]
    [BusinessCentralCreateRecoveryKey("externalDocumentNumber")]
    public string? ExternalDocumentNumber
    {
        get => GetAttributeValue<string>("externalDocumentNumber");
        set => SetWithNotification("externalDocumentNumber", value);
    }

    [JsonProperty("orderDate")]
    [JsonPropertyName("orderDate")]
    [BusinessCentralDate]
    public string? OrderDate
    {
        get => GetAttributeValue<string>("orderDate");
        set => SetWithNotification("orderDate", value);
    }

    [JsonProperty("customerId")]
    [JsonPropertyName("customerId")]
    [BusinessCentralReservationField("customerId")]
    public Guid? CustomerId
    {
        get => GetAttributeValue<Guid?>("customerId");
        set => SetWithNotification("customerId", value);
    }

    [JsonProperty("customerNumber")]
    [JsonPropertyName("customerNumber")]
    public string? CustomerNumber
    {
        get => GetAttributeValue<string>("customerNumber");
        set => SetWithNotification("customerNumber", value);
    }

    [JsonProperty("customerName")]
    [JsonPropertyName("customerName")]
    public string? CustomerName
    {
        get => GetAttributeValue<string>("customerName");
        set => SetWithNotification("customerName", value);
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

    [JsonProperty("status")]
    [JsonPropertyName("status")]
    public string? Status
    {
        get => GetAttributeValue<string>("status");
        set => SetWithNotification("status", value);
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
