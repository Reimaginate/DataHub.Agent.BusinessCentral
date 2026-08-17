using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral;

[BusinessCentralUrl("dimensionSetLines")]
[BusinessCentralParentUrl("customerPayments", nameof(ParentId))]
public sealed class CustomerPaymentDimensionSetLine : BusinessCentralDocument
{
    [JsonProperty("code"), JsonPropertyName("code")]
    public string? Code { get => GetAttributeValue<string>("code"); set => SetWithNotification("code", value); }

    [JsonProperty("consolidationCode"), JsonPropertyName("consolidationCode")]
    public string? ConsolidationCode { get => GetAttributeValue<string>("consolidationCode"); set => SetWithNotification("consolidationCode", value); }

    [JsonProperty("parentId"), JsonPropertyName("parentId")]
    public Guid? ParentId { get => GetAttributeValue<Guid?>("parentId"); set => SetWithNotification("parentId", value); }

    [JsonProperty("parentType"), JsonPropertyName("parentType")]
    public string? ParentType { get => GetAttributeValue<string>("parentType"); set => SetWithNotification("parentType", value); }

    [JsonProperty("displayName"), JsonPropertyName("displayName")]
    public string? DisplayName { get => GetAttributeValue<string>("displayName"); set => SetWithNotification("displayName", value); }

    [JsonProperty("valueId"), JsonPropertyName("valueId")]
    public Guid? ValueId { get => GetAttributeValue<Guid?>("valueId"); set => SetWithNotification("valueId", value); }

    [JsonProperty("valueCode"), JsonPropertyName("valueCode")]
    public string? ValueCode { get => GetAttributeValue<string>("valueCode"); set => SetWithNotification("valueCode", value); }

    [JsonProperty("valueConsolidationCode"), JsonPropertyName("valueConsolidationCode")]
    public string? ValueConsolidationCode { get => GetAttributeValue<string>("valueConsolidationCode"); set => SetWithNotification("valueConsolidationCode", value); }

    [JsonProperty("valueDisplayName"), JsonPropertyName("valueDisplayName")]
    public string? ValueDisplayName { get => GetAttributeValue<string>("valueDisplayName"); set => SetWithNotification("valueDisplayName", value); }
}
