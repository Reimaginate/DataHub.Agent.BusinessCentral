using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("salesInvoiceLines")]
[BusinessCentralParentUrl("salesInvoices", nameof(DocumentId))]
public sealed class SalesInvoiceLine : BusinessCentralDocument
{
    [JsonProperty("documentId")]
    [JsonPropertyName("documentId")]
    public Guid? DocumentId
    {
        get => GetAttributeValue<Guid?>("documentId");
        set => SetWithNotification("documentId", value);
    }

    [JsonProperty("sequence")]
    [JsonPropertyName("sequence")]
    public int? Sequence
    {
        get => GetAttributeValue<int?>("sequence");
        set => SetWithNotification("sequence", value);
    }

    [JsonProperty("itemId")]
    [JsonPropertyName("itemId")]
    public Guid? ItemId
    {
        get => GetAttributeValue<Guid?>("itemId");
        set => SetWithNotification("itemId", value);
    }

    [JsonProperty("lineType")]
    [JsonPropertyName("lineType")]
    public string? LineType
    {
        get => GetAttributeValue<string>("lineType");
        set => SetWithNotification("lineType", value);
    }

    [JsonProperty("lineObjectNumber")]
    [JsonPropertyName("lineObjectNumber")]
    public string? LineObjectNumber
    {
        get => GetAttributeValue<string>("lineObjectNumber");
        set => SetWithNotification("lineObjectNumber", value);
    }

    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string? Description
    {
        get => GetAttributeValue<string>("description");
        set => SetWithNotification("description", value);
    }

    [JsonProperty("description2")]
    [JsonPropertyName("description2")]
    public string? Description2
    {
        get => GetAttributeValue<string>("description2");
        set => SetWithNotification("description2", value);
    }

    [JsonProperty("quantity")]
    [JsonPropertyName("quantity")]
    public decimal? Quantity
    {
        get => GetAttributeValue<decimal?>("quantity");
        set => SetWithNotification("quantity", value);
    }

    [JsonProperty("unitPrice")]
    [JsonPropertyName("unitPrice")]
    public decimal? UnitPrice
    {
        get => GetAttributeValue<decimal?>("unitPrice");
        set => SetWithNotification("unitPrice", value);
    }

    [JsonProperty("discountAmount")]
    [JsonPropertyName("discountAmount")]
    public decimal? DiscountAmount
    {
        get => GetAttributeValue<decimal?>("discountAmount");
        set => SetWithNotification("discountAmount", value);
    }

    [JsonProperty("discountPercent")]
    [JsonPropertyName("discountPercent")]
    public decimal? DiscountPercent
    {
        get => GetAttributeValue<decimal?>("discountPercent");
        set => SetWithNotification("discountPercent", value);
    }

    [JsonProperty("amountExcludingTax")]
    [JsonPropertyName("amountExcludingTax")]
    public decimal? AmountExcludingTax
    {
        get => GetAttributeValue<decimal?>("amountExcludingTax");
        set => SetWithNotification("amountExcludingTax", value);
    }

    [JsonProperty("taxPercent")]
    [JsonPropertyName("taxPercent")]
    public decimal? TaxPercent
    {
        get => GetAttributeValue<decimal?>("taxPercent");
        set => SetWithNotification("taxPercent", value);
    }

    [JsonProperty("totalTaxAmount")]
    [JsonPropertyName("totalTaxAmount")]
    public decimal? TotalTaxAmount
    {
        get => GetAttributeValue<decimal?>("totalTaxAmount");
        set => SetWithNotification("totalTaxAmount", value);
    }

    [JsonProperty("amountIncludingTax")]
    [JsonPropertyName("amountIncludingTax")]
    public decimal? AmountIncludingTax
    {
        get => GetAttributeValue<decimal?>("amountIncludingTax");
        set => SetWithNotification("amountIncludingTax", value);
    }
}
