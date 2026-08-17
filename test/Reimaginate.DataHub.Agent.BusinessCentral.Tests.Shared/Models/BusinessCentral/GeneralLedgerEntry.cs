using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralApiRoute("api/reimaginate/dataHub/v1.0")]
[BusinessCentralUrl("generalLedgerEntries")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class GeneralLedgerEntry : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("entryNumber"), JsonPropertyName("entryNumber")]
    public int? EntryNumber { get => GetAttributeValue<int?>("entryNumber"); set => SetWithNotification("entryNumber", value); }
    [JsonProperty("postingDate"), JsonPropertyName("postingDate"), BusinessCentralDate]
    public string? PostingDate { get => GetAttributeValue<string>("postingDate"); set => SetWithNotification("postingDate", value); }
    [JsonProperty("documentNumber"), JsonPropertyName("documentNumber")]
    public string? DocumentNumber { get => GetAttributeValue<string>("documentNumber"); set => SetWithNotification("documentNumber", value); }
    [JsonProperty("documentType"), JsonPropertyName("documentType")]
    public string? DocumentType { get => GetAttributeValue<string>("documentType"); set => SetWithNotification("documentType", value); }
    [JsonProperty("accountId"), JsonPropertyName("accountId")]
    public Guid? AccountId { get => GetAttributeValue<Guid?>("accountId"); set => SetWithNotification("accountId", value); }
    [JsonProperty("accountNumber"), JsonPropertyName("accountNumber")]
    public string? AccountNumber { get => GetAttributeValue<string>("accountNumber"); set => SetWithNotification("accountNumber", value); }
    [JsonProperty("description"), JsonPropertyName("description")]
    public string? Description { get => GetAttributeValue<string>("description"); set => SetWithNotification("description", value); }
    [JsonProperty("debitAmount"), JsonPropertyName("debitAmount")]
    public decimal? DebitAmount { get => GetAttributeValue<decimal?>("debitAmount"); set => SetWithNotification("debitAmount", value); }
    [JsonProperty("creditAmount"), JsonPropertyName("creditAmount")]
    public decimal? CreditAmount { get => GetAttributeValue<decimal?>("creditAmount"); set => SetWithNotification("creditAmount", value); }
    [JsonProperty("additionalCurrencyDebitAmount"), JsonPropertyName("additionalCurrencyDebitAmount")]
    public decimal? AdditionalCurrencyDebitAmount { get => GetAttributeValue<decimal?>("additionalCurrencyDebitAmount"); set => SetWithNotification("additionalCurrencyDebitAmount", value); }
    [JsonProperty("additionalCurrencyCreditAmount"), JsonPropertyName("additionalCurrencyCreditAmount")]
    public decimal? AdditionalCurrencyCreditAmount { get => GetAttributeValue<decimal?>("additionalCurrencyCreditAmount"); set => SetWithNotification("additionalCurrencyCreditAmount", value); }
    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
