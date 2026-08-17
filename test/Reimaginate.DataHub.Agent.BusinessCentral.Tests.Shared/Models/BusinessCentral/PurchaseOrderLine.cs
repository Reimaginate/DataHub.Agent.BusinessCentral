using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("purchaseOrderLines")]
[BusinessCentralParentUrl("purchaseOrders", nameof(DocumentId))]
[BusinessCentralCreateReservation("purchaseDocumentLineReservations", "Order")]
public sealed class PurchaseOrderLine : BusinessCentralDocument
{
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    [BusinessCentralReservationField("correlationId")]
    public Guid? DataHubCorrelationId { get; set; }

    [JsonProperty("documentId")]
    [JsonPropertyName("documentId")]
    [BusinessCentralReservationField("documentId")]
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
    [BusinessCentralReservationField("itemId")]
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

    [JsonProperty("directUnitCost")]
    [JsonPropertyName("directUnitCost")]
    public decimal? DirectUnitCost
    {
        get => GetAttributeValue<decimal?>("directUnitCost");
        set => SetWithNotification("directUnitCost", value);
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

    [JsonProperty("expectedReceiptDate")]
    [JsonPropertyName("expectedReceiptDate")]
    [BusinessCentralDate]
    public string? ExpectedReceiptDate
    {
        get => GetAttributeValue<string>("expectedReceiptDate");
        set => SetWithNotification("expectedReceiptDate", value);
    }

    [JsonProperty("receivedQuantity")]
    [JsonPropertyName("receivedQuantity")]
    public decimal? ReceivedQuantity
    {
        get => GetAttributeValue<decimal?>("receivedQuantity");
        set => SetWithNotification("receivedQuantity", value);
    }

    [JsonProperty("invoicedQuantity")]
    [JsonPropertyName("invoicedQuantity")]
    public decimal? InvoicedQuantity
    {
        get => GetAttributeValue<decimal?>("invoicedQuantity");
        set => SetWithNotification("invoicedQuantity", value);
    }

    [JsonProperty("invoiceQuantity")]
    [JsonPropertyName("invoiceQuantity")]
    public decimal? InvoiceQuantity
    {
        get => GetAttributeValue<decimal?>("invoiceQuantity");
        set => SetWithNotification("invoiceQuantity", value);
    }

    [JsonProperty("receiveQuantity")]
    [JsonPropertyName("receiveQuantity")]
    public decimal? ReceiveQuantity
    {
        get => GetAttributeValue<decimal?>("receiveQuantity");
        set => SetWithNotification("receiveQuantity", value);
    }
}
