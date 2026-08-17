using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "GeneralJournalDimensionSetLine")]
public sealed class GeneralJournalLineDimension : DataHubEntity
{
    public GeneralJournalLineDimension() => entityType = nameof(GeneralJournalLineDimension);
    public EntityReference? JournalLine { get; set; }
    public EntityReference? Dimension { get; set; }
    public EntityReference? DimensionValue { get; set; }
    public string? Code { get; set; }
    public string? DisplayName { get; set; }
    public string? ValueCode { get; set; }
    public string? ValueDisplayName { get; set; }
}
