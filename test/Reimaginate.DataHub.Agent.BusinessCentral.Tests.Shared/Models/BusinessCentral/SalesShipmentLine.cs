using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("salesShipmentLines")]
[BusinessCentralParentUrl("salesShipments", nameof(DocumentId))]
public sealed class SalesShipmentLine : BusinessCentralDocument
{
    [JsonProperty("documentId"), JsonPropertyName("documentId")]
    public Guid? DocumentId { get => GetAttributeValue<Guid?>("documentId"); set => SetWithNotification("documentId", value); }
    [JsonProperty("documentNo"), JsonPropertyName("documentNo")]
    public string? DocumentNumber { get => GetAttributeValue<string>("documentNo"); set => SetWithNotification("documentNo", value); }
    [JsonProperty("sequence"), JsonPropertyName("sequence")]
    public int? Sequence { get => GetAttributeValue<int?>("sequence"); set => SetWithNotification("sequence", value); }
    [JsonProperty("lineType"), JsonPropertyName("lineType")]
    public string? LineType { get => GetAttributeValue<string>("lineType"); set => SetWithNotification("lineType", value); }
    [JsonProperty("lineObjectNumber"), JsonPropertyName("lineObjectNumber")]
    public string? LineObjectNumber { get => GetAttributeValue<string>("lineObjectNumber"); set => SetWithNotification("lineObjectNumber", value); }
    [JsonProperty("description"), JsonPropertyName("description")]
    public string? Description { get => GetAttributeValue<string>("description"); set => SetWithNotification("description", value); }
    [JsonProperty("description2"), JsonPropertyName("description2")]
    public string? Description2 { get => GetAttributeValue<string>("description2"); set => SetWithNotification("description2", value); }
    [JsonProperty("unitOfMeasureCode"), JsonPropertyName("unitOfMeasureCode")]
    public string? UnitOfMeasureCode { get => GetAttributeValue<string>("unitOfMeasureCode"); set => SetWithNotification("unitOfMeasureCode", value); }
    [JsonProperty("unitPrice"), JsonPropertyName("unitPrice")]
    public decimal? UnitPrice { get => GetAttributeValue<decimal?>("unitPrice"); set => SetWithNotification("unitPrice", value); }
    [JsonProperty("quantity"), JsonPropertyName("quantity")]
    public decimal? Quantity { get => GetAttributeValue<decimal?>("quantity"); set => SetWithNotification("quantity", value); }
    [JsonProperty("discountPercent"), JsonPropertyName("discountPercent")]
    public decimal? DiscountPercent { get => GetAttributeValue<decimal?>("discountPercent"); set => SetWithNotification("discountPercent", value); }
    [JsonProperty("taxPercent"), JsonPropertyName("taxPercent")]
    public decimal? TaxPercent { get => GetAttributeValue<decimal?>("taxPercent"); set => SetWithNotification("taxPercent", value); }
    [JsonProperty("shipmentDate"), JsonPropertyName("shipmentDate"), BusinessCentralDate]
    public string? ShipmentDate { get => GetAttributeValue<string>("shipmentDate"); set => SetWithNotification("shipmentDate", value); }
}
