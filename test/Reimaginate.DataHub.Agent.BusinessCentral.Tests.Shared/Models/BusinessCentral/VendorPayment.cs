using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("vendorPayments")]
[BusinessCentralParentUrl("vendorPaymentJournals", nameof(JournalId))]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class VendorPayment : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("journalId"), JsonPropertyName("journalId")]
    public Guid? JournalId { get => GetAttributeValue<Guid?>("journalId"); set => SetWithNotification("journalId", value); }
    [JsonProperty("journalDisplayName"), JsonPropertyName("journalDisplayName")]
    public string? JournalDisplayName { get => GetAttributeValue<string>("journalDisplayName"); set => SetWithNotification("journalDisplayName", value); }
    [JsonProperty("lineNumber"), JsonPropertyName("lineNumber")]
    public int? LineNumber { get => GetAttributeValue<int?>("lineNumber"); set => SetWithNotification("lineNumber", value); }
    [JsonProperty("vendorId"), JsonPropertyName("vendorId")]
    public Guid? VendorId { get => GetAttributeValue<Guid?>("vendorId"); set => SetWithNotification("vendorId", value); }
    [JsonProperty("vendorNumber"), JsonPropertyName("vendorNumber")]
    public string? VendorNumber { get => GetAttributeValue<string>("vendorNumber"); set => SetWithNotification("vendorNumber", value); }
    [JsonProperty("postingDate"), JsonPropertyName("postingDate"), BusinessCentralDate]
    public string? PostingDate { get => GetAttributeValue<string>("postingDate"); set => SetWithNotification("postingDate", value); }
    [JsonProperty("documentNumber"), JsonPropertyName("documentNumber")]
    public string? DocumentNumber { get => GetAttributeValue<string>("documentNumber"); set => SetWithNotification("documentNumber", value); }
    [JsonProperty("externalDocumentNumber"), JsonPropertyName("externalDocumentNumber")]
    public string? ExternalDocumentNumber { get => GetAttributeValue<string>("externalDocumentNumber"); set => SetWithNotification("externalDocumentNumber", value); }
    [JsonProperty("amount"), JsonPropertyName("amount")]
    public decimal? Amount { get => GetAttributeValue<decimal?>("amount"); set => SetWithNotification("amount", value); }
    [JsonProperty("appliesToInvoiceId"), JsonPropertyName("appliesToInvoiceId")]
    public Guid? AppliesToInvoiceId { get => GetAttributeValue<Guid?>("appliesToInvoiceId"); set => SetWithNotification("appliesToInvoiceId", value); }
    [JsonProperty("appliesToInvoiceNumber"), JsonPropertyName("appliesToInvoiceNumber")]
    public string? AppliesToInvoiceNumber { get => GetAttributeValue<string>("appliesToInvoiceNumber"); set => SetWithNotification("appliesToInvoiceNumber", value); }
    [JsonProperty("description"), JsonPropertyName("description")]
    public string? Description { get => GetAttributeValue<string>("description"); set => SetWithNotification("description", value); }
    [JsonProperty("comment"), JsonPropertyName("comment")]
    public string? Comment { get => GetAttributeValue<string>("comment"); set => SetWithNotification("comment", value); }
    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
