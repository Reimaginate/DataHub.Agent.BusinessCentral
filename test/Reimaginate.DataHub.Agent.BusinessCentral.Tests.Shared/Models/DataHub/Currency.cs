using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "Currency")]
public sealed class Currency : DataHubEntity
{
    public Currency() => entityType = nameof(Currency);
    public string? Code { get; set; }
    public string? DisplayName { get; set; }
    public string? Symbol { get; set; }
    public string? AmountDecimalPlaces { get; set; }
    public decimal? AmountRoundingPrecision { get; set; }
}
