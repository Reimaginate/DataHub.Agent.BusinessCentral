using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("salesInvoices")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class SalesInvoice : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
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

    [JsonProperty("invoiceDate")]
    [JsonPropertyName("invoiceDate")]
    [BusinessCentralDate]
    public string? InvoiceDate
    {
        get => GetAttributeValue<string>("invoiceDate");
        set => SetWithNotification("invoiceDate", value);
    }

    [JsonProperty("postingDate")]
    [JsonPropertyName("postingDate")]
    [BusinessCentralDate]
    public string? PostingDate
    {
        get => GetAttributeValue<string>("postingDate");
        set => SetWithNotification("postingDate", value);
    }

    [JsonProperty("dueDate")]
    [JsonPropertyName("dueDate")]
    [BusinessCentralDate]
    public string? DueDate
    {
        get => GetAttributeValue<string>("dueDate");
        set => SetWithNotification("dueDate", value);
    }

    [JsonProperty("customerId")]
    [JsonPropertyName("customerId")]
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

    [JsonProperty("pricesIncludeTax")]
    [JsonPropertyName("pricesIncludeTax")]
    public bool? PricesIncludeTax
    {
        get => GetAttributeValue<bool?>("pricesIncludeTax");
        set => SetWithNotification("pricesIncludeTax", value);
    }

    [JsonProperty("remainingAmount")]
    [JsonPropertyName("remainingAmount")]
    public decimal? RemainingAmount
    {
        get => GetAttributeValue<decimal?>("remainingAmount");
        set => SetWithNotification("remainingAmount", value);
    }

    [JsonProperty("discountAmount")]
    [JsonPropertyName("discountAmount")]
    public decimal? DiscountAmount
    {
        get => GetAttributeValue<decimal?>("discountAmount");
        set => SetWithNotification("discountAmount", value);
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
