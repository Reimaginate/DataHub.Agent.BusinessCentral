using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("itemLedgerEntries")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class ItemLedgerEntry : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("entryNumber"), JsonPropertyName("entryNumber")]
    public int? EntryNumber { get => GetAttributeValue<int?>("entryNumber"); set => SetWithNotification("entryNumber", value); }
    [JsonProperty("itemNumber"), JsonPropertyName("itemNumber")]
    public string? ItemNumber { get => GetAttributeValue<string>("itemNumber"); set => SetWithNotification("itemNumber", value); }
    [JsonProperty("postingDate"), JsonPropertyName("postingDate"), BusinessCentralDate]
    public string? PostingDate { get => GetAttributeValue<string>("postingDate"); set => SetWithNotification("postingDate", value); }
    [JsonProperty("entryType"), JsonPropertyName("entryType")]
    public string? EntryType { get => GetAttributeValue<string>("entryType"); set => SetWithNotification("entryType", value); }
    [JsonProperty("sourceNumber"), JsonPropertyName("sourceNumber")]
    public string? SourceNumber { get => GetAttributeValue<string>("sourceNumber"); set => SetWithNotification("sourceNumber", value); }
    [JsonProperty("sourceType"), JsonPropertyName("sourceType")]
    public string? SourceType { get => GetAttributeValue<string>("sourceType"); set => SetWithNotification("sourceType", value); }
    [JsonProperty("documentNumber"), JsonPropertyName("documentNumber")]
    public string? DocumentNumber { get => GetAttributeValue<string>("documentNumber"); set => SetWithNotification("documentNumber", value); }
    [JsonProperty("documentType"), JsonPropertyName("documentType")]
    public string? DocumentType { get => GetAttributeValue<string>("documentType"); set => SetWithNotification("documentType", value); }
    [JsonProperty("description"), JsonPropertyName("description")]
    public string? Description { get => GetAttributeValue<string>("description"); set => SetWithNotification("description", value); }
    [JsonProperty("quantity"), JsonPropertyName("quantity")]
    public decimal? Quantity { get => GetAttributeValue<decimal?>("quantity"); set => SetWithNotification("quantity", value); }
    [JsonProperty("salesAmountActual"), JsonPropertyName("salesAmountActual")]
    public decimal? SalesAmountActual { get => GetAttributeValue<decimal?>("salesAmountActual"); set => SetWithNotification("salesAmountActual", value); }
    [JsonProperty("costAmountActual"), JsonPropertyName("costAmountActual")]
    public decimal? CostAmountActual { get => GetAttributeValue<decimal?>("costAmountActual"); set => SetWithNotification("costAmountActual", value); }
    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
