using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("bankAccounts")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class BankAccount : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("number"), JsonPropertyName("number")]
    public string? Number { get => GetAttributeValue<string>("number"); set => SetWithNotification("number", value); }

    [JsonProperty("displayName"), JsonPropertyName("displayName")]
    public string? DisplayName { get => GetAttributeValue<string>("displayName"); set => SetWithNotification("displayName", value); }

    [JsonProperty("bankAccountNumber"), JsonPropertyName("bankAccountNumber")]
    public string? BankAccountNumberValue { get => GetAttributeValue<string>("bankAccountNumber"); set => SetWithNotification("bankAccountNumber", value); }

    [JsonProperty("blocked"), JsonPropertyName("blocked")]
    public bool? Blocked { get => GetAttributeValue<bool?>("blocked"); set => SetWithNotification("blocked", value); }

    [JsonProperty("currencyId"), JsonPropertyName("currencyId")]
    public Guid? CurrencyId { get => GetAttributeValue<Guid?>("currencyId"); set => SetWithNotification("currencyId", value); }

    [JsonProperty("currencyCode"), JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get => GetAttributeValue<string>("currencyCode"); set => SetWithNotification("currencyCode", value); }

    [JsonProperty("iban"), JsonPropertyName("iban")]
    public string? Iban { get => GetAttributeValue<string>("iban"); set => SetWithNotification("iban", value); }

    [JsonProperty("intercompanyEnabled"), JsonPropertyName("intercompanyEnabled")]
    public bool? IntercompanyEnabled { get => GetAttributeValue<bool?>("intercompanyEnabled"); set => SetWithNotification("intercompanyEnabled", value); }

    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }

    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
