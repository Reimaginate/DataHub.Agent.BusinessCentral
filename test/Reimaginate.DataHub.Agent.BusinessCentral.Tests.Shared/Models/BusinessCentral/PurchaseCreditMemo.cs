using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("purchaseCreditMemos")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class PurchaseCreditMemo : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("number"), JsonPropertyName("number")]
    public string? Number { get => GetAttributeValue<string>("number"); set => SetWithNotification("number", value); }

    [JsonProperty("vendorCreditMemoNumber"), JsonPropertyName("vendorCreditMemoNumber")]
    public string? VendorCreditMemoNumber { get => GetAttributeValue<string>("vendorCreditMemoNumber"); set => SetWithNotification("vendorCreditMemoNumber", value); }

    [JsonProperty("creditMemoDate"), JsonPropertyName("creditMemoDate"), BusinessCentralDate]
    public string? CreditMemoDate { get => GetAttributeValue<string>("creditMemoDate"); set => SetWithNotification("creditMemoDate", value); }

    [JsonProperty("postingDate"), JsonPropertyName("postingDate"), BusinessCentralDate]
    public string? PostingDate { get => GetAttributeValue<string>("postingDate"); set => SetWithNotification("postingDate", value); }

    [JsonProperty("dueDate"), JsonPropertyName("dueDate"), BusinessCentralDate]
    public string? DueDate { get => GetAttributeValue<string>("dueDate"); set => SetWithNotification("dueDate", value); }

    [JsonProperty("vendorId"), JsonPropertyName("vendorId")]
    public Guid? VendorId { get => GetAttributeValue<Guid?>("vendorId"); set => SetWithNotification("vendorId", value); }

    [JsonProperty("vendorNumber"), JsonPropertyName("vendorNumber")]
    public string? VendorNumber { get => GetAttributeValue<string>("vendorNumber"); set => SetWithNotification("vendorNumber", value); }

    [JsonProperty("vendorName"), JsonPropertyName("vendorName")]
    public string? VendorName { get => GetAttributeValue<string>("vendorName"); set => SetWithNotification("vendorName", value); }

    [JsonProperty("invoiceId"), JsonPropertyName("invoiceId")]
    public Guid? InvoiceId { get => GetAttributeValue<Guid?>("invoiceId"); set => SetWithNotification("invoiceId", value); }

    [JsonProperty("invoiceNumber"), JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get => GetAttributeValue<string>("invoiceNumber"); set => SetWithNotification("invoiceNumber", value); }

    [JsonProperty("status"), JsonPropertyName("status")]
    public string? Status { get => GetAttributeValue<string>("status"); set => SetWithNotification("status", value); }

    [JsonProperty("pricesIncludeTax"), JsonPropertyName("pricesIncludeTax")]
    public bool? PricesIncludeTax { get => GetAttributeValue<bool?>("pricesIncludeTax"); set => SetWithNotification("pricesIncludeTax", value); }

    [JsonProperty("discountAmount"), JsonPropertyName("discountAmount")]
    public decimal? DiscountAmount { get => GetAttributeValue<decimal?>("discountAmount"); set => SetWithNotification("discountAmount", value); }

    [JsonProperty("discountAppliedBeforeTax"), JsonPropertyName("discountAppliedBeforeTax")]
    public bool? DiscountAppliedBeforeTax { get => GetAttributeValue<bool?>("discountAppliedBeforeTax"); set => SetWithNotification("discountAppliedBeforeTax", value); }

    [JsonProperty("totalAmountExcludingTax"), JsonPropertyName("totalAmountExcludingTax")]
    public decimal? TotalAmountExcludingTax { get => GetAttributeValue<decimal?>("totalAmountExcludingTax"); set => SetWithNotification("totalAmountExcludingTax", value); }

    [JsonProperty("totalTaxAmount"), JsonPropertyName("totalTaxAmount")]
    public decimal? TotalTaxAmount { get => GetAttributeValue<decimal?>("totalTaxAmount"); set => SetWithNotification("totalTaxAmount", value); }

    [JsonProperty("totalAmountIncludingTax"), JsonPropertyName("totalAmountIncludingTax")]
    public decimal? TotalAmountIncludingTax { get => GetAttributeValue<decimal?>("totalAmountIncludingTax"); set => SetWithNotification("totalAmountIncludingTax", value); }

    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }

    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
