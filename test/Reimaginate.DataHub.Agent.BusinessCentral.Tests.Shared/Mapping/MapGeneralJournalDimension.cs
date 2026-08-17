using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Mapping;
using Reimaginate.Mapper;
using BCLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournalDimensionSetLine;
using BCParent = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.GeneralJournalLine;
using BCValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.BusinessCentral.FinancialDimensionValue;
using DHLine = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournalLineDimension;
using DHParent = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.GeneralJournalLine;
using DHValue = Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub.FinancialDimensionValue;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;

public sealed class MapGeneralJournalDimensionToBusinessCentralGeneralJournalDimension : ITypeMapper<DHLine, BCLine>, IDataHubTypeMapper<DHLine, BCLine>
{
    public List<string> MappedEntityReferences { get; } = [nameof(DHLine.JournalLine), nameof(DHLine.DimensionValue)];
    public Task<BCLine> MapAsync(DHLine from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        if (from.JournalLine is null || from.DimensionValue is null || string.IsNullOrWhiteSpace(from.Code))
            throw new InvalidOperationException("A general journal line dimension requires a journal line, dimension code, and dimension value.");
        var parentId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DHParent>(from.JournalLine, typeof(BCParent).Name, cache);
        var valueId = BusinessCentralMappingHelpers.ResolveBusinessCentralId<DHValue>(from.DimensionValue, typeof(BCValue).Name, cache);
        if (!parentId.HasValue || !valueId.HasValue)
            throw new InvalidOperationException($"General journal dimension '{from.id}' requires tracked journal-line and dimension-value ids.");
        return Task.FromResult(new BCLine { ParentId = parentId, Code = from.Code, ValueId = valueId });
    }
}

public sealed class MapBusinessCentralGeneralJournalDimensionToGeneralJournalDimension : ITypeMapper<BCLine, DHLine>
{
    public Task<DHLine> MapAsync(BCLine from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null) =>
        Task.FromResult(new DHLine
        {
            id = from.Id!, createdOn = DateTimeOffset.UnixEpoch, lastUpdated = DateTimeOffset.UnixEpoch,
            JournalLine = BusinessCentralMappingHelpers.ToDataHubReference<DHParent, BCParent>(from.ParentId),
            DimensionValue = BusinessCentralMappingHelpers.ToDataHubReference<DHValue, BCValue>(from.ValueId),
            Code = from.Code, DisplayName = from.DisplayName, ValueCode = from.ValueCode, ValueDisplayName = from.ValueDisplayName
        });
}
