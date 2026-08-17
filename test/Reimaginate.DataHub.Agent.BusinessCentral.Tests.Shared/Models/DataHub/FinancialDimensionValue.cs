using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "FinancialDimensionValue")]
public sealed class FinancialDimensionValue : DataHubEntity
{
    public FinancialDimensionValue() => entityType = nameof(FinancialDimensionValue);

    public EntityReference? Dimension { get; set; }
    public string? Code { get; set; }
    public string? DisplayName { get; set; }
    public string? ConsolidationCode { get; set; }
}
