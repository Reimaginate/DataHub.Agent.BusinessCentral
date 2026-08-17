using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("journalLines")]
[BusinessCentralParentUrl("journals", nameof(JournalId))]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class GeneralJournalLine : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("journalId"), JsonPropertyName("journalId")]
    public Guid? JournalId { get => GetAttributeValue<Guid?>("journalId"); set => SetWithNotification("journalId", value); }
    [JsonProperty("journalDisplayName"), JsonPropertyName("journalDisplayName")]
    public string? JournalDisplayName { get => GetAttributeValue<string>("journalDisplayName"); set => SetWithNotification("journalDisplayName", value); }
    [JsonProperty("lineNumber"), JsonPropertyName("lineNumber")]
    public int? LineNumber { get => GetAttributeValue<int?>("lineNumber"); set => SetWithNotification("lineNumber", value); }
    [JsonProperty("accountType"), JsonPropertyName("accountType")]
    public string? AccountType { get => GetAttributeValue<string>("accountType"); set => SetWithNotification("accountType", NormalizeAccountType(value)); }
    [JsonProperty("accountId"), JsonPropertyName("accountId")]
    public Guid? AccountId { get => GetAttributeValue<Guid?>("accountId"); set => SetWithNotification("accountId", value); }
    [JsonProperty("accountNumber"), JsonPropertyName("accountNumber")]
    public string? AccountNumber { get => GetAttributeValue<string>("accountNumber"); set => SetWithNotification("accountNumber", value); }
    [JsonProperty("postingDate"), JsonPropertyName("postingDate"), BusinessCentralDate]
    public string? PostingDate { get => GetAttributeValue<string>("postingDate"); set => SetWithNotification("postingDate", value); }
    [JsonProperty("documentNumber"), JsonPropertyName("documentNumber")]
    public string? DocumentNumber { get => GetAttributeValue<string>("documentNumber"); set => SetWithNotification("documentNumber", value); }
    [JsonProperty("externalDocumentNumber"), JsonPropertyName("externalDocumentNumber")]
    public string? ExternalDocumentNumber { get => GetAttributeValue<string>("externalDocumentNumber"); set => SetWithNotification("externalDocumentNumber", value); }
    [JsonProperty("amount"), JsonPropertyName("amount")]
    public decimal? Amount { get => GetAttributeValue<decimal?>("amount"); set => SetWithNotification("amount", value); }
    [JsonProperty("description"), JsonPropertyName("description")]
    public string? Description { get => GetAttributeValue<string>("description"); set => SetWithNotification("description", value); }
    [JsonProperty("comment"), JsonPropertyName("comment")]
    public string? Comment { get => GetAttributeValue<string>("comment"); set => SetWithNotification("comment", value); }
    [JsonProperty("taxCode"), JsonPropertyName("taxCode")]
    public string? TaxCode { get => GetAttributeValue<string>("taxCode"); set => SetWithNotification("taxCode", value); }
    [JsonProperty("balanceAccountType"), JsonPropertyName("balanceAccountType")]
    public string? BalanceAccountType { get => GetAttributeValue<string>("balanceAccountType"); set => SetWithNotification("balanceAccountType", NormalizeAccountType(value)); }
    [JsonProperty("balancingAccountId"), JsonPropertyName("balancingAccountId")]
    public Guid? BalancingAccountId { get => GetAttributeValue<Guid?>("balancingAccountId"); set => SetWithNotification("balancingAccountId", value); }
    [JsonProperty("balancingAccountNumber"), JsonPropertyName("balancingAccountNumber")]
    public string? BalancingAccountNumber { get => GetAttributeValue<string>("balancingAccountNumber"); set => SetWithNotification("balancingAccountNumber", value); }
    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }

    private static string? NormalizeAccountType(string? value)
    {
        if (string.Equals(value, "G_x002F_L_x0020_Account", StringComparison.OrdinalIgnoreCase))
            return "G/L Account";
        if (string.Equals(value, "Bank_x0020_Account", StringComparison.OrdinalIgnoreCase))
            return "Bank Account";
        return value;
    }
}
