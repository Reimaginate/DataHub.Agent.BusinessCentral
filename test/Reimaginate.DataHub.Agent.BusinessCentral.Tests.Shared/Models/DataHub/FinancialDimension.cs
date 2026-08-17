using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "FinancialDimension")]
public sealed class FinancialDimension : DataHubEntity
{
    public FinancialDimension() => entityType = nameof(FinancialDimension);

    public string? Code { get; set; }
    public string? DisplayName { get; set; }
    public string? ConsolidationCode { get; set; }
}
