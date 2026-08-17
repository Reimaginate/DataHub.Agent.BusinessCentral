using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("journals")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class GeneralJournal : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonProperty("code"), JsonPropertyName("code"), BusinessCentralCreateRecoveryKey("code")]
    public string? Code { get => GetAttributeValue<string>("code"); set => SetWithNotification("code", value); }
    [JsonProperty("displayName"), JsonPropertyName("displayName")]
    public string? DisplayName { get => GetAttributeValue<string>("displayName"); set => SetWithNotification("displayName", value); }
    [JsonProperty("templateDisplayName"), JsonPropertyName("templateDisplayName")]
    public string? TemplateDisplayName { get => GetAttributeValue<string>("templateDisplayName"); set => SetWithNotification("templateDisplayName", value); }
    [JsonProperty("balancingAccountId"), JsonPropertyName("balancingAccountId")]
    public Guid? BalancingAccountId { get => GetAttributeValue<Guid?>("balancingAccountId"); set => SetWithNotification("balancingAccountId", value); }
    [JsonProperty("balancingAccountNumber"), JsonPropertyName("balancingAccountNumber")]
    public string? BalancingAccountNumber { get => GetAttributeValue<string>("balancingAccountNumber"); set => SetWithNotification("balancingAccountNumber", value); }
    [JsonProperty("lastModifiedDateTime"), JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime"); set => SetWithNotification("lastModifiedDateTime", value); }
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset? LastModifiedAt { get => LastModifiedDateTime; set => LastModifiedDateTime = value; }
}
